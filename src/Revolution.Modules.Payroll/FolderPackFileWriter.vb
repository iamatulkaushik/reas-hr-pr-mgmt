Imports System.Globalization
Imports System.IO
Imports System.Text
Imports Revolution.Core

''' <summary>IPackFileWriter on the file system. The folder is created when it does not exist; "name.ext" becomes "name (2).ext", "name (3).ext" when taken. Workbooks go through ITableExporter.</summary>
Public NotInheritable Class FolderPackFileWriter
    Implements IPackFileWriter

    Private Const MaxTries As Integer = 999

    Private ReadOnly _exporter As ITableExporter

    Public Sub New(exporter As ITableExporter)
        If exporter Is Nothing Then Throw New ArgumentNullException(NameOf(exporter))
        _exporter = exporter
    End Sub

    Public Function SaveText(folder As String, fileName As String, text As String) As OperationResult(Of String) Implements IPackFileWriter.SaveText
        Dim path = FreePath(folder, fileName)
        If Not path.Succeeded Then Return path
        Try
            File.WriteAllText(path.Value, text, New UTF8Encoding(False))
        Catch ex As IOException
            Return OperationResult(Of String).Fail("The file could not be written to the folder.")
        Catch ex As UnauthorizedAccessException
            Return OperationResult(Of String).Fail("The file could not be written to the folder.")
        End Try
        Return path
    End Function

    Public Function SaveSheets(folder As String, fileName As String, sheets As IReadOnlyList(Of ExportSheet)) As OperationResult(Of String) Implements IPackFileWriter.SaveSheets
        Dim path = FreePath(folder, fileName)
        If Not path.Succeeded Then Return path
        Dim written = _exporter.WriteXls(path.Value, sheets)
        If Not written.Succeeded Then Return OperationResult(Of String).Fail(written.Message)
        Return path
    End Function

    Private Shared Function FreePath(folder As String, fileName As String) As OperationResult(Of String)
        If String.IsNullOrWhiteSpace(folder) Then Return OperationResult(Of String).Fail("Choose a folder.")
        Dim safeName = System.IO.Path.GetFileName(fileName)
        If String.IsNullOrWhiteSpace(safeName) Then Return OperationResult(Of String).Fail("The file name is not valid.")
        Try
            Directory.CreateDirectory(folder)
        Catch ex As IOException
            Return OperationResult(Of String).Fail("The folder could not be created.")
        Catch ex As UnauthorizedAccessException
            Return OperationResult(Of String).Fail("The folder could not be created.")
        Catch ex As ArgumentException
            Return OperationResult(Of String).Fail("The folder is not valid.")
        End Try
        Dim stem = System.IO.Path.GetFileNameWithoutExtension(safeName)
        Dim ext = System.IO.Path.GetExtension(safeName)
        Dim candidate = System.IO.Path.Combine(folder, safeName)
        Dim n As Integer = 2
        While File.Exists(candidate)
            If n > MaxTries Then Return OperationResult(Of String).Fail("Too many files with this name in the folder.")
            candidate = System.IO.Path.Combine(folder, $"{stem} ({n.ToString(CultureInfo.InvariantCulture)}){ext}")
            n += 1
        End While
        Return OperationResult(Of String).Ok(candidate)
    End Function
End Class
