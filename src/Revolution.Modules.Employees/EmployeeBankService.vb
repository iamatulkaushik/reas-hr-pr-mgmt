Imports System.Globalization
Imports System.Security.Cryptography
Imports Revolution.Core

''' <summary>
''' Saves, shows and reveals the bank account (ADR 0011). The account is encrypted before it reaches the repository.
''' Audit and reveal log hold field names only, never the number.
''' </summary>
Public NotInheritable Class EmployeeBankService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const Unreadable As String = "The account number could not be read. Check that the key file is the right one."
    Private Const RevealedField As String = "BANKACCOUNT"

    Private ReadOnly _repository As IEmployeeBankRepository
    Private ReadOnly _crypto As IFieldCrypto
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext

    Public Sub New(repository As IEmployeeBankRepository, crypto As IFieldCrypto, permissions As IPermissionService,
                   audit As IAuditService, session As ISessionContext)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If crypto Is Nothing Then Throw New ArgumentNullException(NameOf(crypto))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        _repository = repository
        _crypto = crypto
        _permissions = permissions
        _audit = audit
        _session = session
    End Sub

    ''' <summary>Payment mode, bank, IFSC and the last four characters of the account. Needs View on Employees.</summary>
    Public Function GetMasked(employeeId As Integer) As OperationResult(Of BankView)
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.View) Then
            Return OperationResult(Of BankView).Fail(NotAllowed)
        End If
        If employeeId <= 0 Then
            Return OperationResult(Of BankView).Fail("Pick an employee.")
        End If
        Return OperationResult(Of BankView).Ok(If(_repository.GetBank(employeeId), New BankView()))
    End Function

    ''' <summary>Saves payment mode, bank and IFSC, and replaces or clears the account when asked. Needs Edit on Employees.</summary>
    Public Function Save(employeeId As Integer, details As BankDetails) As OperationResult
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.Edit) Then
            Return OperationResult.Fail(NotAllowed)
        End If
        If details Is Nothing Then Throw New ArgumentNullException(NameOf(details))
        If employeeId <= 0 Then
            Return OperationResult.Fail("Pick an employee.")
        End If
        Dim user = _session.Current
        If user Is Nothing Then
            Return OperationResult.Fail("Sign in first.")
        End If
        Try
            Dim current = _repository.GetBank(employeeId)
            Dim problems = BankDetailsValidator.Validate(details, current IsNot Nothing AndAlso current.HasAccount)
            If problems.Count > 0 Then
                Return OperationResult.Fail(String.Join(" ", problems))
            End If
            _repository.SaveBank(employeeId, BuildWrite(employeeId, user.CompanyId, details), user.UserId)
        Catch ex As EmployeeRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Employees, "SaveBank", "Employee",
                                            employeeId.ToString(CultureInfo.InvariantCulture),
                                            $"Mode {details.PaymentMode}; account {AccountOutcome(details.Account)}"))
    End Function

    ''' <summary>Shows the account number in clear. Needs Reveal on Employees and writes the reveal log before returning it.</summary>
    Public Function RevealAccount(employeeId As Integer) As OperationResult(Of String)
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.Reveal) Then
            Return OperationResult(Of String).Fail(NotAllowed)
        End If
        If employeeId <= 0 Then
            Return OperationResult(Of String).Fail("Pick an employee.")
        End If
        Dim user = _session.Current
        If user Is Nothing Then
            Return OperationResult(Of String).Fail("Sign in first.")
        End If
        Try
            Dim stored = _repository.GetEncryptedAccount(employeeId)
            If stored Is Nothing OrElse stored.Cipher Is Nothing Then
                Return OperationResult(Of String).Ok(String.Empty)
            End If
            Dim account = _crypto.Decrypt(stored.Cipher, stored.KeyVersion, FieldContext.Build(user.CompanyId, employeeId, FieldContext.BankAccount))
            _repository.LogReveal(user.UserId, employeeId, RevealedField)
            Return OperationResult(Of String).Ok(account)
        Catch ex As EmployeeRuleException
            Return OperationResult(Of String).Fail(ex.Message)
        Catch ex As CryptographicException
            Return OperationResult(Of String).Fail(Unreadable)
        End Try
    End Function

    Private Function BuildWrite(employeeId As Integer, companyId As Integer, details As BankDetails) As BankWrite
        Dim change As IdChange
        Select Case details.Account.Action
            Case IdAction.Replace
                change = IdChange.ReplacedWith(Protect(details.Account.Plain, companyId, employeeId))
            Case IdAction.Clear
                change = IdChange.Cleared
            Case Else
                change = IdChange.Unchanged
        End Select
        Dim ifsc As String = IdNormalizer.Compact(If(details.Ifsc, String.Empty)).ToUpperInvariant()
        Return New BankWrite(details.PaymentMode, details.BankId, ifsc, _crypto.CurrentKeyVersion, change)
    End Function

    Private Function Protect(plain As String, companyId As Integer, employeeId As Integer) As ProtectedId
        Dim number As String = IdNormalizer.Compact(plain)
        Dim cipher = _crypto.Encrypt(number, FieldContext.Build(companyId, employeeId, FieldContext.BankAccount))
        Return New ProtectedId(cipher, number.Substring(Math.Max(0, number.Length - 4)))
    End Function

    Private Shared Function AccountOutcome(edit As IdEdit) As String
        Select Case edit.Action
            Case IdAction.Replace
                Return "changed"
            Case IdAction.Clear
                Return "cleared"
            Case Else
                Return "kept"
        End Select
    End Function
End Class
