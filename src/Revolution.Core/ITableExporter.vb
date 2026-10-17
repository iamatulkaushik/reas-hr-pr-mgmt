''' <summary>
''' Writes text tables as an Excel 97-2003 (.xls) workbook (CR 0014). Sheets keep their order, every cell is stored as text,
''' the first row of each sheet is the header. Plain statutory ids may be inside the sheets, so what calls this is the export rule in ADR 0019.
''' </summary>
Public Interface ITableExporter
    ''' <summary>Writes the workbook to a file. Fails with a message when the file cannot be written.</summary>
    Function WriteXls(path As String, sheets As IReadOnlyList(Of ExportSheet)) As OperationResult
    ''' <summary>The workbook as bytes, for tests and for callers that write the file themselves.</summary>
    Function ToXls(sheets As IReadOnlyList(Of ExportSheet)) As Byte()
End Interface
