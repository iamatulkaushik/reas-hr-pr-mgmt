''' <summary>Bank storage through mst.usp_EmployeeBank_*. Never receives or returns a clear account number.</summary>
Public Interface IEmployeeBankRepository
    ''' <summary>Nothing when the employee has no bank row yet.</summary>
    Function GetBank(employeeId As Integer) As BankView

    ''' <summary>Nothing when the employee has no bank row yet.</summary>
    Function GetEncryptedAccount(employeeId As Integer) As EncryptedBankAccount

    Sub SaveBank(employeeId As Integer, write As BankWrite, userId As Integer?)

    ''' <summary>Writes aud.RevealLog. Fields are names only, never values.</summary>
    Sub LogReveal(userId As Integer, employeeId As Integer, fields As String)
End Interface
