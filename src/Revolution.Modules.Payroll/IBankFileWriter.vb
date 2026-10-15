''' <summary>One implementation per bank. The Owner picks the bank; the writer builds that bank's files.</summary>
Public Interface IBankFileWriter
    ReadOnly Property BankName As String

    ''' <summary>First four characters of this bank's IFSC. Used to tell same-bank payees from other-bank payees.</summary>
    ReadOnly Property IfscPrefix As String
    Function Build(batch As BankBatch) As BankFiles
End Interface
