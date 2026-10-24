using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace RevolutionAssociates.Excel;

/// <summary>Writes OpenXML workbooks (.xlsx).</summary>
internal static class XlsxWriter
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PkgRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string ContentTypesNs = "http://schemas.openxmlformats.org/package/2006/content-types";

    private const int MaxRows = 1_048_576;
    private const int MaxCols = 16_384;
    private const int MaxCellChars = 32_767;

    // cellXfs indexes defined in styles.xml below
    private const int StyleDefault = 0;
    private const int StyleDate = 1;
    private const int StyleDateTime = 2;
    private const int StyleBold = 3;

    public static void Write(Workbook workbook, Stream output)
    {
        var sheets = PrepareSheets(workbook);

        // pass 1: shared string table
        var sst = new Dictionary<string, int>(StringComparer.Ordinal);
        var sstList = new List<string>();
        var cellCache = new List<List<(int Row, int Col, object Value)>>();
        foreach (var sheet in sheets)
        {
            if (sheet.RowCount > MaxRows || sheet.ColumnCount > MaxCols)
                throw new InvalidOperationException(
                    $"Worksheet '{sheet.Name}' exceeds the .xlsx limit of {MaxRows} rows x {MaxCols} columns.");

            var cells = sheet.Cells().ToList();
            cellCache.Add(cells);
            foreach (var (_, _, value) in cells)
            {
                if (value is string s)
                {
                    s = Truncate(s);
                    if (!sst.ContainsKey(s))
                    {
                        sst[s] = sstList.Count;
                        sstList.Add(s);
                    }
                }
            }
        }

        using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);

        WritePart(zip, "[Content_Types].xml", w => WriteContentTypes(w, sheets.Count));
        WritePart(zip, "_rels/.rels", WriteRootRels);
        WritePart(zip, "xl/workbook.xml", w => WriteWorkbook(w, sheets));
        WritePart(zip, "xl/_rels/workbook.xml.rels", w => WriteWorkbookRels(w, sheets.Count));
        WritePart(zip, "xl/styles.xml", WriteStyles);
        WritePart(zip, "xl/sharedStrings.xml", w => WriteSharedStrings(w, sstList));

        for (int i = 0; i < sheets.Count; i++)
        {
            int index = i;
            WritePart(zip, $"xl/worksheets/sheet{i + 1}.xml",
                w => WriteSheet(w, sheets[index], cellCache[index], sst, isFirst: index == 0));
        }
    }

    // ---------- helpers ----------

    private static List<Worksheet> PrepareSheets(Workbook workbook)
    {
        var sheets = new List<Worksheet>(workbook.Sheets);
        if (sheets.Count == 0) sheets.Add(new Worksheet("Sheet1"));

        // Excel requires valid, unique sheet names.
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sheet in sheets)
        {
            var baseName = Workbook.CleanSheetName(sheet.Name);
            var name = baseName;
            int n = 2;
            while (!used.Add(name))
            {
                var suffix = "_" + n++;
                name = (baseName.Length + suffix.Length > 31 ? baseName[..(31 - suffix.Length)] : baseName) + suffix;
            }
            if (!string.Equals(name, sheet.Name, StringComparison.Ordinal)) sheet.Name = name;
        }
        return sheets;
    }

    private static void WritePart(ZipArchive zip, string name, Action<XmlWriter> body)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            CloseOutput = false
        };
        using var w = XmlWriter.Create(stream, settings);
        w.WriteStartDocument(true);
        body(w);
        w.WriteEndDocument();
        w.Flush();
    }

    private static string Truncate(string s) => s.Length > MaxCellChars ? s[..MaxCellChars] : s;

    /// <summary>Escapes characters XML 1.0 cannot carry using Excel's _xHHHH_ convention.</summary>
    private static string EscapeText(string s)
    {
        StringBuilder? sb = null;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            bool bad = c < 0x20 && c != '\t' && c != '\n' && c != '\r' || c == 0xFFFE || c == 0xFFFF;
            // lone surrogates are invalid too
            if (!bad && char.IsHighSurrogate(c) && !(i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))) bad = true;
            if (!bad && char.IsLowSurrogate(c) && !(i > 0 && char.IsHighSurrogate(s[i - 1]))) bad = true;
            // a literal "_x0041_" in user text must be escaped so it survives a round trip
            if (!bad && c == '_' && i + 6 < s.Length + 0 && IsEscapeSequence(s, i)) bad = true;

            if (bad)
            {
                sb ??= new StringBuilder(s, 0, i, s.Length + 16);
                if (c == '_') sb.Append("_x005F_");
                else sb.Append("_x").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture)).Append('_');
            }
            else
            {
                sb?.Append(c);
            }
        }
        return sb?.ToString() ?? s;
    }

    private static bool IsEscapeSequence(string s, int i)
    {
        if (i + 6 >= s.Length || s[i + 1] != 'x' && s[i + 1] != 'X') return false;
        for (int k = 2; k <= 5; k++)
            if (!Uri.IsHexDigit(s[i + k])) return false;
        return s[i + 6] == '_';
    }

    // ---------- static parts ----------

    private static void WriteContentTypes(XmlWriter w, int sheetCount)
    {
        w.WriteStartElement("Types", ContentTypesNs);
        Default(w, "rels", "application/vnd.openxmlformats-package.relationships+xml");
        Default(w, "xml", "application/xml");
        Override(w, "/xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
        for (int i = 1; i <= sheetCount; i++)
            Override(w, $"/xl/worksheets/sheet{i}.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
        Override(w, "/xl/styles.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
        Override(w, "/xl/sharedStrings.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml");
        w.WriteEndElement();

        static void Default(XmlWriter x, string ext, string type)
        {
            x.WriteStartElement("Default", ContentTypesNs);
            x.WriteAttributeString("Extension", ext);
            x.WriteAttributeString("ContentType", type);
            x.WriteEndElement();
        }

        static void Override(XmlWriter x, string part, string type)
        {
            x.WriteStartElement("Override", ContentTypesNs);
            x.WriteAttributeString("PartName", part);
            x.WriteAttributeString("ContentType", type);
            x.WriteEndElement();
        }
    }

    private static void Relationship(XmlWriter w, string id, string type, string target)
    {
        w.WriteStartElement("Relationship", PkgRelNs);
        w.WriteAttributeString("Id", id);
        w.WriteAttributeString("Type", type);
        w.WriteAttributeString("Target", target);
        w.WriteEndElement();
    }

    private static void WriteRootRels(XmlWriter w)
    {
        w.WriteStartElement("Relationships", PkgRelNs);
        Relationship(w, "rId1", RelNs + "/officeDocument", "xl/workbook.xml");
        w.WriteEndElement();
    }

    private static void WriteWorkbook(XmlWriter w, List<Worksheet> sheets)
    {
        w.WriteStartElement("workbook", Main);
        w.WriteAttributeString("xmlns", "r", null, RelNs);

        w.WriteStartElement("bookViews", Main);
        w.WriteStartElement("workbookView", Main);
        w.WriteEndElement();
        w.WriteEndElement();

        w.WriteStartElement("sheets", Main);
        for (int i = 0; i < sheets.Count; i++)
        {
            w.WriteStartElement("sheet", Main);
            w.WriteAttributeString("name", sheets[i].Name);
            w.WriteAttributeString("sheetId", (i + 1).ToString(CultureInfo.InvariantCulture));
            w.WriteAttributeString("id", RelNs, "rId" + (i + 1).ToString(CultureInfo.InvariantCulture));
            w.WriteEndElement();
        }
        w.WriteEndElement();

        w.WriteEndElement();
    }

    private static void WriteWorkbookRels(XmlWriter w, int sheetCount)
    {
        w.WriteStartElement("Relationships", PkgRelNs);
        for (int i = 1; i <= sheetCount; i++)
            Relationship(w, "rId" + i.ToString(CultureInfo.InvariantCulture), RelNs + "/worksheet", $"worksheets/sheet{i}.xml");
        Relationship(w, "rId" + (sheetCount + 1).ToString(CultureInfo.InvariantCulture), RelNs + "/styles", "styles.xml");
        Relationship(w, "rId" + (sheetCount + 2).ToString(CultureInfo.InvariantCulture), RelNs + "/sharedStrings", "sharedStrings.xml");
        w.WriteEndElement();
    }

    private static void WriteStyles(XmlWriter w)
    {
        w.WriteStartElement("styleSheet", Main);

        w.WriteStartElement("numFmts", Main);
        w.WriteAttributeString("count", "2");
        NumFmt(w, 164, "yyyy\\-mm\\-dd");
        NumFmt(w, 165, "yyyy\\-mm\\-dd\\ hh:mm:ss");
        w.WriteEndElement();

        w.WriteStartElement("fonts", Main);
        w.WriteAttributeString("count", "2");
        Font(w, bold: false);
        Font(w, bold: true);
        w.WriteEndElement();

        w.WriteStartElement("fills", Main);
        w.WriteAttributeString("count", "2");
        Fill(w, "none");
        Fill(w, "gray125");
        w.WriteEndElement();

        w.WriteStartElement("borders", Main);
        w.WriteAttributeString("count", "1");
        w.WriteStartElement("border", Main);
        foreach (var side in new[] { "left", "right", "top", "bottom", "diagonal" })
        {
            w.WriteStartElement(side, Main);
            w.WriteEndElement();
        }
        w.WriteEndElement();
        w.WriteEndElement();

        w.WriteStartElement("cellStyleXfs", Main);
        w.WriteAttributeString("count", "1");
        Xf(w, 0, 0, includeXfId: false);
        w.WriteEndElement();

        w.WriteStartElement("cellXfs", Main);
        w.WriteAttributeString("count", "4");
        Xf(w, 0, 0, includeXfId: true);      // 0 default
        Xf(w, 164, 0, includeXfId: true);    // 1 date
        Xf(w, 165, 0, includeXfId: true);    // 2 date-time
        Xf(w, 0, 1, includeXfId: true);      // 3 bold
        w.WriteEndElement();

        w.WriteStartElement("cellStyles", Main);
        w.WriteAttributeString("count", "1");
        w.WriteStartElement("cellStyle", Main);
        w.WriteAttributeString("name", "Normal");
        w.WriteAttributeString("xfId", "0");
        w.WriteAttributeString("builtinId", "0");
        w.WriteEndElement();
        w.WriteEndElement();

        w.WriteEndElement();

        static void NumFmt(XmlWriter x, int id, string code)
        {
            x.WriteStartElement("numFmt", Main);
            x.WriteAttributeString("numFmtId", id.ToString(CultureInfo.InvariantCulture));
            x.WriteAttributeString("formatCode", code);
            x.WriteEndElement();
        }

        static void Font(XmlWriter x, bool bold)
        {
            x.WriteStartElement("font", Main);
            if (bold)
            {
                x.WriteStartElement("b", Main);
                x.WriteEndElement();
            }
            x.WriteStartElement("sz", Main);
            x.WriteAttributeString("val", "11");
            x.WriteEndElement();
            x.WriteStartElement("name", Main);
            x.WriteAttributeString("val", "Calibri");
            x.WriteEndElement();
            x.WriteEndElement();
        }

        static void Fill(XmlWriter x, string pattern)
        {
            x.WriteStartElement("fill", Main);
            x.WriteStartElement("patternFill", Main);
            x.WriteAttributeString("patternType", pattern);
            x.WriteEndElement();
            x.WriteEndElement();
        }

        static void Xf(XmlWriter x, int numFmtId, int fontId, bool includeXfId)
        {
            x.WriteStartElement("xf", Main);
            x.WriteAttributeString("numFmtId", numFmtId.ToString(CultureInfo.InvariantCulture));
            x.WriteAttributeString("fontId", fontId.ToString(CultureInfo.InvariantCulture));
            x.WriteAttributeString("fillId", "0");
            x.WriteAttributeString("borderId", "0");
            if (includeXfId) x.WriteAttributeString("xfId", "0");
            if (numFmtId != 0) x.WriteAttributeString("applyNumberFormat", "1");
            if (fontId != 0) x.WriteAttributeString("applyFont", "1");
            x.WriteEndElement();
        }
    }

    private static void WriteSharedStrings(XmlWriter w, List<string> strings)
    {
        w.WriteStartElement("sst", Main);
        w.WriteAttributeString("count", strings.Count.ToString(CultureInfo.InvariantCulture));
        w.WriteAttributeString("uniqueCount", strings.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var s in strings)
        {
            w.WriteStartElement("si", Main);
            w.WriteStartElement("t", Main);
            if (s.Length > 0 && (char.IsWhiteSpace(s[0]) || char.IsWhiteSpace(s[^1]) || s.Contains('\n')))
                w.WriteAttributeString("xml", "space", null, "preserve");
            w.WriteString(EscapeText(s));
            w.WriteEndElement();
            w.WriteEndElement();
        }
        w.WriteEndElement();
    }

    // ---------- worksheet ----------

    private static void WriteSheet(XmlWriter w, Worksheet sheet, List<(int Row, int Col, object Value)> cells,
        Dictionary<string, int> sst, bool isFirst)
    {
        w.WriteStartElement("worksheet", Main);

        if (sheet.RowCount > 0 && sheet.ColumnCount > 0)
        {
            w.WriteStartElement("dimension", Main);
            w.WriteAttributeString("ref", "A1:" + Worksheet.ToReference(sheet.RowCount - 1, sheet.ColumnCount - 1));
            w.WriteEndElement();
        }

        w.WriteStartElement("sheetViews", Main);
        w.WriteStartElement("sheetView", Main);
        if (isFirst) w.WriteAttributeString("tabSelected", "1");
        w.WriteAttributeString("workbookViewId", "0");
        if (sheet.FreezeFirstRow)
        {
            w.WriteStartElement("pane", Main);
            w.WriteAttributeString("ySplit", "1");
            w.WriteAttributeString("topLeftCell", "A2");
            w.WriteAttributeString("activePane", "bottomLeft");
            w.WriteAttributeString("state", "frozen");
            w.WriteEndElement();
            w.WriteStartElement("selection", Main);
            w.WriteAttributeString("pane", "bottomLeft");
            w.WriteEndElement();
        }
        w.WriteEndElement();
        w.WriteEndElement();

        w.WriteStartElement("sheetFormatPr", Main);
        w.WriteAttributeString("defaultRowHeight", "15");
        w.WriteEndElement();

        var widths = sheet.ColumnWidths.Where(kv => kv.Key >= 0 && kv.Key < MaxCols && kv.Value > 0)
            .OrderBy(kv => kv.Key).ToList();
        if (widths.Count > 0)
        {
            w.WriteStartElement("cols", Main);
            foreach (var (col, width) in widths)
            {
                w.WriteStartElement("col", Main);
                var idx = (col + 1).ToString(CultureInfo.InvariantCulture);
                w.WriteAttributeString("min", idx);
                w.WriteAttributeString("max", idx);
                w.WriteAttributeString("width", Math.Min(width, 255).ToString("R", CultureInfo.InvariantCulture));
                w.WriteAttributeString("customWidth", "1");
                w.WriteEndElement();
            }
            w.WriteEndElement();
        }

        w.WriteStartElement("sheetData", Main);
        int currentRow = -1;
        foreach (var (row, col, value) in cells)
        {
            if (row != currentRow)
            {
                if (currentRow >= 0) w.WriteEndElement();
                w.WriteStartElement("row", Main);
                w.WriteAttributeString("r", (row + 1).ToString(CultureInfo.InvariantCulture));
                currentRow = row;
            }
            WriteCell(w, row, col, value, sst, bold: sheet.BoldFirstRow && row == 0);
        }
        if (currentRow >= 0) w.WriteEndElement();
        w.WriteEndElement();

        w.WriteEndElement();
    }

    private static void WriteCell(XmlWriter w, int row, int col, object value, Dictionary<string, int> sst, bool bold)
    {
        w.WriteStartElement("c", Main);
        w.WriteAttributeString("r", Worksheet.ToReference(row, col));

        switch (value)
        {
            case string s:
                if (bold) w.WriteAttributeString("s", StyleBold.ToString(CultureInfo.InvariantCulture));
                w.WriteAttributeString("t", "s");
                w.WriteElementString("v", Main, sst[Truncate(s)].ToString(CultureInfo.InvariantCulture));
                break;

            case double d:
                if (double.IsNaN(d) || double.IsInfinity(d))
                {
                    w.WriteAttributeString("t", "e");
                    w.WriteElementString("v", Main, "#NUM!");
                }
                else
                {
                    if (bold) w.WriteAttributeString("s", StyleBold.ToString(CultureInfo.InvariantCulture));
                    w.WriteElementString("v", Main, d.ToString("R", CultureInfo.InvariantCulture));
                }
                break;

            case bool b:
                w.WriteAttributeString("t", "b");
                w.WriteElementString("v", Main, b ? "1" : "0");
                break;

            case DateTime dt:
                var style = dt.TimeOfDay == TimeSpan.Zero ? StyleDate : StyleDateTime;
                w.WriteAttributeString("s", style.ToString(CultureInfo.InvariantCulture));
                w.WriteElementString("v", Main,
                    ExcelDate.ToSerial(dt, date1904: false).ToString("R", CultureInfo.InvariantCulture));
                break;

            case CellError e:
                w.WriteAttributeString("t", "e");
                w.WriteElementString("v", Main, e.Text);
                break;

            default:
                w.WriteAttributeString("t", "s");
                var text = Truncate(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
                if (!sst.TryGetValue(text, out var idx)) idx = 0;
                w.WriteElementString("v", Main, idx.ToString(CultureInfo.InvariantCulture));
                break;
        }

        w.WriteEndElement();
    }
}
