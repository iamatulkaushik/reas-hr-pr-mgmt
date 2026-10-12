Imports Revolution.Modules.Employees

Friend NotInheritable Class FakeBankRepository
    Implements IEmployeeBankRepository

    Public ReadOnly Banks As New Dictionary(Of Integer, BankView)()
    Public ReadOnly Accounts As New Dictionary(Of Integer, EncryptedBankAccount)()
    Public ReadOnly Writes As New List(Of BankWrite)()
    Public ReadOnly RevealLog As New List(Of String)()
    Public MissingEmployee As Boolean

    Public Function GetBank(employeeId As Integer) As BankView Implements IEmployeeBankRepository.GetBank
        Dim found As BankView = Nothing
        Banks.TryGetValue(employeeId, found)
        Return found
    End Function

    Public Function GetEncryptedAccount(employeeId As Integer) As EncryptedBankAccount Implements IEmployeeBankRepository.GetEncryptedAccount
        Dim found As EncryptedBankAccount = Nothing
        Accounts.TryGetValue(employeeId, found)
        Return found
    End Function

    Public Sub SaveBank(employeeId As Integer, write As BankWrite, userId As Integer?) Implements IEmployeeBankRepository.SaveBank
        If MissingEmployee Then Throw New EmployeeRuleException("Employee not found.")
        Writes.Add(write)
        Dim view As BankView = GetBank(employeeId)
        Dim last4 As String = If(view Is Nothing, String.Empty, view.AccountLast4)
        Dim stored As EncryptedBankAccount = GetEncryptedAccount(employeeId)
        Select Case write.Account.Kind
            Case IdChangeKind.Replaced
                Accounts(employeeId) = New EncryptedBankAccount(write.KeyVersion, write.Account.Value.Cipher)
                last4 = write.Account.Value.Last4
            Case IdChangeKind.Cleared
                Accounts(employeeId) = New EncryptedBankAccount(write.KeyVersion, Nothing)
                last4 = String.Empty
            Case Else
                If stored Is Nothing Then Accounts(employeeId) = New EncryptedBankAccount(write.KeyVersion, Nothing)
        End Select
        Banks(employeeId) = New BankView With {.PaymentMode = write.PaymentMode, .BankId = write.BankId, .Ifsc = write.Ifsc, .AccountLast4 = last4}
    End Sub

    Public Sub LogReveal(userId As Integer, employeeId As Integer, fields As String) Implements IEmployeeBankRepository.LogReveal
        RevealLog.Add($"{userId}:{employeeId}:{fields}")
    End Sub
End Class

Friend NotInheritable Class FakeFlagsRepository
    Implements IStatutoryFlagsRepository

    Public ReadOnly Rows As New Dictionary(Of Integer, StatutoryFlags)()
    Public MissingEmployee As Boolean

    Public Function GetFlags(employeeId As Integer) As StatutoryFlags Implements IStatutoryFlagsRepository.GetFlags
        Dim found As StatutoryFlags = Nothing
        Rows.TryGetValue(employeeId, found)
        Return found
    End Function

    Public Sub SaveFlags(employeeId As Integer, flags As StatutoryFlags, userId As Integer?) Implements IStatutoryFlagsRepository.SaveFlags
        If MissingEmployee Then Throw New EmployeeRuleException("Employee not found.")
        Rows(employeeId) = flags
    End Sub
End Class
