Imports System.IO
Imports RevolutionAssociates.Excel
Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class ExcelTableExporterTests
    Private ReadOnly _exporter As New ExcelTableExporter()

    Private Shared Function Sheet(name As String, ParamArray rows As String()()) As ExportSheet
        Return New ExportSheet(name, rows)
    End Function

    Private Shared Function ReadWorkbook(bytes As Byte()) As Workbook
        Return ExcelFile.Read(bytes, ExcelFormat.Xls)
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
        Assert.Equal(2, workbook.Sheets.Count)
        Assert.Equal("Upload", workbook(0).Name)
        Assert.Equal("Instructions", workbook(1).Name)
        Assert.Equal("IP Number", workbook(0).GetString(0, 0))
        Assert.Equal("A Person", workbook(0).GetString(1, 1))
        Assert.Equal(2, workbook(0).RowCount)
    End Sub

    <Fact>
    Public Sub ToXls_KeepsLeadingZerosLongDigitsAndFormulaLookalikesAsText()
        Dim values = {"0012345678", "100123456789012", "1E5", "=1+1", "-5", "  padded  "}
        Dim workbook = ReadWorkbook(_exporter.ToXls({Sheet("Upload", values)}))
        For index = 0 To values.Length - 1
            Assert.IsType(Of String)(workbook(0)(0, index))
            Assert.Equal(values(index), workbook(0).GetString(0, index))
        Next
    End Sub

    <Fact>
    Public Sub ToXls_KeepsNonLatinText()
        Dim workbook = ReadWorkbook(_exporter.ToXls({Sheet("Upload", New String() {"अनुराग शर्मा", "Zoë Müller"})}))
        Assert.Equal("अनुराग शर्मा", workbook(0).GetString(0, 0))
        Assert.Equal("Zoë Müller", workbook(0).GetString(0, 1))
    End Sub

    <Fact>
    Public Sub ToXls_WritesMissingAndEmptyCellsAsEmpty()
        Dim workbook = ReadWorkbook(_exporter.ToXls({Sheet("Upload", New String() {"a", Nothing, "", "d"}, Nothing, New String() {"x"})}))
        Dim first = workbook(0)
        Assert.Equal("a", first.GetString(0, 0))
        Assert.Equal(String.Empty, first.GetString(0, 1))
        Assert.Equal(String.Empty, first.GetString(0, 2))
        Assert.Equal("d", first.GetString(0, 3))
        Assert.Equal("x", first.GetString(2, 0))
    End Sub

    <Fact>
    Public Sub ToXls_KeepsALongCell_AtTheLimit()
        Dim longText = New String("é"c, 32767)
        Dim workbook = ReadWorkbook(_exporter.ToXls({Sheet("Upload", New String() {longText})}))
        Assert.Equal(longText, workbook(0).GetString(0, 0))
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
    Public Sub ToXls_RejectsACellThatIsTooLong_WithoutQuotingIt()
        Dim tooLong = "SECRET" & New String("x"c, 32767)
        Dim ex = Assert.Throws(Of ArgumentException)(Function() _exporter.ToXls({Sheet("Upload", New String() {tooLong})}))
        Assert.DoesNotContain("SECRET", ex.Message)
    End Sub

    <Fact>
    Public Sub WriteXls_SavesAFileThatReadsBack()
        Dim target = TempFile()
        Try
            Dim result = _exporter.WriteXls(target, {Sheet("Upload", New String() {"UAN"}, New String() {"100123456789"})})
            Assert.True(result.Succeeded, result.Message)
            Dim workbook = ReadWorkbook(File.ReadAllBytes(target))
            Assert.Equal("100123456789", workbook(0).GetString(1, 0))
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
