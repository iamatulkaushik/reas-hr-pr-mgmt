using RevolutionAssociates.Excel;
using Xunit;

namespace RevolutionAssociates.Excel.Tests;

public class RoundTripTests
{
    private static Workbook Sample()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Payroll");
        s.AppendRow("Name", "Joined", "Salary", "Active");
        s.AppendRow("Asha", new DateTime(2026, 10, 21), 52000.5, true);
        s.AppendRow("Ravi ₹ Kumar", new DateTime(2025, 1, 2, 9, 30, 0), -12.25, false);
        s.AppendRow("Line1\nLine2", null, 0, true);
        s.BoldFirstRow = true;
        s.FreezeFirstRow = true;
        wb.AddSheet("Second").AppendRow("x", 1);
        return wb;
    }

    private static void AssertSample(Workbook wb)
    {
        var s = wb["Payroll"];
        Assert.Equal("Name", s.GetString(0, 0));
        Assert.Equal("Asha", s.GetString(1, 0));
        Assert.Equal(new DateTime(2026, 10, 21), s[1, 1]);
        Assert.Equal(52000.5, s[1, 2]);
        Assert.Equal(true, s[1, 3]);
        Assert.Equal("Ravi ₹ Kumar", s.GetString(2, 0));
        Assert.Equal(new DateTime(2025, 1, 2, 9, 30, 0), s[2, 1]);
        Assert.Equal(-12.25, s[2, 2]);
        Assert.Equal(false, s[2, 3]);
        Assert.Equal("Line1\nLine2", s.GetString(3, 0));
        Assert.Null(s[3, 1]);
        Assert.Equal(1.0, wb["Second"][0, 1]);
    }

    [Theory]
    [InlineData(ExcelFormat.Xlsx)]
    [InlineData(ExcelFormat.Xls)]
    public void BinaryFormats_RoundTrip(ExcelFormat format)
    {
        var bytes = ExcelFile.WriteToBytes(Sample(), format);
        Assert.Equal(format, ExcelFile.DetectFormat(bytes));
        AssertSample(ExcelFile.Read(bytes, format));
    }

    [Fact]
    public void Csv_RoundTrip_FirstSheet()
    {
        var bytes = ExcelFile.WriteToBytes(Sample(), ExcelFormat.Csv);
        var wb = ExcelFile.Read(bytes, ExcelFormat.Csv, "Payroll");
        Assert.Equal("Asha", wb[0].GetString(1, 0));
        Assert.Equal("Line1\nLine2", wb[0].GetString(3, 0));
    }

    [Fact]
    public void Xls_ManyStrings_SpansContinueRecords()
    {
        var wb = new Workbook();
        var s = wb.AddSheet("Big");
        for (int i = 0; i < 3000; i++) s.AppendRow("string number " + i + new string('x', i % 40), i);
        s.AppendRow(new string('Ж', 20000)); // wide string longer than one record

        var back = ExcelFile.Read(ExcelFile.WriteToBytes(wb, ExcelFormat.Xls), ExcelFormat.Xls)["Big"];
        for (int i = 0; i < 3000; i++)
        {
            Assert.Equal("string number " + i + new string('x', i % 40), back.GetString(i, 0));
            Assert.Equal((double)i, back[i, 1]);
        }
        Assert.Equal(new string('Ж', 20000), back.GetString(3000, 0));
    }

    [Fact]
    public void Xls_TooManyColumns_Throws()
    {
        var wb = new Workbook();
        wb.AddSheet("Wide").Set(0, 256, "x");
        Assert.Throws<InvalidOperationException>(() => ExcelFile.WriteToBytes(wb, ExcelFormat.Xls));
    }

    public class Employee
    {
        [ExcelColumn("Employee Name", Order = 1)] public string Name { get; set; } = "";
        public DateTime Joined { get; set; }
        public decimal Salary { get; set; }
        public int? Grade { get; set; }
        [ExcelIgnore] public string? Notes { get; set; }
    }

    [Theory]
    [InlineData(".xlsx")]
    [InlineData(".xls")]
    [InlineData(".csv")]
    public void Objects_RoundTrip(string ext)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ext);
        try
        {
            var staff = new[]
            {
                new Employee { Name = "Asha", Joined = new DateTime(2026, 10, 21), Salary = 52000.5m, Grade = 3, Notes = "x" },
                new Employee { Name = "Ravi", Joined = new DateTime(2024, 4, 1), Salary = 40000m, Grade = null }
            };
            ExcelFile.WriteObjects(path, staff);
            var back = ExcelFile.ReadObjects<Employee>(path);

            Assert.Equal(2, back.Count);
            Assert.Equal("Asha", back[0].Name);
            Assert.Equal(52000.5m, back[0].Salary);
            Assert.Equal(3, back[0].Grade);
            Assert.Null(back[0].Notes);
            Assert.Equal(new DateTime(2024, 4, 1), back[1].Joined);
            Assert.Null(back[1].Grade);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void DataTable_RoundTrip()
    {
        var wb = Sample();
        var table = wb["Payroll"].ToDataTable();
        Assert.Equal(4, table.Columns.Count);
        Assert.Equal(3, table.Rows.Count);
        Assert.Equal("Asha", table.Rows[0]["Name"]);

        var again = ExcelFile.FromDataTable(table);
        Assert.Equal("Asha", again[0].GetString(1, 0));
    }

    [Fact]
    public void Dates_1900LeapYearQuirk()
    {
        var wb = new Workbook();
        wb.AddSheet("D").AppendRow(new DateTime(1900, 3, 1), new DateTime(1900, 1, 1));
        var back = ExcelFile.Read(ExcelFile.WriteToBytes(wb, ExcelFormat.Xlsx), ExcelFormat.Xlsx)[0];
        Assert.Equal(new DateTime(1900, 3, 1), back[0, 0]);
        Assert.Equal(new DateTime(1900, 1, 1), back[0, 1]);
    }
}
