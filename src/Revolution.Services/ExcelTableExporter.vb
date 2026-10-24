Imports System.IO
Imports RevolutionAssociates.Excel

''' <summary>
''' Writes Excel 97-2003 (.xls) workbooks with RevolutionAssociates.Excel (CR 0014, ADR 0019). Sheets keep their order, the first row is the
''' header, and every cell is a text cell with the Text number format: no formulas, no numbers, no dates.
''' The sheets may hold plain statutory ids, so nothing here writes a temporary file, logs a cell, or puts a cell in an error message.
''' </summary>
Public NotInheritable Class ExcelTableExporter
    Implements ITableExporter

    ' Limits of the Excel 97-2003 format. They are checked here, with plain messages, before the library is called.
    Private Const MaxRows As Integer = 65536
    Private Const MaxColumns As Integer = 256
    Private Const MaxCellLength As Integer = 32767

    Public Function WriteXls(path As String, sheets As IReadOnlyList(Of ExportSheet)) As OperationResult Implements ITableExporter.WriteXls
        If String.IsNullOrWhiteSpace(path) Then
            Return OperationResult.Fail("Choose a file to save to.")
        End If
        Dim problem = Validate(sheets)
        If problem.Length > 0 Then
            Return OperationResult.Fail(problem)
        End If
        Dim bytes = Build(sheets)
        Dim existedBefore = File.Exists(path)
        Try
            File.WriteAllBytes(path, bytes)
            Return OperationResult.Ok()
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse
                                   TypeOf ex Is ArgumentException OrElse TypeOf ex Is NotSupportedException
            RemoveHalfWrittenFile(path, existedBefore)
            Return OperationResult.Fail("The file could not be saved. Check that the folder exists, that you may write there, and that the file is not open in Excel.")
        End Try
    End Function

    Public Function ToXls(sheets As IReadOnlyList(Of ExportSheet)) As Byte() Implements ITableExporter.ToXls
        Dim problem = Validate(sheets)
        If problem.Length > 0 Then
            Throw New ArgumentException(problem)
        End If
        Return Build(sheets)
    End Function

    ''' <summary>Returns the first problem as a sentence, or an empty string. Never quotes a cell.</summary>
    Private Shared Function Validate(sheets As IReadOnlyList(Of ExportSheet)) As String
        If sheets Is Nothing OrElse sheets.Count = 0 Then
            Return "A workbook needs at least one sheet."
        End If
        Dim names As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each sheet In sheets
            If sheet Is Nothing Then
                Return "A sheet is missing."
            End If
            If Not names.Add(sheet.Name) Then
                Return $"Two sheets are called '{sheet.Name}'. Sheet names must differ, ignoring upper and lower case."
            End If
            Dim problem = ValidateSheet(sheet)
            If problem.Length > 0 Then
                Return problem
            End If
        Next
        Return String.Empty
    End Function

    Private Shared Function ValidateSheet(sheet As ExportSheet) As String
        If sheet.Table.Count > MaxRows Then
            Return $"Sheet '{sheet.Name}' has {sheet.Table.Count} rows. The Excel 97-2003 format allows {MaxRows}."
        End If
        For Each source In sheet.Table
            If source Is Nothing Then
                Continue For
            End If
            If source.Length > MaxColumns Then
                Return $"Sheet '{sheet.Name}' has a row with {source.Length} columns. The Excel 97-2003 format allows {MaxColumns}."
            End If
            If source.Any(Function(value) value IsNot Nothing AndAlso value.Length > MaxCellLength) Then
                Return $"Sheet '{sheet.Name}' has a cell longer than {MaxCellLength} characters, which the Excel 97-2003 format cannot hold."
            End If
        Next
        Return String.Empty
    End Function

    Private Shared Function Build(sheets As IReadOnlyList(Of ExportSheet)) As Byte()
        Dim book As New Workbook()
        For Each sheet In sheets
            Dim target = book.AddSheet(sheet.Name)
            For rowIndex = 0 To sheet.Table.Count - 1
                FillRow(target, rowIndex, sheet.Table(rowIndex))
            Next
        Next
        Return ExcelFile.WriteToBytes(book, ExcelFormat.Xls)
    End Function

    ''' <summary>Only text goes in. Missing and empty cells are left out, which Excel shows as empty cells.</summary>
    Private Shared Sub FillRow(target As Worksheet, rowIndex As Integer, source As String())
        If source Is Nothing Then
            Return
        End If
        For columnIndex = 0 To source.Length - 1
            If Not String.IsNullOrEmpty(source(columnIndex)) Then
                target.Set(rowIndex, columnIndex, source(columnIndex))
            End If
        Next
    End Sub

    ''' <summary>A failed write may leave part of a file with plain ids in it. Remove it, unless it was the user's file from before.</summary>
    Private Shared Sub RemoveHalfWrittenFile(path As String, existedBefore As Boolean)
        If existedBefore Then
            Return
        End If
        Try
            File.Delete(path)
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException
            ' Nothing more can be done; the caller already reports that the save failed.
        End Try
    End Sub
End Class
