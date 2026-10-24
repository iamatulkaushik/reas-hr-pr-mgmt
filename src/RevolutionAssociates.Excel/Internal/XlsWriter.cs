using System.Buffers.Binary;
using System.Text;

namespace RevolutionAssociates.Excel;

/// <summary>Writes legacy Excel 97-2003 (BIFF8) workbooks (.xls). Supports values, dates, bold header row,
/// column widths and a frozen first row. Limits: 65,536 rows x 256 columns per sheet.</summary>
internal static class XlsWriter
{
    private const int MaxRows = 65_536;
    private const int MaxCols = 256;
    private const int MaxCellChars = 32_767;
    private const int MaxRecordBody = 8224;

    private const int RecBof = 0x0809;
    private const int RecEof = 0x000A;
    private const int RecContinue = 0x003C;
    private const int RecCodePage = 0x0042;
    private const int RecDateMode = 0x0022;
    private const int RecWindow1 = 0x003D;
    private const int RecFont = 0x0031;
    private const int RecFormat = 0x041E;
    private const int RecXf = 0x00E0;
    private const int RecStyle = 0x0293;
    private const int RecBoundSheet = 0x0085;
    private const int RecSst = 0x00FC;
    private const int RecDimensions = 0x0200;
    private const int RecColInfo = 0x007D;
    private const int RecWindow2 = 0x023E;
    private const int RecPane = 0x0041;
    private const int RecNumber = 0x0203;
    private const int RecLabelSst = 0x00FD;
    private const int RecBoolErr = 0x0205;

    // XF indexes: 0-14 style XFs, 15 default cell XF, then ours.
    private const int XfDefault = 15;
    private const int XfDate = 16;
    private const int XfDateTime = 17;
    private const int XfBold = 18;
    private const int XfBoldDate = 19;
    private const int XfBoldDateTime = 20;
    private const int XfText = 21;      // built-in Text format (@): string cells are formatted as text

    private const int FmtDateTime = 164;
    private const int FmtDate = 165;
    private const int FmtText = 49;     // built-in number format "@"

    public static void Write(Workbook workbook, Stream output)
    {
        var sheets = workbook.Sheets.Count > 0 ? workbook.Sheets : new List<Worksheet> { new("Sheet1") };

        // pass 1: validate limits and build the shared string table
        var sstIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var sstList = new List<string>();
        var cellCache = new List<List<(int Row, int Col, object Value)>>();
        int sstTotal = 0;
        var names = new List<string>();

        foreach (var sheet in sheets)
        {
            if (sheet.RowCount > MaxRows || sheet.ColumnCount > MaxCols)
                throw new InvalidOperationException(
                    $"Worksheet '{sheet.Name}' exceeds the .xls limit of {MaxRows} rows x {MaxCols} columns. Use .xlsx instead.");

            names.Add(UniqueName(Workbook.CleanSheetName(sheet.Name), names));

            var cells = sheet.Cells().ToList();
            cellCache.Add(cells);
            foreach (var (_, _, value) in cells)
            {
                if (value is not string s) continue;
                if (s.Length > MaxCellChars)
                    throw new InvalidOperationException(
                        $"A cell on worksheet '{sheet.Name}' is longer than {MaxCellChars} characters, which the .xls format cannot hold.");
                sstTotal++;
                if (!sstIndex.ContainsKey(s))
                {
                    sstIndex[s] = sstList.Count;
                    sstList.Add(s);
                }
            }
        }

        // pass 2: worksheet substreams
        var sheetBytes = new List<byte[]>();
        for (int i = 0; i < sheets.Count; i++)
            sheetBytes.Add(BuildSheet(sheets[i], cellCache[i], sstIndex, isFirst: i == 0));

        // pass 3: globals. Built twice because BOUNDSHEET records hold absolute offsets of the sheets.
        var offsets = new int[sheets.Count];
        var globals = BuildGlobals(names, offsets, sstList, sstTotal, sheets.Count);
        int next = globals.Length;
        for (int i = 0; i < sheets.Count; i++)
        {
            offsets[i] = next;
            next += sheetBytes[i].Length;
        }
        globals = BuildGlobals(names, offsets, sstList, sstTotal, sheets.Count);

        using var book = new MemoryStream();
        book.Write(globals);
        foreach (var s in sheetBytes) book.Write(s);

        CompoundFile.WriteSingleStream(output, "Workbook", book.ToArray());
    }

    // ---------- workbook globals ----------

    private static byte[] BuildGlobals(List<string> names, int[] sheetOffsets, List<string> sst, int sstTotal, int sheetCount)
    {
        using var ms = new MemoryStream();

        Bof(ms, 0x0005);
        Rec(ms, RecCodePage, U16(1200));         // UTF-16
        Rec(ms, RecDateMode, U16(0));            // 1900 date system

        // WINDOW1
        {
            var b = new byte[18];
            W16(b, 0, 0x01E0); W16(b, 2, 0x005A); W16(b, 4, 0x3FCF); W16(b, 6, 0x2A4E);
            W16(b, 8, 0x0038); W16(b, 10, 0); W16(b, 12, 0); W16(b, 14, 1); W16(b, 16, 600);
            Rec(ms, RecWindow1, b);
        }

        // fonts: 0 normal, 1 bold, 2 and 3 filler (index 4 does not exist in BIFF)
        for (int i = 0; i < 4; i++) Font(ms, bold: i == 1);

        // custom number formats
        NumberFormat(ms, FmtDateTime, "yyyy\\-mm\\-dd\\ hh:mm:ss");
        NumberFormat(ms, FmtDate, "yyyy\\-mm\\-dd");

        // 15 style XFs + default cell XF
        for (int i = 0; i < 15; i++) Xf(ms, font: 0, fmt: 0, style: true);
        Xf(ms, 0, 0, false);                         // 15 default
        Xf(ms, 0, FmtDate, false);                   // 16 date
        Xf(ms, 0, FmtDateTime, false);               // 17 date-time
        Xf(ms, 1, 0, false);                         // 18 bold
        Xf(ms, 1, FmtDate, false);                   // 19 bold date
        Xf(ms, 1, FmtDateTime, false);               // 20 bold date-time
        Xf(ms, 0, FmtText, false);                   // 21 text

        // STYLE: built-in "Normal"
        Rec(ms, RecStyle, new byte[] { 0x00, 0x80, 0x00, 0xFF });

        for (int i = 0; i < sheetCount; i++)
        {
            var name = names[i];
            bool wide = name.Any(c => c > 0xFF);
            var chars = wide ? Encoding.Unicode.GetBytes(name) : Encoding.Latin1.GetBytes(name);
            var b = new byte[8 + chars.Length];
            BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(0, 4), (uint)sheetOffsets[i]);
            b[4] = 0; // visible
            b[5] = 0; // worksheet
            b[6] = (byte)name.Length;
            b[7] = (byte)(wide ? 1 : 0);
            Buffer.BlockCopy(chars, 0, b, 8, chars.Length);
            Rec(ms, RecBoundSheet, b);
        }

        WriteSst(ms, sst, sstTotal);

        Rec(ms, RecEof, Array.Empty<byte>());
        return ms.ToArray();
    }

    private static void Bof(Stream s, int type)
    {
        var b = new byte[16];
        W16(b, 0, 0x0600);   // BIFF8
        W16(b, 2, type);     // 0x05 workbook globals, 0x10 worksheet
        W16(b, 4, 0x0DBB);   // build
        W16(b, 6, 0x07CC);   // year
        W16(b, 12, 0x0006);  // lowest Excel version
        Rec(s, RecBof, b);
    }

    private static void Font(Stream s, bool bold)
    {
        const string name = "Arial";
        // 14 fixed bytes, then ShortXLUnicodeString: cch(1), flags(1), chars
        var b = new byte[14 + 2 + name.Length];
        W16(b, 0, 200);                       // height: 10 pt in 1/20 pt units
        W16(b, 2, bold ? 0x0001 : 0x0000);    // grbit: bold flag
        W16(b, 4, 0x7FFF);                    // automatic colour
        W16(b, 6, bold ? 700 : 400);          // weight
        // 8: escapement, 10: underline, 11: family, 12: charset, 13: reserved - all zero
        b[14] = (byte)name.Length;
        b[15] = 0;                            // compressed characters
        Buffer.BlockCopy(Encoding.Latin1.GetBytes(name), 0, b, 16, name.Length);
        Rec(s, RecFont, b);
    }

    private static void NumberFormat(Stream s, int id, string code)
    {
        var chars = Encoding.Latin1.GetBytes(code);
        var b = new byte[5 + chars.Length];
        W16(b, 0, id);
        W16(b, 2, code.Length);
        b[4] = 0;
        Buffer.BlockCopy(chars, 0, b, 5, chars.Length);
        Rec(s, RecFormat, b);
    }

    private static void Xf(Stream s, int font, int fmt, bool style)
    {
        var b = new byte[20];
        W16(b, 0, font);
        W16(b, 2, fmt);
        if (style)
        {
            W16(b, 4, 0xFFF5);   // locked, style XF, parent = 0xFFF
            b[9] = 0xF4;         // attributes: all "used"
        }
        else
        {
            W16(b, 4, 0x0001);   // locked, cell XF, parent = style XF 0
            byte used = 0;
            if (fmt != 0) used |= 0x04;
            if (font != 0) used |= 0x08;
            b[9] = used;
        }
        W16(b, 18, 0x20C0);      // pattern colours: automatic foreground / background
        Rec(s, RecXf, b);
    }

    // ---------- shared string table (with CONTINUE records) ----------

    private static void WriteSst(Stream output, List<string> strings, int total)
    {
        var buf = new MemoryStream();
        bool first = true;

        void Flush()
        {
            Rec(output, first ? RecSst : RecContinue, buf.ToArray());
            first = false;
            buf = new MemoryStream();
        }

        var head = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(head.AsSpan(0, 4), (uint)total);
        BinaryPrimitives.WriteUInt32LittleEndian(head.AsSpan(4, 4), (uint)strings.Count);
        buf.Write(head);

        foreach (var s in strings)
        {
            bool wide = s.Any(c => c > 0xFF);
            int unit = wide ? 2 : 1;

            // never split the 3-byte string header or leave no room for one character
            if (buf.Length + 3 + unit > MaxRecordBody) Flush();

            var hdr = new byte[3];
            W16(hdr, 0, s.Length);
            hdr[2] = (byte)(wide ? 1 : 0);
            buf.Write(hdr);

            int index = 0;
            while (index < s.Length)
            {
                int room = (MaxRecordBody - (int)buf.Length) / unit;
                int take = Math.Min(room, s.Length - index);
                var part = s.Substring(index, take);
                buf.Write(wide ? Encoding.Unicode.GetBytes(part) : Encoding.Latin1.GetBytes(part));
                index += take;

                if (index < s.Length)
                {
                    Flush();
                    buf.WriteByte((byte)(wide ? 1 : 0)); // continuation repeats the encoding flag
                }
            }
        }

        Flush();
    }

    // ---------- worksheet ----------

    private static byte[] BuildSheet(Worksheet sheet, List<(int Row, int Col, object Value)> cells,
        Dictionary<string, int> sst, bool isFirst)
    {
        using var ms = new MemoryStream();
        Bof(ms, 0x0010);

        foreach (var kv in sheet.ColumnWidths.OrderBy(k => k.Key))
        {
            if (kv.Key < 0 || kv.Key >= MaxCols) continue;
            var b = new byte[12];
            W16(b, 0, kv.Key);
            W16(b, 2, kv.Key);
            W16(b, 4, (int)Math.Clamp(Math.Round(kv.Value * 256.0), 0, 65535));
            W16(b, 6, XfDefault);
            Rec(ms, RecColInfo, b);
        }

        // DIMENSIONS: first row, last row + 1, first col, last col + 1
        {
            var b = new byte[14];
            BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(0, 4), 0);
            BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(4, 4), (uint)Math.Max(sheet.RowCount, 1));
            W16(b, 8, 0);
            W16(b, 10, Math.Max(sheet.ColumnCount, 1));
            Rec(ms, RecDimensions, b);
        }

        foreach (var (row, col, value) in cells)
            WriteCell(ms, row, col, value, sst, bold: sheet.BoldFirstRow && row == 0);

        // WINDOW2 (+ PANE when the first row is frozen)
        {
            int grbit = 0x06B6;
            if (!isFirst) grbit &= ~0x0200;          // only the first sheet is selected
            if (sheet.FreezeFirstRow) grbit |= 0x0008 | 0x0100;
            var b = new byte[18];
            W16(b, 0, grbit);
            W16(b, 6, 0x40);
            Rec(ms, RecWindow2, b);

            if (sheet.FreezeFirstRow)
            {
                var p = new byte[10];
                W16(p, 2, 1);   // y split: one row
                W16(p, 4, 1);   // top visible row of the lower pane
                p[8] = 2;       // active pane: bottom-left
                Rec(ms, RecPane, p);
            }
        }

        Rec(ms, RecEof, Array.Empty<byte>());
        return ms.ToArray();
    }

    private static void WriteCell(Stream s, int row, int col, object value, Dictionary<string, int> sst, bool bold)
    {
        switch (value)
        {
            case string str:
                WriteLabel(s, row, col, bold, sst[Truncate(str)]);
                break;
            case double d when double.IsNaN(d) || double.IsInfinity(d):
                WriteError(s, row, col, CellError.Num.ToBiff(), bold);
                break;
            case double d:
                WriteNumber(s, row, col, bold ? XfBold : XfDefault, d);
                break;
            case bool bo:
            {
                var b = new byte[8];
                W16(b, 0, row); W16(b, 2, col); W16(b, 4, bold ? XfBold : XfDefault);
                b[6] = (byte)(bo ? 1 : 0);
                Rec(s, RecBoolErr, b);
                break;
            }
            case DateTime dt:
            {
                bool dateOnly = dt.TimeOfDay == TimeSpan.Zero;
                int xf = dateOnly ? (bold ? XfBoldDate : XfDate) : (bold ? XfBoldDateTime : XfDateTime);
                // a time-of-day value (from TimeOnly) lives on DateTime.MinValue: store just the fraction of a day
                double serial = dt.Year <= 1 ? dt.TimeOfDay.TotalDays : ExcelDate.ToSerial(dt, date1904: false);
                WriteNumber(s, row, col, xf, serial);
                break;
            }
            case CellError e:
                WriteError(s, row, col, e.ToBiff(), bold);
                break;
            default:
            {
                var text = Truncate(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
                if (text.Length == 0) break;
                WriteLabel(s, row, col, bold, sst.TryGetValue(text, out var idx) ? idx : 0);
                break;
            }
        }
    }

    private static void WriteLabel(Stream s, int row, int col, bool bold, int sstIndex)
    {
        var b = new byte[10];
        W16(b, 0, row); W16(b, 2, col); W16(b, 4, bold ? XfBold : XfText);
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(6, 4), (uint)sstIndex);
        Rec(s, RecLabelSst, b);
    }

    private static void WriteNumber(Stream s, int row, int col, int xf, double value)
    {
        var b = new byte[14];
        W16(b, 0, row); W16(b, 2, col); W16(b, 4, xf);
        BinaryPrimitives.WriteDoubleLittleEndian(b.AsSpan(6, 8), value);
        Rec(s, RecNumber, b);
    }

    private static void WriteError(Stream s, int row, int col, byte code, bool bold)
    {
        var b = new byte[8];
        W16(b, 0, row); W16(b, 2, col); W16(b, 4, bold ? XfBold : XfDefault);
        b[6] = code;
        b[7] = 1; // error
        Rec(s, RecBoolErr, b);
    }

    // ---------- helpers ----------

    private static string Truncate(string s) => s.Length > MaxCellChars ? s[..MaxCellChars] : s;

    private static string UniqueName(string name, List<string> existing)
    {
        var result = name;
        int n = 2;
        while (existing.Any(e => string.Equals(e, result, StringComparison.OrdinalIgnoreCase)))
        {
            var suffix = "_" + n++;
            result = (name.Length + suffix.Length > 31 ? name[..(31 - suffix.Length)] : name) + suffix;
        }
        return result;
    }

    private static void Rec(Stream s, int id, byte[] body)
    {
        if (body.Length > MaxRecordBody)
            throw new InvalidOperationException($"BIFF record 0x{id:X4} is too large ({body.Length} bytes).");
        var head = new byte[4];
        BinaryPrimitives.WriteUInt16LittleEndian(head.AsSpan(0, 2), (ushort)id);
        BinaryPrimitives.WriteUInt16LittleEndian(head.AsSpan(2, 2), (ushort)body.Length);
        s.Write(head, 0, 4);
        s.Write(body, 0, body.Length);
    }

    private static byte[] U16(int v)
    {
        var b = new byte[2];
        W16(b, 0, v);
        return b;
    }

    private static void W16(byte[] d, int o, int v) =>
        BinaryPrimitives.WriteUInt16LittleEndian(d.AsSpan(o, 2), (ushort)v);
}
