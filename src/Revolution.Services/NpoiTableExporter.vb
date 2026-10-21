Imports System.IO
Imports NPOI.HSSF.UserModel
Imports NPOI.SS.UserModel

''' <summary>
''' Writes Excel 97-2003 (.xls) workbooks with NPOI (CR 0014, ADR 0019). Sheets keep their order, the first row is the header,
''' every cell is stored as text with the Text number format, with no formulas and no other styling.
''' The sheets may hold plain statutory ids, so nothing here writes a temporary file, logs a cell, or puts a cell in an error message.
''' </summary>
Public NotInheritable Class NpoiTableExporter
    Implements ITableExporter

    ' Limits of the Excel 97-2003 format.
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
        Dim workbook As New HSSFWorkbook()
        Dim textStyle = workbook.CreateCellStyle()
        textStyle.DataFormat = workbook.CreateDataFormat().GetFormat("@")
        For Each sheet In sheets
            FillSheet(workbook.CreateSheet(sheet.Name), sheet.Table, textStyle)
        Next
        Using stream As New MemoryStream()
            workbook.Write(stream, True)
            Return stream.ToArray()
        End Using
    End Function

    Private Shared Sub FillSheet(target As ISheet, table As IReadOnlyList(Of String()), textStyle As ICellStyle)
        For rowIndex = 0 To table.Count - 1
            Dim row = target.CreateRow(rowIndex)
            Dim source = table(rowIndex)
            If source Is Nothing Then
                Continue For
            End If
            For columnIndex = 0 To source.Length - 1
                Dim cell = row.CreateCell(columnIndex, CellType.String)
                cell.SetCellValue(If(source(columnIndex), String.Empty))
                cell.CellStyle = textStyle
            Next
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
