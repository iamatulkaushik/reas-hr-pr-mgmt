namespace RevolutionAssociates.Excel;

/// <summary>Spreadsheet formats supported by <see cref="ExcelFile"/>.</summary>
public enum ExcelFormat
{
    Unknown = 0,
    /// <summary>OpenXML workbook (.xlsx, .xlsm, .xltx, .xltm).</summary>
    Xlsx,
    /// <summary>Legacy Excel 97-2003 BIFF8 workbook (.xls).</summary>
    Xls,
    /// <summary>Comma-separated values (.csv).</summary>
    Csv,
    /// <summary>Tab-separated values (.tsv, .tab).</summary>
    Tsv
}

/// <summary>Entry point: read and write workbooks in any supported format.</summary>
public static partial class ExcelFile
{
    // ---------- format detection ----------

    /// <summary>Detects a format from the file extension only.</summary>
    public static ExcelFormat FormatFromExtension(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".xlsx" or ".xlsm" or ".xltx" or ".xltm" => ExcelFormat.Xlsx,
            ".xls" or ".xlt" => ExcelFormat.Xls,
            ".csv" or ".txt" => ExcelFormat.Csv,
            ".tsv" or ".tab" => ExcelFormat.Tsv,
            _ => ExcelFormat.Unknown
        };
    }

    /// <summary>Detects a format from the leading bytes of the content.</summary>
    public static ExcelFormat DetectFormat(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 4 && data[0] == 0x50 && data[1] == 0x4B && (data[2] == 0x03 || data[2] == 0x05))
            return ExcelFormat.Xlsx;
        if (data.Length >= 8 &&
            data[0] == 0xD0 && data[1] == 0xCF && data[2] == 0x11 && data[3] == 0xE0 &&
            data[4] == 0xA1 && data[5] == 0xB1 && data[6] == 0x1A && data[7] == 0xE1)
            return ExcelFormat.Xls;
        return ExcelFormat.Unknown;
    }

    // ---------- reading ----------

    /// <summary>Reads a workbook from disk. The format is sniffed from content, then the extension.</summary>
    public static Workbook Read(string path)
    {
        var data = File.ReadAllBytes(path);
        return ReadSniffed(data, path);
    }

    public static async Task<Workbook> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var data = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return ReadSniffed(data, path);
    }

    private static Workbook ReadSniffed(byte[] data, string path)
    {
        var format = DetectFormat(data);
        if (format == ExcelFormat.Unknown) format = FormatFromExtension(path);
        if (format == ExcelFormat.Unknown) format = ExcelFormat.Csv;
        return Read(data, format, Path.GetFileNameWithoutExtension(path));
    }

    /// <summary>Reads a workbook from a stream. If <paramref name="format"/> is Unknown it is sniffed.</summary>
    public static Workbook Read(Stream stream, ExcelFormat format = ExcelFormat.Unknown)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return Read(ms.ToArray(), format, "Sheet1");
    }

    public static async Task<Workbook> ReadAsync(Stream stream, ExcelFormat format = ExcelFormat.Unknown,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        return Read(ms.ToArray(), format, "Sheet1");
    }

    /// <summary>Reads a workbook from an in-memory buffer.</summary>
    public static Workbook Read(byte[] data, ExcelFormat format = ExcelFormat.Unknown, string defaultSheetName = "Sheet1")
    {
        ArgumentNullException.ThrowIfNull(data);
        if (format == ExcelFormat.Unknown)
        {
            format = DetectFormat(data);
            if (format == ExcelFormat.Unknown) format = ExcelFormat.Csv;
        }

        switch (format)
        {
            case ExcelFormat.Xlsx:
                return XlsxReader.Read(new MemoryStream(data, writable: false));
            case ExcelFormat.Xls:
                return XlsReader.Read(data);
            case ExcelFormat.Csv:
                return CsvFormat.ReadWorkbook(data, defaultSheetName, delimiter: null);
            case ExcelFormat.Tsv:
                return CsvFormat.ReadWorkbook(data, defaultSheetName, delimiter: '\t');
            default:
                throw new NotSupportedException("Unsupported spreadsheet format.");
        }
    }

    // ---------- writing ----------

    /// <summary>Writes a workbook to disk. The format comes from the extension unless given.</summary>
    public static void Write(Workbook workbook, string path, ExcelFormat format = ExcelFormat.Unknown)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        if (format == ExcelFormat.Unknown) format = FormatFromExtension(path);
        if (format == ExcelFormat.Unknown) format = ExcelFormat.Xlsx;

        // Build in memory first, so a workbook that cannot be written never leaves an empty or half-written file behind.
        var bytes = WriteToBytes(workbook, format);

        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        File.WriteAllBytes(path, bytes);
    }

    /// <summary>Writes a workbook to a stream.</summary>
    public static void Write(Workbook workbook, Stream stream, ExcelFormat format)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        ArgumentNullException.ThrowIfNull(stream);

        switch (format)
        {
            case ExcelFormat.Xlsx:
                XlsxWriter.Write(workbook, stream);
                break;
            case ExcelFormat.Xls:
                XlsWriter.Write(workbook, stream);
                break;
            case ExcelFormat.Csv:
                CsvFormat.Write(RequireFirstSheet(workbook), stream, ',');
                break;
            case ExcelFormat.Tsv:
                CsvFormat.Write(RequireFirstSheet(workbook), stream, '\t');
                break;
            default:
                throw new NotSupportedException("Unsupported spreadsheet format.");
        }
    }

    public static byte[] WriteToBytes(Workbook workbook, ExcelFormat format)
    {
        using var ms = new MemoryStream();
        Write(workbook, ms, format);
        return ms.ToArray();
    }

    private static Worksheet RequireFirstSheet(Workbook workbook)
    {
        if (workbook.Sheets.Count == 0) throw new InvalidOperationException("The workbook has no worksheets.");
        return workbook.Sheets[0];
    }

    // ---------- conversion ----------

    /// <summary>Converts a file from one format to another (e.g. .xls to .xlsx).</summary>
    public static void Convert(string sourcePath, string destinationPath)
    {
        var wb = Read(sourcePath);
        Write(wb, destinationPath);
    }
}
