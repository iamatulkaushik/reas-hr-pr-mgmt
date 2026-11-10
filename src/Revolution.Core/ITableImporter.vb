''' <summary>
''' Reads a spreadsheet file (.xlsx, .xls, .csv, .tsv) into an ImportTable of text cells (ADR 0025). The first row with anything in it is the header; blank rows are skipped;
''' each data row keeps its row number in the sheet. Fails with a plain message, never quoting a cell, when the file is missing, too large, too long or cannot be read.
''' </summary>
Public Interface ITableImporter
    Function Read(path As String) As OperationResult(Of ImportTable)
End Interface
