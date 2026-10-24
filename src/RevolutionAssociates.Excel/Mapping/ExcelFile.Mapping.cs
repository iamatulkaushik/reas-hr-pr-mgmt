using System.Data;

namespace RevolutionAssociates.Excel;

public static partial class ExcelFile
{
    /// <summary>Builds a one-sheet workbook from a sequence of objects.</summary>
    public static Workbook FromObjects<T>(IEnumerable<T> items, string? sheetName = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        var wb = new Workbook();
        wb.AddSheet(sheetName ?? typeof(T).Name).LoadObjects(items);
        return wb;
    }

    /// <summary>Builds a one-sheet workbook from a DataTable (sheet named after the table).</summary>
    public static Workbook FromDataTable(DataTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        var wb = new Workbook();
        wb.AddSheet(string.IsNullOrWhiteSpace(table.TableName) ? null : table.TableName).LoadDataTable(table);
        return wb;
    }

    /// <summary>Builds a workbook with one sheet per table in the DataSet.</summary>
    public static Workbook FromDataSet(DataSet dataSet)
    {
        ArgumentNullException.ThrowIfNull(dataSet);
        var wb = new Workbook();
        foreach (DataTable t in dataSet.Tables)
            wb.AddSheet(string.IsNullOrWhiteSpace(t.TableName) ? null : t.TableName).LoadDataTable(t);
        return wb;
    }

    /// <summary>Writes objects to a file. The format comes from the extension (.xlsx, .xls, .csv, .tsv).</summary>
    public static void WriteObjects<T>(string path, IEnumerable<T> items, string? sheetName = null) =>
        Write(FromObjects(items, sheetName), path);

    /// <summary>Writes a DataTable to a file. The format comes from the extension.</summary>
    public static void WriteDataTable(string path, DataTable table) =>
        Write(FromDataTable(table), path);

    /// <summary>Reads the first (or named) sheet of a file into objects.</summary>
    public static List<T> ReadObjects<T>(string path, string? sheetName = null) where T : new() =>
        PickSheet(Read(path), sheetName).ToObjects<T>();

    /// <summary>Reads the first (or named) sheet of a file into a DataTable.</summary>
    public static DataTable ReadDataTable(string path, string? sheetName = null, bool firstRowIsHeader = true) =>
        PickSheet(Read(path), sheetName).ToDataTable(firstRowIsHeader);

    private static Worksheet PickSheet(Workbook wb, string? sheetName)
    {
        if (sheetName != null) return wb[sheetName];
        if (wb.Sheets.Count == 0) throw new InvalidOperationException("The workbook has no worksheets.");
        return wb.Sheets[0];
    }
}
