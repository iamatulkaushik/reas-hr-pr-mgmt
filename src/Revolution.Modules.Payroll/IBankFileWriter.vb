''' <summary>One implementation per bank. The Owner picks the bank; the writer builds that bank's files.</summary>
Public Interface IBankFileWriter
    ReadOnly Property BankName As String
    Function Build(batch As BankBatch) As BankFiles
End Interface
