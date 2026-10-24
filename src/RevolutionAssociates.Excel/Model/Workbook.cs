namespace RevolutionAssociates.Excel;

/// <summary>An ordered collection of worksheets.</summary>
public sealed class Workbook
{
    private static readonly char[] InvalidSheetChars = { '\\', '/', '?', '*', '[', ']', ':' };

    public List<Worksheet> Sheets { get; } = new();

    /// <summary>True when the source workbook used the 1904 date system.</summary>
    public bool Date1904 { get; set; }

    public Worksheet this[int index] => Sheets[index];

    public Worksheet this[string name] =>
        FindSheet(name) ?? throw new KeyNotFoundException($"Worksheet '{name}' was not found.");

    public Worksheet? FindSheet(string name) =>
        Sheets.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Adds a worksheet. Invalid characters are replaced and the name is made unique.</summary>
    public Worksheet AddSheet(string? name = null)
    {
        var sheet = new Worksheet(UniqueName(name));
        Sheets.Add(sheet);
        return sheet;
    }

    private string UniqueName(string? requested)
    {
        var baseName = string.IsNullOrWhiteSpace(requested) ? "Sheet" + (Sheets.Count + 1) : requested.Trim();
        baseName = CleanSheetName(baseName);

        var name = baseName;
        int n = 2;
        while (Sheets.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            var suffix = "_" + n++;
            name = (baseName.Length + suffix.Length > 31 ? baseName[..(31 - suffix.Length)] : baseName) + suffix;
        }
        return name;
    }

    /// <summary>Replaces characters Excel forbids in sheet names and trims to 31 characters.</summary>
    internal static string CleanSheetName(string name)
    {
        foreach (var c in InvalidSheetChars) name = name.Replace(c, '_');
        name = name.Trim('\'');
        if (name.Length == 0) name = "Sheet";
        return name.Length > 31 ? name[..31] : name;
    }
}
