Imports Revolution.Core
Imports Xunit

Public Class ImportTemplateTests
    Private Shared Function EmployeeTemplate() As ImportTemplate
        Return New ImportTemplate("Employees", New List(Of ImportColumn) From {
            New ImportColumn("Full Name", True, "The name as on the Aadhaar card.", "Asha Rao", New String() {"Name", "Employee Name"}),
            New ImportColumn("Date of Birth", True, "dd/mm/yyyy", "09/11/1990", New String() {"DOB"}),
            New ImportColumn("Mobile", False, "10 digits")})
    End Function

    Private Shared Function Table(headers As String(), ParamArray rows As String()()) As ImportTable
        Dim raw As New List(Of RawImportRow)()
        For i As Integer = 0 To rows.Length - 1
            raw.Add(New RawImportRow(i + 2, rows(i)))
        Next
        Return New ImportTable(headers, raw)
    End Function

    <Fact>
    Public Sub Map_MatchesHeadersInAnyOrderCaseAndAlias_AndIgnoresOtherColumns()
        Dim mapped = EmployeeTemplate().Map(Table(New String() {"dob", "EMPLOYEE NAME", "Remarks", "Mobile"},
                                                  New String() {"09/11/1990", " Asha Rao ", "x", "9876543210"}))
        Assert.True(mapped.Succeeded)
        Dim row = mapped.Value.Rows.Single()
        Assert.Equal(2, row.RowNumber)
        Assert.Equal("Asha Rao", row.Get("Full Name"))
        Assert.Equal("09/11/1990", row.Get("date of birth"))
        Assert.Equal("9876543210", row.Get("Mobile"))
        Assert.Equal("Remarks", mapped.Value.IgnoredColumns.Single())
    End Sub

    <Fact>
    Public Sub Map_WhenARequiredColumnIsMissing_NamesTheColumns_AndQuotesNoCell()
        Dim mapped = EmployeeTemplate().Map(Table(New String() {"Mobile"}, New String() {"9876543210"}))
        Assert.False(mapped.Succeeded)
        Assert.Contains("Full Name", mapped.Message)
        Assert.Contains("Date of Birth", mapped.Message)
        Assert.DoesNotContain("9876543210", mapped.Message)
    End Sub

    <Fact>
    Public Sub Map_WhenTwoColumnsMatchOneTemplateColumn_Fails()
        Dim mapped = EmployeeTemplate().Map(Table(New String() {"Name", "Employee Name", "DOB"}, New String() {"a", "b", "c"}))
        Assert.False(mapped.Succeeded)
        Assert.Contains("Full Name", mapped.Message)
    End Sub

    <Fact>
    Public Sub Map_PadsRowsShorterThanTheHeader_AndReadsAMissingOptionalColumnAsEmpty()
        Dim mapped = EmployeeTemplate().Map(Table(New String() {"Full Name", "Date of Birth", "Mobile"}, New String() {"Asha Rao"}))
        Assert.True(mapped.Succeeded)
        Dim row = mapped.Value.Rows.Single()
        Assert.Equal("Asha Rao", row.Get("Full Name"))
        Assert.Equal(String.Empty, row.Get("Date of Birth"))
        Assert.Equal(String.Empty, row.Get("Mobile"))
        Dim noMobile = EmployeeTemplate().Map(Table(New String() {"Full Name", "Date of Birth"}, New String() {"Asha Rao", "09/11/1990"}))
        Assert.True(noMobile.Succeeded)
        Assert.Equal(String.Empty, noMobile.Value.Rows.Single().Get("Mobile"))
    End Sub

    <Fact>
    Public Sub ToExportSheets_HasTheHeaderRow_AndAnInstructionsSheet()
        Dim sheets = EmployeeTemplate().ToExportSheets()
        Assert.Equal(2, sheets.Count)
        Assert.Equal("Employees", sheets(0).Name)
        Assert.Equal(New String() {"Full Name", "Date of Birth", "Mobile"}, sheets(0).Table.Single())
        Assert.Equal(ImportTemplate.InstructionsSheetName, sheets(1).Name)
        Assert.Equal(4, sheets(1).Table.Count)
        Assert.Equal("Yes", sheets(1).Table(1)(1))
        Assert.Equal("No", sheets(1).Table(3)(1))
        Assert.Equal("Asha Rao", sheets(1).Table(1)(3))
    End Sub

    <Fact>
    Public Sub Constructor_RefusesAnEmptyTemplate_ADuplicateNameOrAlias_AndABadSheetName()
        Dim one As New List(Of ImportColumn) From {New ImportColumn("Name", True, "x")}
        Assert.Throws(Of ArgumentException)(Function() New ImportTemplate("T", New List(Of ImportColumn)()))
        Assert.Throws(Of ArgumentException)(Function() New ImportTemplate("Instructions", one))
        Assert.Throws(Of ArgumentException)(Function() New ImportTemplate("Bad/Name", one))
        Assert.Throws(Of ArgumentException)(Function() New ImportTemplate("T", New List(Of ImportColumn) From {
            New ImportColumn("Name", True, "x"), New ImportColumn("Full name", False, "y", "", New String() {"NAME"})}))
    End Sub

    <Fact>
    Public Sub Normalize_KeepsLettersAndDigitsOnly()
        Assert.Equal("dateofbirth", ImportTemplate.Normalize(" Date_of Birth* "))
        Assert.Equal("esiip2", ImportTemplate.Normalize("ESI IP-2"))
        Assert.Equal(String.Empty, ImportTemplate.Normalize(Nothing))
    End Sub
End Class
