using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace RevolutionAssociates.Excel;

/// <summary>Reads OpenXML workbooks (.xlsx, .xlsm, .xltx, .xltm).</summary>
internal static class XlsxReader
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PkgRel = "http://schemas.openxmlformats.org/package/2006/relationships";

    private const int MaxRows = 1_048_576;
    private const int MaxCols = 16_384;

    private static readonly XmlReaderSettings Settings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true
    };

    public static Workbook Read(Stream stream)
    {
        ZipArchive zip;
        try
        {
            zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException("The file is not a valid .xlsx (OpenXML) package.", ex);
        }

        using (zip)
        {
            var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in zip.Entries) entries[Normalize(e.FullName)] = e;

            var workbookPath = FindWorkbookPart(entries);
            var workbookDoc = LoadXml(entries, workbookPath)
                              ?? throw new InvalidDataException("The package has no workbook part.");

            var wb = new Workbook();
            var pr = workbookDoc.Descendants(Main + "workbookPr").FirstOrDefault();
            var d1904 = (string?)pr?.Attribute("date1904");
            wb.Date1904 = d1904 == "1" || string.Equals(d1904, "true", StringComparison.OrdinalIgnoreCase);

            var baseDir = workbookPath.Contains('/') ? workbookPath[..(workbookPath.LastIndexOf('/') + 1)] : string.Empty;
            var relsPath = baseDir + "_rels/" + workbookPath[(workbookPath.LastIndexOf('/') + 1)..] + ".rels";
            var rels = LoadRelationships(entries, relsPath, baseDir);

            var sharedStrings = LoadSharedStrings(entries, rels);
            var dateStyles = LoadDateStyles(entries, rels);

            foreach (var sheetEl in workbookDoc.Descendants(Main + "sheet"))
            {
                var name = (string?)sheetEl.Attribute("name") ?? "Sheet" + (wb.Sheets.Count + 1);
                var rid = (string?)sheetEl.Attribute(RelNs + "id");
                var sheet = wb.AddSheet(name);
                // preserve the original name when it is valid (AddSheet only changes invalid/duplicate names)
                if (rid != null && rels.TryGetValue(rid, out var rel) && entries.TryGetValue(rel.Target, out var entry))
                {
                    using var s = entry.Open();
                    ReadSheet(s, sheet, sharedStrings, dateStyles, wb.Date1904);
                }
            }

            return wb;
        }
    }

    // ---------- package plumbing ----------

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');

    private sealed record Rel(string Type, string Target);

    private static string FindWorkbookPart(Dictionary<string, ZipArchiveEntry> entries)
    {
        var root = LoadXml(entries, "_rels/.rels");
        if (root != null)
        {
            foreach (var r in root.Descendants(PkgRel + "Relationship"))
            {
                var type = (string?)r.Attribute("Type") ?? string.Empty;
                if (type.EndsWith("/officeDocument", StringComparison.OrdinalIgnoreCase))
                {
                    var target = (string?)r.Attribute("Target");
                    if (target != null) return Normalize(target);
                }
            }
        }
        return "xl/workbook.xml";
    }

    private static Dictionary<string, Rel> LoadRelationships(
        Dictionary<string, ZipArchiveEntry> entries, string relsPath, string baseDir)
    {
        var result = new Dictionary<string, Rel>(StringComparer.Ordinal);
        var doc = LoadXml(entries, relsPath);
        if (doc == null) return result;

        foreach (var r in doc.Descendants(PkgRel + "Relationship"))
        {
            var id = (string?)r.Attribute("Id");
            var target = (string?)r.Attribute("Target");
            var type = (string?)r.Attribute("Type") ?? string.Empty;
            if (id == null || target == null) continue;
            if (string.Equals((string?)r.Attribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase)) continue;
            result[id] = new Rel(type, ResolveTarget(baseDir, target));
        }
        return result;
    }

    private static string ResolveTarget(string baseDir, string target)
    {
        target = target.Replace('\\', '/');
        if (target.StartsWith('/')) return target.TrimStart('/');

        var parts = new List<string>((baseDir + target).Split('/', StringSplitOptions.RemoveEmptyEntries));
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] == ".") { parts.RemoveAt(i); i--; }
            else if (parts[i] == ".." && i > 0) { parts.RemoveRange(i - 1, 2); i -= 2; }
        }
        return string.Join('/', parts);
    }

    private static XDocument? LoadXml(Dictionary<string, ZipArchiveEntry> entries, string path)
    {
        if (!entries.TryGetValue(Normalize(path), out var entry)) return null;
        using var s = entry.Open();
        using var xr = XmlReader.Create(s, Settings);
        return XDocument.Load(xr);
    }

    // ---------- shared strings ----------

    private static List<string> LoadSharedStrings(Dictionary<string, ZipArchiveEntry> entries, Dictionary<string, Rel> rels)
    {
        var list = new List<string>();
        var path = rels.Values.FirstOrDefault(r => r.Type.EndsWith("/sharedStrings", StringComparison.OrdinalIgnoreCase))?.Target
                   ?? "xl/sharedStrings.xml";
        if (!entries.TryGetValue(path, out var entry)) return list;

        using var s = entry.Open();
        using var xr = XmlReader.Create(s, Settings);
        while (xr.Read())
        {
            if (xr.NodeType == XmlNodeType.Element && xr.LocalName == "si")
                list.Add(ReadTextContainer(xr));
        }
        return list;
    }

    /// <summary>Reads the text of an &lt;si&gt; or &lt;is&gt; element, skipping phonetic runs.
    /// Leaves the reader on the container's end element (or on the element itself if empty).</summary>
    private static string ReadTextContainer(XmlReader r)
    {
        if (r.IsEmptyElement) return string.Empty;

        var sb = new StringBuilder();
        int depth = r.Depth;
        bool more = r.Read();
        while (more && !(r.NodeType == XmlNodeType.EndElement && r.Depth == depth))
        {
            if (r.NodeType == XmlNodeType.Element && r.LocalName == "rPh")
            {
                r.Skip();
                more = !r.EOF;
                continue;
            }
            if (r.NodeType == XmlNodeType.Element && r.LocalName == "t")
            {
                sb.Append(r.ReadElementContentAsString());
                more = !r.EOF;
                continue;
            }
            more = r.Read();
        }
        return Unescape(sb.ToString());
    }

    private static readonly Regex EscapeRegex = new("_x([0-9A-Fa-f]{4})_", RegexOptions.Compiled);

    private static string Unescape(string s)
    {
        if (s.IndexOf("_x", StringComparison.Ordinal) < 0) return s;
        return EscapeRegex.Replace(s, m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
    }

    // ---------- styles ----------

    /// <summary>Returns, for each cell format index (the "s" attribute), whether it is a date/time format.</summary>
    private static bool[] LoadDateStyles(Dictionary<string, ZipArchiveEntry> entries, Dictionary<string, Rel> rels)
    {
        var path = rels.Values.FirstOrDefault(r => r.Type.EndsWith("/styles", StringComparison.OrdinalIgnoreCase))?.Target
                   ?? "xl/styles.xml";
        var doc = LoadXml(entries, path);
        if (doc?.Root == null) return Array.Empty<bool>();

        var custom = new Dictionary<int, string>();
        foreach (var nf in doc.Descendants(Main + "numFmt"))
        {
            var idAttr = (string?)nf.Attribute("numFmtId");
            var code = (string?)nf.Attribute("formatCode");
            if (idAttr != null && code != null && int.TryParse(idAttr, out var id)) custom[id] = code;
        }

        var xfs = doc.Root.Element(Main + "cellXfs")?.Elements(Main + "xf").ToList();
        if (xfs == null) return Array.Empty<bool>();

        var result = new bool[xfs.Count];
        for (int i = 0; i < xfs.Count; i++)
        {
            int id = int.TryParse((string?)xfs[i].Attribute("numFmtId"), out var n) ? n : 0;
            result[i] = ExcelDate.IsBuiltInDateFormat(id) ||
                        (custom.TryGetValue(id, out var code) && ExcelDate.IsDateFormat(code));
        }
        return result;
    }

    // ---------- worksheet ----------

    private static void ReadSheet(Stream stream, Worksheet sheet, List<string> sst, bool[] dateStyles, bool date1904)
    {
        using var xr = XmlReader.Create(stream, Settings);
        int row = -1;
        int col = -1;

        while (xr.Read())
        {
            if (xr.NodeType != XmlNodeType.Element) continue;

            if (xr.LocalName == "row")
            {
                var r = xr.GetAttribute("r");
                row = r != null && int.TryParse(r, out var rn) ? rn - 1 : row + 1;
                col = -1;
            }
            else if (xr.LocalName == "c")
            {
                ReadCell(xr, sheet, sst, dateStyles, date1904, ref row, ref col);
            }
        }
    }

    private static void ReadCell(XmlReader xr, Worksheet sheet, List<string> sst, bool[] dateStyles, bool date1904,
        ref int row, ref int col)
    {
        var reference = xr.GetAttribute("r");
        var type = xr.GetAttribute("t");
        var styleAttr = xr.GetAttribute("s");

        if (reference != null)
        {
            try
            {
                var (rr, cc) = Worksheet.ParseReference(reference);
                row = rr;
                col = cc;
            }
            catch (FormatException)
            {
                col++;
            }
        }
        else
        {
            col++;
        }

        string? v = null;
        string? inline = null;

        if (!xr.IsEmptyElement)
        {
            int depth = xr.Depth;
            bool more = xr.Read();
            while (more && !(xr.NodeType == XmlNodeType.EndElement && xr.Depth == depth))
            {
                if (xr.NodeType == XmlNodeType.Element)
                {
                    if (xr.LocalName == "v")
                    {
                        v = xr.ReadElementContentAsString();
                        more = !xr.EOF;
                        continue;
                    }
                    if (xr.LocalName == "is")
                    {
                        inline = ReadTextContainer(xr);
                        more = xr.Read();
                        continue;
                    }
                    if (xr.LocalName == "f")
                    {
                        xr.Skip();
                        more = !xr.EOF;
                        continue;
                    }
                }
                more = xr.Read();
            }
        }

        if (row < 0 || row >= MaxRows || col < 0 || col >= MaxCols) return;

        object? value = null;
        switch (type)
        {
            case "s":
                if (int.TryParse(v, out var idx) && idx >= 0 && idx < sst.Count) value = sst[idx];
                break;
            case "str":
                value = v == null ? null : Unescape(v);
                break;
            case "inlineStr":
                value = inline ?? (v == null ? null : Unescape(v));
                break;
            case "b":
                value = v == "1" || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
                break;
            case "e":
                value = CellError.FromText(v ?? "#N/A");
                break;
            case "d":
                value = DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)
                    ? dt
                    : v;
                break;
            default:
                if (v != null && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
                {
                    bool isDate = styleAttr != null && int.TryParse(styleAttr, out var si) &&
                                  si >= 0 && si < dateStyles.Length && dateStyles[si];
                    value = isDate ? ExcelDate.FromSerial(num, date1904) : num;
                }
                else if (!string.IsNullOrEmpty(v))
                {
                    value = v;
                }
                break;
        }

        if (value != null) sheet.Set(row, col, value);
    }
}
