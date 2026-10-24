# RevolutionAssociates.Excel

Dependency-free .NET 10 library for reading and writing spreadsheets.

| Format | Read | Write |
|---|---|---|
| `.xlsx` `.xlsm` `.xltx` `.xltm` (OpenXML) | yes | yes |
| `.xls` (Excel 97-2003, BIFF8) | yes | yes |
| `.csv` `.tsv` | yes | yes |

No third-party packages are needed. The library uses only the .NET base class library.

## Install

```
dotnet add package RevolutionAssociates.Excel
```

## Quick start

```csharp
using RevolutionAssociates.Excel;

// Read any supported file (format is detected from content, then extension)
Workbook wb = ExcelFile.Read("employees.xls");
Worksheet sheet = wb[0];
string name = sheet.GetString(1, 0);   // row 2, column A  (rows/columns are zero-based)
object? raw = sheet[1, 2];             // string, double, bool, DateTime, CellError or null

// Write
var book = new Workbook();
var s = book.AddSheet("Payroll");
s.AppendRow("Name", "Joined", "Salary");
s.AppendRow("Asha", new DateTime(2026, 10, 21), 52000);
s.BoldFirstRow = true;
s.FreezeFirstRow = true;
ExcelFile.Write(book, "payroll.xlsx");   // or .xls / .csv

// Convert between formats
ExcelFile.Convert("old.xls", "new.xlsx");
```

## Objects and DataTables

```csharp
public class Employee
{
    [ExcelColumn("Employee Name", Order = 1)] public string Name { get; set; } = "";
    public DateTime Joined { get; set; }
    public decimal Salary { get; set; }
    [ExcelIgnore] public string? Notes { get; set; }
}

ExcelFile.WriteObjects("staff.xlsx", employees);
List<Employee> back = ExcelFile.ReadObjects<Employee>("staff.xlsx");

DataTable table = ExcelFile.ReadDataTable("staff.xlsx");
ExcelFile.WriteDataTable("copy.xls", table);
```

Headers match property names or `[ExcelColumn]` names, ignoring case, spaces and underscores.

## Notes and limits

- Values are returned as `string`, `double`, `bool`, `DateTime` or `CellError`. Formula cells return their cached result.
- `.xls` files are limited to 65,536 rows and 256 columns. Writing a larger sheet throws; use `.xlsx`.
- Password-protected workbooks and BIFF5 (Excel 5/95) files are not supported.
- Styles beyond bold header, frozen first row, date formats and column widths are not written.

## License

MIT. Copyright (c) Revolution Associates. https://reas.host/
