Imports System.IO
Imports NPOI.HSSF.UserModel
Imports NPOI.SS.UserModel
Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class NpoiTableExporterTests
    Private ReadOnly _exporter As New NpoiTableExporter()

    Private Shared Function Sheet(name As String, ParamArray rows As String()()) As ExportSheet
        Return New ExportSheet(name, rows)
    End Function

    Private Shared Function ReadWorkbook(bytes As Byte()) As HSSFWorkbook
        Return New HSSFWorkbook(New MemoryStream(bytes))
    End Function

    Private Shared Function TempFile() As String
        Return Path.Combine(Path.GetTempPath(), "rev-test-" & Guid.NewGuid().ToString("N") & ".xls")
    End Function

    <Fact>
    Public Sub ToXls_WritesTheOldBinaryFormat_NotXlsx()
        Dim bytes = _exporter.ToXls({Sheet("Upload", New String() {"A"})})
        Assert.Equal(New Byte() {&HD0, &HCF, &H11, &HE0}, bytes.Take(4).ToArray())
    End Sub

    <Fact>
    Public Sub ToXls_KeepsSheetOrderNamesAndHeaderRow()
        Dim bytes = _exporter.ToXls({
            Sheet("Upload", New String() {"IP Number", "Name"}, New String() {"1234567890", "A Person"}),
            Sheet("Instructions", New String() {"Step"}, New String() {"Upload this file"})})
        Dim workbook = ReadWorkbook(bytes)
        Assert.Equal(2, workbook.NumberOfSheets)
        Assert.Equal("Upload", workbook.GetSheetName(0))
        Assert.Equal("Instructions", workbook.GetSheetName(1))
        Assert.Equal("IP Number", workbook.GetSheetAt(0).GetRow(0).GetCell(0).StringCellValue)
        Assert.Equal("A Person", workbook.GetSheetAt(0).GetRow(1).GetCell(1).StringCellValue)
        Assert.Equal(1, workbook.GetSheetAt(0).LastRowNum)
    End Sub

    <Fact>
    Public Sub ToXls_KeepsLeadingZerosLongDigitsAndFormulaLookalikesAsText()
        Dim values = {"0012345678", "100123456789012", "1E5", "=1+1", "-5"}
        Dim workbook = ReadWorkbook(_exporter.ToXls({Sheet("Upload", values)}))
        Dim row = workbook.GetSheetAt(0).GetRow(0)
        For index = 0 To values.Length - 1
            Assert.Equal(CellType.String, row.GetCell(index).CellType)
            Assert.Equal(values(index), row.GetCell(index).StringCellValue)
        Next
    End Sub

    <Fact>
    Public Sub ToXls_WritesMissingCellsAsEmpty()
        Dim workbook = ReadWorkbook(_exporter.ToXls({Sheet("Upload", New String() {"a", Nothing, "c"}, Nothing)}))
        Dim firstSheet = workbook.GetSheetAt(0)
        Dim middle = firstSheet.GetRow(0).GetCell(1)
        Assert.True(middle Is Nothing OrElse middle.StringCellValue.Length = 0)
        Assert.Equal("c", firstSheet.GetRow(0).GetCell(2).StringCellValue)
    End Sub

    <Fact>
    Public Sub ToXls_RejectsNoSheets()
        Assert.Throws(Of ArgumentException)(Function() _exporter.ToXls(New ExportSheet() {}))
    End Sub

    <Fact>
    Public Sub ToXls_RejectsSheetNamesThatDifferOnlyInCase()
        Assert.Throws(Of ArgumentException)(Function() _exporter.ToXls({Sheet("Upload", New String() {"A"}), Sheet("upload", New String() {"A"})}))
    End Sub

    <Fact>
    Public Sub ToXls_RejectsMoreRowsThanTheFormatAllows()
        Dim rows = Enumerable.Repeat(New String() {"x"}, 65537).ToArray()
        Dim ex = Assert.Throws(Of ArgumentException)(Function() _exporter.ToXls({Sheet("Big", rows)}))
        Assert.Contains("65536", ex.Message)
    End Sub

    <Fact>
    Public Sub WriteXls_SavesAFileThatReadsBack()
        Dim target = TempFile()
        Try
            Dim result = _exporter.WriteXls(target, {Sheet("Upload", New String() {"UAN"}, New String() {"100123456789"})})
            Assert.True(result.Succeeded, result.Message)
            Dim workbook = ReadWorkbook(File.ReadAllBytes(target))
            Assert.Equal("100123456789", workbook.GetSheetAt(0).GetRow(1).GetCell(0).StringCellValue)
        Finally
            File.Delete(target)
        End Try
    End Sub

    <Fact>
    Public Sub WriteXls_FailsWithoutQuotingCells_WhenTheFolderDoesNotExist()
        Dim target = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing-folder", "out.xls")
        Dim result = _exporter.WriteXls(target, {Sheet("Upload", New String() {"SECRET-UAN-123"})})
        Assert.False(result.Succeeded)
        Assert.NotEqual(String.Empty, result.Message)
        Assert.DoesNotContain("SECRET-UAN-123", result.Message)
    End Sub

    <Fact>
    Public Sub WriteXls_RejectsABlankPath()
        Assert.False(_exporter.WriteXls(" ", {Sheet("Upload", New String() {"A"})}).Succeeded)
    End Sub

    <Fact>
    Public Sub WriteXls_ReturnsAFailureAndMakesNoFile_WhenTheSheetsAreInvalid()
        Dim target = TempFile()
        Dim result = _exporter.WriteXls(target, New ExportSheet() {})
        Assert.False(result.Succeeded)
        Assert.False(File.Exists(target))
    End Sub
End Class
