using System.Globalization;
using System.Text;

namespace RevolutionAssociates.Excel;

/// <summary>CSV / TSV reading and writing (RFC 4180 style).</summary>
internal static class CsvFormat
{
    private static readonly char[] CandidateDelimiters = { ',', ';', '\t', '|' };

    // ---------- reading ----------

    public static Workbook ReadWorkbook(byte[] data, string sheetName, char? delimiter)
    {
        var text = DecodeText(data);
        var wb = new Workbook();
        var sheet = wb.AddSheet(string.IsNullOrWhiteSpace(sheetName) ? "Sheet1" : sheetName);

        char delim = delimiter ?? DetectDelimiter(text);
        int row = 0;
        foreach (var fields in ParseRecords(text, delim))
        {
            for (int c = 0; c < fields.Count; c++)
            {
                var f = fields[c];
                if (f.Length == 0) continue;
                sheet.Set(row, c, InferValue(f));
            }
            row++;
        }
        return wb;
    }

    private static string DecodeText(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            return Encoding.UTF8.GetString(data, 3, data.Length - 3);
        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
            return Encoding.Unicode.GetString(data, 2, data.Length - 2);
        if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(data, 2, data.Length - 2);

        try
        {
            return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(data);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(data);
        }
    }

    private static char DetectDelimiter(string text)
    {
        // Count candidates in the first non-empty line, ignoring quoted sections.
        int best = 0;
        char bestChar = ',';
        var counts = new int[CandidateDelimiters.Length];
        bool inQuote = false;
        bool sawContent = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"') { inQuote = !inQuote; sawContent = true; continue; }
            if (inQuote) continue;
            if (c == '\n' || c == '\r')
            {
                if (sawContent) break;
                continue;
            }
            sawContent = true;
            int idx = Array.IndexOf(CandidateDelimiters, c);
            if (idx >= 0) counts[idx]++;
        }

        for (int i = 0; i < counts.Length; i++)
        {
            if (counts[i] > best)
            {
                best = counts[i];
                bestChar = CandidateDelimiters[i];
            }
        }
        return bestChar;
    }

    private static IEnumerable<List<string>> ParseRecords(string text, char delim)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuote = false;
        bool fieldStarted = false;
        int i = 0;

        while (i < text.Length)
        {
            char c = text[i];

            if (inQuote)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        sb.Append('"');
                        i += 2;
                        continue;
                    }
                    inQuote = false;
                    i++;
                    continue;
                }
                sb.Append(c);
                i++;
                continue;
            }

            if (c == '"' && sb.Length == 0 && !fieldStarted)
            {
                inQuote = true;
                fieldStarted = true;
                i++;
            }
            else if (c == delim)
            {
                fields.Add(sb.ToString());
                sb.Clear();
                fieldStarted = false;
                i++;
            }
            else if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                i++;
                fields.Add(sb.ToString());
                sb.Clear();
                fieldStarted = false;
                yield return fields;
                fields = new List<string>();
            }
            else
            {
                sb.Append(c);
                fieldStarted = true;
                i++;
            }
        }

        if (sb.Length > 0 || fields.Count > 0 || fieldStarted)
        {
            fields.Add(sb.ToString());
            yield return fields;
        }
    }

    /// <summary>Converts raw text into a typed cell value (number, bool, date, or text).</summary>
    internal static object InferValue(string text)
    {
        var t = text.Trim();
        if (t.Length == 0) return text;

        if (t.Equals("TRUE", StringComparison.OrdinalIgnoreCase)) return true;
        if (t.Equals("FALSE", StringComparison.OrdinalIgnoreCase)) return false;

        // keep identifiers such as 00123 or +91... as text
        bool leadingZero = t.Length > 1 && t[0] == '0' && char.IsDigit(t[1]);
        bool leadingPlus = t[0] == '+';
        if (!leadingZero && !leadingPlus && t.Length <= 15 && LooksNumeric(t) &&
            double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) &&
            !double.IsNaN(d) && !double.IsInfinity(d))
            return d;

        if (t.Length is 10 or 16 or 19 &&
            DateTime.TryParseExact(t,
                new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        return text;
    }

    private static bool LooksNumeric(string t)
    {
        bool digit = false;
        foreach (char c in t)
        {
            if (char.IsDigit(c)) digit = true;
            else if (c is '.' or '-' or 'e' or 'E' or '+') { }
            else return false;
        }
        return digit;
    }

    // ---------- writing ----------

    public static void Write(Worksheet sheet, Stream stream, char delimiter)
    {
        using var writer = new StreamWriter(stream, new UTF8Encoding(true), 4096, leaveOpen: true)
        {
            NewLine = "\r\n"
        };

        for (int r = 0; r < sheet.RowCount; r++)
        {
            for (int c = 0; c < sheet.ColumnCount; c++)
            {
                if (c > 0) writer.Write(delimiter);
                writer.Write(Escape(sheet.GetString(r, c), delimiter));
            }
            writer.WriteLine();
        }
        writer.Flush();
    }

    private static string Escape(string value, char delimiter)
    {
        if (value.Length == 0) return value;
        bool needsQuote = value.Contains(delimiter) || value.Contains('"') || value.Contains('\n') ||
                          value.Contains('\r') || value[0] == ' ' || value[^1] == ' ';
        return needsQuote ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
    }
}
