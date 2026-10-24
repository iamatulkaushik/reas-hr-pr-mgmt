using System.Text;

namespace RevolutionAssociates.Excel;

/// <summary>Excel serial-date conversion and number-format classification.</summary>
internal static class ExcelDate
{
    private static readonly DateTime Base1900 = new(1899, 12, 30);
    private static readonly DateTime Base1900Early = new(1899, 12, 31);
    private static readonly DateTime Base1904 = new(1904, 1, 1);

    public static DateTime FromSerial(double serial, bool date1904)
    {
        long ms = (long)Math.Round(serial * 86_400_000.0, MidpointRounding.AwayFromZero);
        try
        {
            if (date1904) return Base1904.AddMilliseconds(ms);
            // Serials 1..60 sit one day later than the OADate offset because Excel
            // treats 1900 as a leap year (fictitious 1900-02-29 is serial 60).
            if (serial >= 1 && serial < 61) return Base1900Early.AddMilliseconds(ms);
            return Base1900.AddMilliseconds(ms);
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateTime.MinValue;
        }
    }

    public static double ToSerial(DateTime value, bool date1904)
    {
        if (date1904) return (value - Base1904).TotalDays;
        double d = (value - Base1900).TotalDays;
        if (d >= 2 && d < 61) return d - 1;
        return d;
    }

    // ---------- number formats ----------

    private static readonly HashSet<int> BuiltInDateIds = new()
    {
        14, 15, 16, 17, 18, 19, 20, 21, 22,
        27, 28, 29, 30, 31, 32, 33, 34, 35, 36,
        45, 46, 47,
        50, 51, 52, 53, 54, 55, 56, 57, 58
    };

    public static bool IsBuiltInDateFormat(int id) => BuiltInDateIds.Contains(id);

    /// <summary>True when a custom number-format code displays a date and/or time.</summary>
    public static bool IsDateFormat(string? code)
    {
        if (string.IsNullOrEmpty(code)) return false;

        var sb = new StringBuilder(code.Length);
        bool inQuote = false;
        for (int i = 0; i < code.Length; i++)
        {
            char c = code[i];
            if (inQuote)
            {
                if (c == '"') inQuote = false;
                continue;
            }

            if (c == '"')
            {
                inQuote = true;
            }
            else if (c == '\\' || c == '_' || c == '*')
            {
                i++; // skip the escaped / padding character
            }
            else if (c == '[')
            {
                int end = code.IndexOf(']', i + 1);
                if (end < 0) break;
                var inner = code.Substring(i + 1, end - i - 1).ToLowerInvariant();
                // elapsed-time brackets such as [h] or [mm] are time formats
                if (inner.Length > 0 && inner.All(ch => ch == 'h')) sb.Append('h');
                else if (inner.Length > 0 && inner.All(ch => ch == 'm')) sb.Append('m');
                else if (inner.Length > 0 && inner.All(ch => ch == 's')) sb.Append('s');
                i = end;
            }
            else
            {
                sb.Append(char.ToLowerInvariant(c));
            }
        }

        return sb.ToString().IndexOfAny(new[] { 'y', 'd', 'h', 's', 'm' }) >= 0;
    }
}
