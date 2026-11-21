Imports Revolution.Core

''' <summary>Writes the Monthly Pack files into the folder the user chose. An existing file is never overwritten: the new one gets a number (ADR 0027 point 3). Returns the path written.</summary>
Public Interface IPackFileWriter
    Function SaveText(folder As String, fileName As String, text As String) As OperationResult(Of String)

    Function SaveSheets(folder As String, fileName As String, sheets As IReadOnlyList(Of ExportSheet)) As OperationResult(Of String)
End Interface
