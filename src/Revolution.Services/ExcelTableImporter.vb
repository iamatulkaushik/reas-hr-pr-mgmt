Imports System.Globalization
Imports System.IO
Imports RevolutionAssociates.Excel

''' <summary>
''' Reads .xlsx, .xls, .csv and .tsv files with RevolutionAssociates.Excel into an ImportTable of text cells (ADR 0025). Only the first sheet is read, only values (no formula is
''' evaluated, no macro runs), and every cell becomes text: numbers without a trailing .0 (a 12-digit Aadhaar typed as a number stays 12 digits), dates as yyyy-mm-dd.
''' The sheets may hold plain statutory ids, so nothing here writes a temporary file, logs a cell, or puts a cell in an error message.
''' </summary>
Public NotInheritable Class ExcelTableImporter
    Implements ITableImporter

    Private Const Unreadable As String = "The file could not be read. Use a .xlsx, .xls or .csv file that is not open in Excel."

    Public Function Read(path As String) As OperationResult(Of ImportTable) Implements ITableImporter.Read
        If String.IsNullOrWhiteSpace(path) Then
            Return OperationResult(Of ImportTable).Fail("Choose a file to import.")
        End If
        Try
            Dim info As New FileInfo(path)
            If Not info.Exists Then
                Return OperationResult(Of ImportTable).Fail("The file was not found.")
            End If
            If info.Length > ImportLimits.MaxFileBytes Then
                Return OperationResult(Of ImportTable).Fail($"The file is larger than {ImportLimits.MaxFileBytes \ (1024L * 1024L)} MB. Split it into smaller files.")
            End If
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse
                                   TypeOf ex Is ArgumentException OrElse TypeOf ex Is NotSupportedException
            Return OperationResult(Of ImportTable).Fail(Unreadable)
        End Try
        Dim book As Workbook
        Try
            book = ExcelFile.Read(path)
        Catch ex As Exception
            ' The library throws its own exception types for a damaged or unknown file; the text of any of them could hold a cell.
            Return OperationResult(Of ImportTable).Fail(Unreadable)
        End Try
        If book Is Nothing OrElse book.Sheets.Count = 0 Then
            Return OperationResult(Of ImportTable).Fail("The file has no sheet.")
        End If
        Return Build(book.Sheets(0))
    End Function

    Private Shared Function Build(sheet As Worksheet) As OperationResult(Of ImportTable)
        Dim headerRow As Integer = -1
        For r As Integer = 0 To sheet.RowCount - 1
            If HasContent(sheet, r) Then
                headerRow = r
                Exit For
            End If
        Next
        If headerRow < 0 Then
            Return OperationResult(Of ImportTable).Fail("The file is empty. The first row must hold the column names.")
        End If
        Dim columnCount As Integer = sheet.ColumnCount
        If columnCount > ImportLimits.MaxColumns Then
            Return OperationResult(Of ImportTable).Fail($"The file has more than {ImportLimits.MaxColumns} columns.")
        End If
        Dim headers As New List(Of String)()
        For c As Integer = 0 To columnCount - 1
            headers.Add(CellText(sheet.Get(headerRow, c)))
        Next
        Dim rows As New List(Of RawImportRow)()
        For r As Integer = headerRow + 1 To sheet.RowCount - 1
            If Not HasContent(sheet, r) Then
                Continue For
            End If
            If rows.Count >= ImportLimits.MaxRows Then
                Return OperationResult(Of ImportTable).Fail($"The file has more than {ImportLimits.MaxRows} rows. Split it into smaller files.")
            End If
            Dim cells(columnCount - 1) As String
            For c As Integer = 0 To columnCount - 1
                cells(c) = CellText(sheet.Get(r, c))
            Next
            rows.Add(New RawImportRow(r + 1, cells))
        Next
        Return OperationResult(Of ImportTable).Ok(New ImportTable(headers, rows))
    End Function

    Private Shared Function HasContent(sheet As Worksheet, row As Integer) As Boolean
        For c As Integer = 0 To sheet.ColumnCount - 1
            If CellText(sheet.Get(row, c)).Length > 0 Then
                Return True
            End If
        Next
        Return False
    End Function

    ''' <summary>Every kind of cell value as text. Numbers keep up to 10 decimals and no trailing zeros; a whole number has no decimal point.</summary>
    Private Shared Function CellText(value As Object) As String
        If value Is Nothing Then
            Return String.Empty
        End If
        Dim text As String = TryCast(value, String)
        If text IsNot Nothing Then
            Return text.Trim()
        End If
        If TypeOf value Is Double Then
            Return NumberText(CDbl(value))
        End If
        If TypeOf value Is Date Then
            Dim moment As Date = CDate(value)
            Return moment.ToString(If(moment.TimeOfDay = TimeSpan.Zero, "yyyy-MM-dd", "yyyy-MM-dd HH:mm"), CultureInfo.InvariantCulture)
        End If
        If TypeOf value Is Boolean Then
            Return If(CBool(value), "TRUE", "FALSE")
        End If
        Return Convert.ToString(value, CultureInfo.InvariantCulture)
    End Function

    Private Shared Function NumberText(number As Double) As String
        If Double.IsNaN(number) OrElse Double.IsInfinity(number) Then
            Return "#ERROR"
        End If
        If Math.Abs(number) >= 7.9E+27 Then
            Return number.ToString("R", CultureInfo.InvariantCulture)
        End If
        Return Decimal.Round(CDec(number), 10).ToString("0.##########", CultureInfo.InvariantCulture)
    End Function
End Class
