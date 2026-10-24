using System.Buffers.Binary;
using System.Text;

namespace RevolutionAssociates.Excel;

/// <summary>Reads legacy Excel 97-2003 (BIFF8) workbooks (.xls).</summary>
internal static class XlsReader
{
    private const int RecBof = 0x0809;
    private const int RecEof = 0x000A;
    private const int RecContinue = 0x003C;
    private const int RecFilePass = 0x002F;
    private const int RecDateMode = 0x0022;
    private const int RecBoundSheet = 0x0085;
    private const int RecSst = 0x00FC;
    private const int RecFormat = 0x041E;
    private const int RecXf = 0x00E0;
    private const int RecLabelSst = 0x00FD;
    private const int RecNumber = 0x0203;
    private const int RecRk = 0x027E;
    private const int RecMulRk = 0x00BD;
    private const int RecLabel = 0x0204;
    private const int RecBoolErr = 0x0205;
    private const int RecFormula = 0x0006;
    private const int RecString = 0x0207;

    public static Workbook Read(byte[] file)
    {
        var stream = CompoundFile.ReadStream(file, "Workbook", "Book")
                     ?? throw new InvalidDataException("No 'Workbook' stream found; the file is not a BIFF8 .xls workbook.");

        var wb = new Workbook();
        var sheetInfos = new List<(string Name, int Offset, byte Type)>();
        var sst = new List<string>();
        var customFormats = new Dictionary<int, string>();
        var xfFormat = new List<int>();

        // ---------- workbook globals ----------
        int pos = 0;
        int depth = 0;
        bool started = false;
        while (TryReadHeader(stream, pos, out int id, out int len))
        {
            int body = pos + 4;
            if (body + len > stream.Length) len = stream.Length - body;

            if (id == RecBof)
            {
                if (len >= 2 && U16(stream, body) == 0x0500)
                    throw new NotSupportedException("BIFF5 (Excel 5/95) workbooks are not supported; save as .xlsx or Excel 97-2003.");
                depth++;
                started = true;
            }
            else if (id == RecEof)
            {
                depth--;
                if (started && depth <= 0) break;
            }
            else if (started && depth == 1)
            {
                switch (id)
                {
                    case RecFilePass:
                        throw new NotSupportedException("Password-protected .xls workbooks are not supported.");
                    case RecDateMode:
                        wb.Date1904 = len >= 2 && U16(stream, body) == 1;
                        break;
                    case RecBoundSheet when len >= 8:
                    {
                        int offset = (int)U32(stream, body);
                        byte type = stream[body + 5];
                        int cch = stream[body + 6];
                        byte flags = stream[body + 7];
                        int p = body + 8;
                        var name = ReadChars(stream, ref p, cch, (flags & 1) != 0, body + len);
                        sheetInfos.Add((name, offset, type));
                        break;
                    }
                    case RecSst:
                    {
                        var segments = new List<byte[]> { Slice(stream, body, len) };
                        int next = body + len;
                        // gather CONTINUE records that belong to the SST
                        while (TryReadHeader(stream, next, out int cid, out int clen) && cid == RecContinue)
                        {
                            int cb = next + 4;
                            if (cb + clen > stream.Length) clen = stream.Length - cb;
                            segments.Add(Slice(stream, cb, clen));
                            next = cb + clen;
                        }
                        ParseSst(segments, sst);
                        pos = next;
                        continue;
                    }
                    case RecFormat when len >= 5:
                    {
                        int idx = U16(stream, body);
                        int cch = U16(stream, body + 2);
                        byte flags = stream[body + 4];
                        int p = body + 5;
                        customFormats[idx] = ReadChars(stream, ref p, cch, (flags & 1) != 0, body + len);
                        break;
                    }
                    case RecXf when len >= 4:
                        xfFormat.Add(U16(stream, body + 2));
                        break;
                }
            }

            pos = body + len;
        }

        var dateXf = new bool[xfFormat.Count];
        for (int i = 0; i < dateXf.Length; i++)
        {
            int fmt = xfFormat[i];
            dateXf[i] = ExcelDate.IsBuiltInDateFormat(fmt) ||
                        (customFormats.TryGetValue(fmt, out var code) && ExcelDate.IsDateFormat(code));
        }

        // ---------- worksheets ----------
        foreach (var info in sheetInfos)
        {
            if (info.Type != 0) continue; // skip charts, macro sheets, VBA modules
            var sheet = wb.AddSheet(info.Name);
            if (info.Offset >= 0 && info.Offset < stream.Length)
                ReadSheet(stream, info.Offset, sheet, sst, dateXf, wb.Date1904);
        }

        return wb;
    }

    // ---------- worksheet records ----------

    private static void ReadSheet(byte[] s, int start, Worksheet sheet, List<string> sst, bool[] dateXf, bool date1904)
    {
        int pos = start;
        int depth = 0;
        (int Row, int Col)? pendingString = null;

        while (TryReadHeader(s, pos, out int id, out int len))
        {
            int b = pos + 4;
            if (b + len > s.Length) len = s.Length - b;

            switch (id)
            {
                case RecBof:
                    depth++;
                    break;
                case RecEof:
                    depth--;
                    if (depth <= 0) return;
                    break;

                case RecLabelSst when len >= 10:
                {
                    int idx = (int)U32(s, b + 6);
                    if (idx >= 0 && idx < sst.Count) Put(sheet, U16(s, b), U16(s, b + 2), sst[idx]);
                    break;
                }
                case RecNumber when len >= 14:
                    PutNumber(sheet, U16(s, b), U16(s, b + 2), U16(s, b + 4),
                        BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReadUInt64LittleEndian(s.AsSpan(b + 6, 8))),
                        dateXf, date1904);
                    break;
                case RecRk when len >= 10:
                    PutNumber(sheet, U16(s, b), U16(s, b + 2), U16(s, b + 4), DecodeRk(U32(s, b + 6)), dateXf, date1904);
                    break;
                case RecMulRk when len >= 6:
                {
                    int row = U16(s, b);
                    int firstCol = U16(s, b + 2);
                    int count = (len - 6) / 6;
                    for (int i = 0; i < count; i++)
                    {
                        int o = b + 4 + i * 6;
                        PutNumber(sheet, row, firstCol + i, U16(s, o), DecodeRk(U32(s, o + 2)), dateXf, date1904);
                    }
                    break;
                }
                case RecLabel when len >= 9:
                {
                    int cch = U16(s, b + 6);
                    byte flags = s[b + 8];
                    int p = b + 9;
                    Put(sheet, U16(s, b), U16(s, b + 2), ReadChars(s, ref p, cch, (flags & 1) != 0, b + len));
                    break;
                }
                case RecBoolErr when len >= 8:
                {
                    byte val = s[b + 6];
                    bool isErr = s[b + 7] != 0;
                    Put(sheet, U16(s, b), U16(s, b + 2), isErr ? CellError.FromBiff(val) : val != 0);
                    break;
                }
                case RecFormula when len >= 20:
                {
                    int row = U16(s, b), col = U16(s, b + 2), xf = U16(s, b + 4);
                    pendingString = null;
                    if (s[b + 12] == 0xFF && s[b + 13] == 0xFF)
                    {
                        switch (s[b + 6])
                        {
                            case 0: pendingString = (row, col); break;
                            case 1: Put(sheet, row, col, s[b + 8] != 0); break;
                            case 2: Put(sheet, row, col, CellError.FromBiff(s[b + 8])); break;
                        }
                    }
                    else
                    {
                        PutNumber(sheet, row, col, xf,
                            BitConverter.UInt64BitsToDouble(BinaryPrimitives.ReadUInt64LittleEndian(s.AsSpan(b + 6, 8))),
                            dateXf, date1904);
                    }
                    break;
                }
                case RecString when pendingString != null && len >= 3:
                {
                    int cch = U16(s, b);
                    byte flags = s[b + 2];
                    int p = b + 3;
                    Put(sheet, pendingString.Value.Row, pendingString.Value.Col,
                        ReadChars(s, ref p, cch, (flags & 1) != 0, b + len));
                    pendingString = null;
                    break;
                }
            }

            pos = b + len;
        }
    }

    private static void Put(Worksheet sheet, int row, int col, object value)
    {
        if (row < 65536 && col < 256) sheet.Set(row, col, value);
    }

    private static void PutNumber(Worksheet sheet, int row, int col, int xf, double value, bool[] dateXf, bool date1904)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return;
        bool isDate = xf >= 0 && xf < dateXf.Length && dateXf[xf];
        Put(sheet, row, col, isDate ? ExcelDate.FromSerial(value, date1904) : value);
    }

    private static double DecodeRk(uint rk)
    {
        double value;
        if ((rk & 2) != 0)
        {
            value = (int)rk >> 2;
        }
        else
        {
            value = BitConverter.UInt64BitsToDouble((ulong)(rk & 0xFFFFFFFC) << 32);
        }
        return (rk & 1) != 0 ? value / 100.0 : value;
    }

    // ---------- strings ----------

    /// <summary>Reads <paramref name="cch"/> characters whose encoding is given by the already-read flag.</summary>
    private static string ReadChars(byte[] d, ref int p, int cch, bool utf16, int limit)
    {
        int bytes = utf16 ? cch * 2 : cch;
        bytes = Math.Max(0, Math.Min(bytes, limit - p));
        var text = utf16 ? Encoding.Unicode.GetString(d, p, bytes & ~1) : Encoding.Latin1.GetString(d, p, bytes);
        p += bytes;
        return text;
    }

    private static void ParseSst(List<byte[]> segments, List<string> output)
    {
        var r = new SegmentReader(segments);
        if (!r.HasData) return;
        r.Skip(4); // total string count
        uint unique = r.ReadU32();

        for (uint i = 0; i < unique && r.HasData; i++)
        {
            int cch = r.ReadU16();
            byte flags = r.ReadU8();
            int runs = (flags & 0x08) != 0 ? r.ReadU16() : 0;
            int ext = (flags & 0x04) != 0 ? (int)r.ReadU32() : 0;

            output.Add(r.ReadString(cch, (flags & 1) != 0));

            if (runs > 0) r.Skip(runs * 4);
            if (ext > 0) r.Skip(ext);
        }
    }

    /// <summary>Reads across SST + CONTINUE segments. A string that is split by a CONTINUE record
    /// repeats its one-byte encoding flag at the start of the continuation.</summary>
    private sealed class SegmentReader
    {
        private readonly List<byte[]> _segments;
        private int _seg;
        private int _pos;

        public SegmentReader(List<byte[]> segments) => _segments = segments;

        public bool HasData
        {
            get
            {
                Normalize();
                return _seg < _segments.Count;
            }
        }

        private void Normalize()
        {
            while (_seg < _segments.Count && _pos >= _segments[_seg].Length)
            {
                _seg++;
                _pos = 0;
            }
        }

        public byte ReadU8()
        {
            Normalize();
            if (_seg >= _segments.Count) return 0;
            return _segments[_seg][_pos++];
        }

        public int ReadU16() => ReadU8() | (ReadU8() << 8);

        public uint ReadU32() => (uint)(ReadU8() | (ReadU8() << 8) | (ReadU8() << 16) | (ReadU8() << 24));

        public void Skip(int count)
        {
            while (count > 0)
            {
                Normalize();
                if (_seg >= _segments.Count) return;
                int take = Math.Min(count, _segments[_seg].Length - _pos);
                _pos += take;
                count -= take;
            }
        }

        public string ReadString(int cch, bool utf16)
        {
            var sb = new StringBuilder(cch);
            int remaining = cch;
            bool first = true;

            while (remaining > 0)
            {
                if (_seg >= _segments.Count) break;

                if (_pos >= _segments[_seg].Length)
                {
                    _seg++;
                    _pos = 0;
                    if (_seg >= _segments.Count) break;
                    if (!first || true)
                    {
                        // continuation of a split string: first byte re-states the encoding
                        utf16 = (_segments[_seg][_pos++] & 1) != 0;
                    }
                    continue;
                }

                first = false;
                var seg = _segments[_seg];
                int charSize = utf16 ? 2 : 1;
                int available = (seg.Length - _pos) / charSize;
                int take = Math.Min(available, remaining);
                if (take == 0)
                {
                    _pos = seg.Length; // odd trailing byte
                    continue;
                }

                sb.Append(utf16
                    ? Encoding.Unicode.GetString(seg, _pos, take * 2)
                    : Encoding.Latin1.GetString(seg, _pos, take));
                _pos += take * charSize;
                remaining -= take;
            }

            return sb.ToString();
        }
    }

    // ---------- low level ----------

    private static bool TryReadHeader(byte[] d, int pos, out int id, out int len)
    {
        if (pos + 4 > d.Length)
        {
            id = len = 0;
            return false;
        }
        id = U16(d, pos);
        len = U16(d, pos + 2);
        return true;
    }

    private static byte[] Slice(byte[] d, int offset, int length) => d.AsSpan(offset, length).ToArray();
    private static int U16(byte[] d, int o) => BinaryPrimitives.ReadUInt16LittleEndian(d.AsSpan(o, 2));
    private static uint U32(byte[] d, int o) => BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(o, 4));
}
