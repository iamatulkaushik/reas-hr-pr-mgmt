''' <summary>
''' What a module supplies to import or enter many rows at once (ADR 0025): the check of one row, the key that finds the same item listed twice, and the save of one row.
''' Messages say what is wrong with a field by its column name and never repeat an id or other cell value. The module's own service does the real work
''' (permission, validation, audit), so a row is saved exactly as it would be on the single screen.
''' </summary>
Public Interface IBulkRowHandler
    ''' <summary>Empty when the row is acceptable; otherwise one plain sentence for the first problem (or a few joined). Must not save anything.</summary>
    Function Validate(row As ImportRow) As String

    ''' <summary>Identifies the person or item (for example the Aadhaar number, or name and date of birth) so that a repeat in the same list is refused. Empty skips the check.</summary>
    Function RowKey(row As ImportRow) As String

    ''' <summary>Saves one row that passed Validate. The text of a successful result is a short note shown against the row, for example the new employee code.</summary>
    Function Save(row As ImportRow) As OperationResult(Of String)
End Interface
