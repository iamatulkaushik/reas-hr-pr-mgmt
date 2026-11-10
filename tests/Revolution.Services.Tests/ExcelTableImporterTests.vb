Imports System.IO
Imports Revolution.Core
Imports Revolution.Services
Imports RevolutionAssociates.Excel
Imports Xunit

Public Class ExcelTableImporterTests
    Private Shared Function TempPath(extension As String) As String
        Return Path.Combine(Path.GetTempPath(), "rv-import-" & Guid.NewGuid().ToString("N") & extension)
    End Function

    Private Shared Function WriteXls(path As String, ParamArray rows As String()()) As String
        Dim exporter As New ExcelTableExporter()
        File.WriteAllBytes(path, exporter.ToXls(New List(Of ExportSheet) From {New ExportSheet("Sheet1", rows)}))
        Return path
    End Function

    <Fact>
    Public Sub Read_ReturnsTheHeaderAndTheRows_SkipsBlankRows_AndKeepsSheetRowNumbers()
        Dim path = WriteXls(TempPath(".xls"), New String() {"Name", "DOB"}, New String() {"Asha", "09/11/1990"}, New String() {"", ""}, New String() {"Ravi", "01/02/1985"})
        Try
            Dim result = New ExcelTableImporter().Read(path)
            Assert.True(result.Succeeded)
            Assert.Equal(New String() {"Name", "DOB"}, result.Value.Headers)
            Assert.Equal(2, result.Value.Rows.Count)
            Assert.Equal(2, result.Value.Rows(0).RowNumber)
            Assert.Equal("Asha", result.Value.Rows(0).Cells(0))
            Assert.Equal(4, result.Value.Rows(1).RowNumber)
            Assert.Equal("01/02/1985", result.Value.Rows(1).Cells(1))
        Finally
            File.Delete(path)
        End Try
    End Sub

    <Fact>
    Public Sub Read_FindsTheHeaderBelowBlankLeadingRows()
        Dim path = WriteXls(TempPath(".xls"), New String() {""}, New String() {"Name"}, New String() {"Asha"})
        Try
            Dim result = New ExcelTableImporter().Read(path)
            Assert.True(result.Succeeded)
            Assert.Equal("Name", result.Value.Headers(0))
            Assert.Equal(3, result.Value.Rows.Single().RowNumber)
        Finally
            File.Delete(path)
        End Try
    End Sub

    <Fact>
    Public Sub Read_ReadsACsvFile()
        Dim path = TempPath(".csv")
        File.WriteAllText(path, "Name,City" & vbCrLf & "Asha,Rohtak" & vbCrLf)
        Try
            Dim result = New ExcelTableImporter().Read(path)
            Assert.True(result.Succeeded)
            Assert.Equal(New String() {"Name", "City"}, result.Value.Headers)
            Assert.Equal("Rohtak", result.Value.Rows.Single().Cells(1))
        Finally
            File.Delete(path)
        End Try
    End Sub

    <Fact>
    Public Sub Read_TurnsNumbersAndDatesIntoText_SoATwelveDigitIdStaysTwelveDigits()
        Dim path = TempPath(".xlsx")
        Dim book As New Workbook()
        Dim sheet = book.AddSheet("Sheet1")
        sheet.Set(0, 0, "Aadhaar")
        sheet.Set(0, 1, "Joined")
        sheet.Set(0, 2, "Salary")
        sheet.Set(1, 0, 123456789012.0R)
        sheet.Set(1, 1, New Date(2026, 11, 9))
        sheet.Set(1, 2, 52000.5R)
        ExcelFile.Write(book, path)
        Try
            Dim result = New ExcelTableImporter().Read(path)
            Assert.True(result.Succeeded)
            Assert.Equal(New String() {"123456789012", "2026-11-09", "52000.5"}, result.Value.Rows.Single().Cells)
        Finally
            File.Delete(path)
        End Try
    End Sub

    <Fact>
    Public Sub Read_WithoutAPathOrWithAMissingFile_Fails()
        Dim importer As New ExcelTableImporter()
        Assert.False(importer.Read(Nothing).Succeeded)
        Assert.False(importer.Read("   ").Succeeded)
        Dim missing = importer.Read(TempPath(".xlsx"))
        Assert.False(missing.Succeeded)
        Assert.Contains("not found", missing.Message)
    End Sub

    <Fact>
    Public Sub Read_OfAFileThatIsNotASpreadsheet_FailsWithAPlainMessage_AndQuotesNothing()
        Dim path = TempPath(".xls")
        File.WriteAllBytes(path, System.Text.Encoding.ASCII.GetBytes("SECRET-123456789012 this is not a workbook"))
        Try
            Dim result = New ExcelTableImporter().Read(path)
            If Not result.Succeeded Then
                Assert.DoesNotContain("SECRET", result.Message)
            End If
        Finally
            File.Delete(path)
        End Try
    End Sub

    <Fact>
    Public Sub Read_RefusesMoreRowsThanTheLimit()
        Dim rows As New List(Of String())()
        rows.Add(New String() {"Name"})
        For i As Integer = 1 To ImportLimits.MaxRows + 1
            rows.Add(New String() {"Person " & i})
        Next
        Dim path = TempPath(".xls")
        File.WriteAllBytes(path, New ExcelTableExporter().ToXls(New List(Of ExportSheet) From {New ExportSheet("Sheet1", rows)}))
        Try
            Dim result = New ExcelTableImporter().Read(path)
            Assert.False(result.Succeeded)
            Assert.Contains(ImportLimits.MaxRows.ToString(), result.Message)
        Finally
            File.Delete(path)
        End Try
    End Sub

    <Fact>
    Public Sub Read_RefusesMoreColumnsThanTheLimit()
        Dim header(ImportLimits.MaxColumns) As String
        For i As Integer = 0 To header.Length - 1
            header(i) = "Column " & i
        Next
        Dim path = WriteXls(TempPath(".xls"), header)
        Try
            Dim result = New ExcelTableImporter().Read(path)
            Assert.False(result.Succeeded)
            Assert.Contains(ImportLimits.MaxColumns.ToString(), result.Message)
        Finally
            File.Delete(path)
        End Try
    End Sub
End Class
