using System.Data;
using System.Globalization;
using System.Reflection;

namespace RevolutionAssociates.Excel;

/// <summary>Customises how a property is mapped to a worksheet column.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ExcelColumnAttribute : Attribute
{
    public ExcelColumnAttribute() { }
    public ExcelColumnAttribute(string name) => Name = name;

    /// <summary>Header text. Defaults to the property name.</summary>
    public string? Name { get; set; }

    /// <summary>Column order when writing (lower first). Unordered properties follow in declaration order.</summary>
    public int Order { get; set; } = int.MaxValue;
}

/// <summary>Excludes a property from import and export.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class ExcelIgnoreAttribute : Attribute { }

/// <summary>DataTable / object import and export helpers for <see cref="Worksheet"/>.</summary>
public static class WorksheetExtensions
{
    // ---------- layout ----------

    /// <summary>Sets column widths from the longest value in each column.</summary>
    public static Worksheet AutoFitColumns(this Worksheet sheet, double minWidth = 8, double maxWidth = 60)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var longest = new Dictionary<int, int>();
        foreach (var (row, col, _) in sheet.Cells())
        {
            int len = LongestLine(sheet.GetString(row, col));
            if (!longest.TryGetValue(col, out var cur) || len > cur) longest[col] = len;
        }
        foreach (var kv in longest)
            sheet.ColumnWidths[kv.Key] = Math.Clamp(kv.Value + 2, minWidth, maxWidth);
        return sheet;
    }

    private static int LongestLine(string s)
    {
        int best = 0;
        foreach (var line in s.Split('\n')) best = Math.Max(best, line.TrimEnd('\r').Length);
        return best;
    }

    // ---------- DataTable ----------

    /// <summary>Appends a DataTable (header row from column names, then its rows).</summary>
    public static Worksheet LoadDataTable(this Worksheet sheet, DataTable table, bool writeHeader = true)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(table);

        if (writeHeader)
        {
            bool atTop = sheet.RowCount == 0;
            sheet.AppendRow(table.Columns.Cast<DataColumn>().Select(c => (object?)c.ColumnName).ToArray());
            if (atTop)
            {
                sheet.BoldFirstRow = true;
                sheet.FreezeFirstRow = true;
            }
        }

        foreach (DataRow dr in table.Rows)
        {
            var values = new object?[table.Columns.Count];
            for (int c = 0; c < values.Length; c++)
                values[c] = dr[c] is DBNull ? null : dr[c];
            sheet.AppendRow(values);
        }

        return sheet.AutoFitColumns();
    }

    /// <summary>Converts a worksheet into a DataTable. Column type is <see cref="object"/> so cell types are preserved.
    /// Fully blank rows are skipped.</summary>
    public static DataTable ToDataTable(this Worksheet sheet, bool firstRowIsHeader = true)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var table = new DataTable(sheet.Name);
        int cols = sheet.ColumnCount;
        int firstData = firstRowIsHeader ? 1 : 0;

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int c = 0; c < cols; c++)
        {
            var name = firstRowIsHeader && sheet.RowCount > 0 ? sheet.GetString(0, c).Trim() : string.Empty;
            if (name.Length == 0) name = "Column" + (c + 1);
            var unique = name;
            int n = 2;
            while (!used.Add(unique)) unique = name + "_" + n++;
            table.Columns.Add(new DataColumn(unique, typeof(object)));
        }

        for (int r = firstData; r < sheet.RowCount; r++)
        {
            if (IsBlankRow(sheet, r, cols)) continue;
            var values = new object[cols];
            for (int c = 0; c < cols; c++) values[c] = sheet.Get(r, c) ?? DBNull.Value;
            table.Rows.Add(values);
        }

        return table;
    }

    // ---------- objects ----------

    /// <summary>Appends objects as rows. Public readable properties become columns.</summary>
    public static Worksheet LoadObjects<T>(this Worksheet sheet, IEnumerable<T> items, bool writeHeader = true)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(items);

        var props = MappedProperties(typeof(T), forWrite: true);

        if (writeHeader)
        {
            bool atTop = sheet.RowCount == 0;
            sheet.AppendRow(props.Select(p => (object?)p.Header).ToArray());
            if (atTop)
            {
                sheet.BoldFirstRow = true;
                sheet.FreezeFirstRow = true;
            }
        }

        foreach (var item in items)
        {
            var values = new object?[props.Count];
            for (int i = 0; i < props.Count; i++)
                values[i] = item is null ? null : props[i].Property.GetValue(item);
            sheet.AppendRow(values);
        }

        return sheet.AutoFitColumns();
    }

    /// <summary>Reads rows into objects. The first row must hold headers that match property names
    /// (or <see cref="ExcelColumnAttribute"/> names), ignoring case, spaces and underscores.
    /// Unmatched columns are ignored; fully blank rows are skipped.</summary>
    public static List<T> ToObjects<T>(this Worksheet sheet) where T : new()
    {
        ArgumentNullException.ThrowIfNull(sheet);
        var result = new List<T>();
        if (sheet.RowCount == 0) return result;

        var props = MappedProperties(typeof(T), forWrite: false);
        var byKey = new Dictionary<string, MappedProperty>(StringComparer.Ordinal);
        foreach (var p in props)
        {
            byKey.TryAdd(Key(p.Header), p);
            byKey.TryAdd(Key(p.Property.Name), p);
        }

        var map = new List<(int Col, MappedProperty Prop)>();
        var seen = new HashSet<PropertyInfo>();
        for (int c = 0; c < sheet.ColumnCount; c++)
        {
            var header = sheet.GetString(0, c);
            if (header.Length > 0 && byKey.TryGetValue(Key(header), out var prop) && seen.Add(prop.Property))
                map.Add((c, prop));
        }

        for (int r = 1; r < sheet.RowCount; r++)
        {
            if (IsBlankRow(sheet, r, sheet.ColumnCount)) continue;

            var obj = new T();
            foreach (var (col, prop) in map)
            {
                var raw = sheet.Get(r, col);
                if (raw is null) continue;
                try
                {
                    var converted = prop.Property.PropertyType == typeof(string)
                        ? sheet.GetString(r, col)
                        : ConvertCell(raw, prop.Property.PropertyType);
                    if (converted is not null) prop.Property.SetValue(obj, converted);
                }
                catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
                {
                    throw new FormatException(
                        $"Cannot convert cell {Worksheet.ToReference(r, col)} ('{sheet.GetString(r, col)}') " +
                        $"to {prop.Property.PropertyType.Name} for property '{prop.Property.Name}' on sheet '{sheet.Name}'.", ex);
                }
            }
            result.Add(obj);
        }

        return result;
    }

    // ---------- internals ----------

    private sealed record MappedProperty(PropertyInfo Property, string Header);

    private static List<MappedProperty> MappedProperties(Type type, bool forWrite)
    {
        var list = new List<(MappedProperty Prop, int Order, int Index)>();
        int index = 0;
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length > 0) continue;
            if (p.GetCustomAttribute<ExcelIgnoreAttribute>() != null) continue;
            if (forWrite ? !p.CanRead : !(p.CanWrite && p.SetMethod!.IsPublic)) continue;

            var attr = p.GetCustomAttribute<ExcelColumnAttribute>();
            var header = string.IsNullOrWhiteSpace(attr?.Name) ? p.Name : attr!.Name!;
            list.Add((new MappedProperty(p, header), attr?.Order ?? int.MaxValue, index++));
        }

        return list.OrderBy(x => x.Order).ThenBy(x => x.Index).Select(x => x.Prop).ToList();
    }

    private static string Key(string s)
    {
        var chars = s.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray();
        return new string(chars);
    }

    private static bool IsBlankRow(Worksheet sheet, int row, int cols)
    {
        for (int c = 0; c < cols; c++)
            if (sheet.Get(row, c) is not null) return false;
        return true;
    }

    /// <summary>Converts a raw cell value to <paramref name="target"/>. Returns null for blanks.</summary>
    internal static object? ConvertCell(object? value, Type target)
    {
        if (value is null) return null;
        if (value is string blank && string.IsNullOrWhiteSpace(blank)) return null;

        var type = Nullable.GetUnderlyingType(target) ?? target;
        if (type == typeof(object) || type.IsInstanceOfType(value)) return value;
        if (value is CellError err) throw new FormatException($"The cell contains the error value {err}.");

        var inv = CultureInfo.InvariantCulture;

        if (type.IsEnum)
        {
            return value switch
            {
                string s => Enum.Parse(type, s.Trim(), ignoreCase: true),
                double d => Enum.ToObject(type, Convert.ToInt64(d, inv)),
                _ => throw new InvalidCastException()
            };
        }

        if (type == typeof(DateTime))
        {
            return value switch
            {
                double d => ExcelDate.FromSerial(d, date1904: false),
                string s => DateTime.Parse(s.Trim(), inv),
                _ => throw new InvalidCastException()
            };
        }

        if (type == typeof(DateTimeOffset))
        {
            var dt = (DateTime)ConvertCell(value, typeof(DateTime))!;
            return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), TimeSpan.Zero);
        }

        if (type == typeof(DateOnly))
        {
            return value switch
            {
                DateTime dt => DateOnly.FromDateTime(dt),
                double d => DateOnly.FromDateTime(ExcelDate.FromSerial(d, date1904: false)),
                string s => DateOnly.Parse(s.Trim(), inv),
                _ => throw new InvalidCastException()
            };
        }

        if (type == typeof(TimeOnly))
        {
            return value switch
            {
                DateTime dt => TimeOnly.FromDateTime(dt),
                double d => TimeOnly.FromTimeSpan(TimeSpan.FromDays(d - Math.Floor(d))),
                string s => TimeOnly.Parse(s.Trim(), inv),
                _ => throw new InvalidCastException()
            };
        }

        if (type == typeof(TimeSpan))
        {
            return value switch
            {
                double d => TimeSpan.FromDays(d),
                DateTime dt => dt.TimeOfDay,
                string s => TimeSpan.Parse(s.Trim(), inv),
                _ => throw new InvalidCastException()
            };
        }

        if (type == typeof(Guid)) return Guid.Parse(Convert.ToString(value, inv)!.Trim());

        if (type == typeof(char))
        {
            var s = Convert.ToString(value, inv) ?? string.Empty;
            return s.Length > 0 ? s[0] : null;
        }

        if (type == typeof(bool))
        {
            switch (value)
            {
                case double d: return d != 0;
                case string s:
                    var t = s.Trim().ToLowerInvariant();
                    if (t is "true" or "yes" or "y" or "1" or "t") return true;
                    if (t is "false" or "no" or "n" or "0" or "f") return false;
                    throw new FormatException($"'{s}' is not a boolean value.");
                default: throw new InvalidCastException();
            }
        }

        // numeric targets
        if (value is bool b) value = b ? 1.0 : 0.0;
        if (value is DateTime when2) value = ExcelDate.ToSerial(when2, date1904: false);
        if (value is string text) value = text.Trim();

        return Convert.ChangeType(value, type, inv);
    }
}
