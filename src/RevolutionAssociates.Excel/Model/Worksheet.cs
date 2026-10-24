namespace RevolutionAssociates.Excel;

/// <summary>
/// A sparse grid of cell values. Rows and columns are zero-based.
/// Values are: null, string, double, bool, DateTime, or <see cref="CellError"/>.
/// </summary>
public sealed class Worksheet
{
    private readonly Dictionary<(int Row, int Col), object?> _cells = new();
    private int _maxRow = -1;
    private int _maxCol = -1;

    public Worksheet(string name)
    {
        Name = name;
    }

    public string Name { get; set; }

    /// <summary>Number of rows up to and including the last used row.</summary>
    public int RowCount => _maxRow + 1;

    /// <summary>Number of columns up to and including the last used column.</summary>
    public int ColumnCount => _maxCol + 1;

    /// <summary>Column widths in characters, keyed by zero-based column index (used when writing).</summary>
    public Dictionary<int, double> ColumnWidths { get; } = new();

    /// <summary>When true, the first row is written in bold (xlsx only).</summary>
    public bool BoldFirstRow { get; set; }

    /// <summary>When true, the first row is frozen (xlsx only).</summary>
    public bool FreezeFirstRow { get; set; }

    public object? this[int row, int col]
    {
        get => Get(row, col);
        set => Set(row, col, value);
    }

    public object? Get(int row, int col)
    {
        return _cells.TryGetValue((row, col), out var v) ? v : null;
    }

    public void Set(int row, int col, object? value)
    {
        if (row < 0) throw new ArgumentOutOfRangeException(nameof(row));
        if (col < 0) throw new ArgumentOutOfRangeException(nameof(col));
        value = Normalize(value);
        if (value is null)
        {
            _cells.Remove((row, col));
            return;
        }
        _cells[(row, col)] = value;
        if (row > _maxRow) _maxRow = row;
        if (col > _maxCol) _maxCol = col;
    }

    /// <summary>Gets a value formatted as text, or an empty string for blank cells.</summary>
    public string GetString(int row, int col)
    {
        var v = Get(row, col);
        return v switch
        {
            null => string.Empty,
            string s => s,
            bool b => b ? "TRUE" : "FALSE",
            double d => d.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
            DateTime dt => dt.TimeOfDay == TimeSpan.Zero
                ? dt.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
                : dt.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            CellError e => e.ToString(),
            _ => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    /// <summary>Appends a row after the last used row and returns its index.</summary>
    public int AppendRow(params object?[] values)
    {
        int row = _maxRow + 1;
        for (int c = 0; c < values.Length; c++)
            Set(row, c, values[c]);
        // keep an all-blank row counted so row indexes stay predictable
        if (row > _maxRow) _maxRow = row;
        return row;
    }

    /// <summary>Returns the values of one row as an array of length <see cref="ColumnCount"/>.</summary>
    public object?[] GetRow(int row)
    {
        var arr = new object?[ColumnCount];
        for (int c = 0; c < arr.Length; c++) arr[c] = Get(row, c);
        return arr;
    }

    /// <summary>Enumerates every row (including blank ones) as arrays.</summary>
    public IEnumerable<object?[]> Rows()
    {
        for (int r = 0; r < RowCount; r++) yield return GetRow(r);
    }

    /// <summary>Enumerates only the populated cells, in row-major order.</summary>
    public IEnumerable<(int Row, int Col, object Value)> Cells()
    {
        foreach (var kv in _cells.OrderBy(k => k.Key.Row).ThenBy(k => k.Key.Col))
            yield return (kv.Key.Row, kv.Key.Col, kv.Value!);
    }

    /// <summary>Converts an A1-style reference (e.g. "C5") to zero-based row/column.</summary>
    public static (int Row, int Col) ParseReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("Empty reference.", nameof(reference));
        int i = 0;
        int col = 0;
        while (i < reference.Length && char.IsLetter(reference[i]))
        {
            col = col * 26 + (char.ToUpperInvariant(reference[i]) - 'A' + 1);
            i++;
        }
        if (i == 0 || i == reference.Length || !int.TryParse(reference.AsSpan(i), out int row1) || row1 < 1)
            throw new FormatException($"Invalid cell reference '{reference}'.");
        return (row1 - 1, col - 1);
    }

    /// <summary>Converts a zero-based column index to letters (0 → "A", 26 → "AA").</summary>
    public static string ColumnName(int col)
    {
        if (col < 0) throw new ArgumentOutOfRangeException(nameof(col));
        var s = string.Empty;
        col++;
        while (col > 0)
        {
            int rem = (col - 1) % 26;
            s = (char)('A' + rem) + s;
            col = (col - 1) / 26;
        }
        return s;
    }

    public static string ToReference(int row, int col) =>
        ColumnName(col) + (row + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static object? Normalize(object? v)
    {
        switch (v)
        {
            case null:
            case DBNull:
                return null;
            case string s:
                return s.Length == 0 ? null : s;
            case double or bool or DateTime or CellError:
                return v;
            case DateTimeOffset dto:
                return dto.DateTime;
            case DateOnly d:
                return d.ToDateTime(TimeOnly.MinValue);
            case TimeOnly t:
                return DateTime.MinValue.Add(t.ToTimeSpan());
            case TimeSpan ts:
                return ts.TotalDays;
            case decimal m:
                return (double)m;
            case float f:
                return (double)f;
            case byte or sbyte or short or ushort or int or uint or long or ulong:
                return Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture);
            case Enum e:
                return e.ToString();
            case char c:
                return c.ToString();
            case Guid g:
                return g.ToString();
            default:
                return Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

/// <summary>An Excel error value such as #DIV/0!.</summary>
public readonly record struct CellError(string Text)
{
    public override string ToString() => Text;

    public static readonly CellError Null = new("#NULL!");
    public static readonly CellError DivZero = new("#DIV/0!");
    public static readonly CellError Value = new("#VALUE!");
    public static readonly CellError Ref = new("#REF!");
    public static readonly CellError Name = new("#NAME?");
    public static readonly CellError Num = new("#NUM!");
    public static readonly CellError NA = new("#N/A");

    internal static CellError FromBiff(byte code) => code switch
    {
        0x00 => Null,
        0x07 => DivZero,
        0x0F => Value,
        0x17 => Ref,
        0x1D => Name,
        0x24 => Num,
        0x2A => NA,
        _ => new CellError("#ERR" + code)
    };

    internal static CellError FromText(string text) => text switch
    {
        "#NULL!" => Null,
        "#DIV/0!" => DivZero,
        "#VALUE!" => Value,
        "#REF!" => Ref,
        "#NAME?" => Name,
        "#NUM!" => Num,
        "#N/A" => NA,
        _ => new CellError(text)
    };

    internal byte ToBiff() => Text switch
    {
        "#NULL!" => 0x00,
        "#DIV/0!" => 0x07,
        "#VALUE!" => 0x0F,
        "#REF!" => 0x17,
        "#NAME?" => 0x1D,
        "#NUM!" => 0x24,
        _ => 0x2A
    };
}
