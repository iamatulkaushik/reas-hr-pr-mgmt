Imports System.Globalization
Imports System.Security.Cryptography
Imports Revolution.Core

''' <summary>
''' Saves, shows and reveals the protected ids (ADR 0011). Plain ids live only inside these methods.
''' A row has one key version (IdKeyVersion), so when the key has moved on, a save re-encrypts every id on the row together.
''' Nothing here logs a plain or cipher value.
''' </summary>
Public NotInheritable Class EmployeeIdentifierService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const Unreadable As String = "An id could not be read. Check that the key file is the right one."

    Private ReadOnly _repository As IEmployeeRepository
    Private ReadOnly _crypto As IFieldCrypto
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext

    Public Sub New(repository As IEmployeeRepository, crypto As IFieldCrypto, permissions As IPermissionService,
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

    ''' <summary>Replaces or clears ids. Needs Edit on Employees. A duplicate id in the company is refused by the database.</summary>
    Public Function Save(employeeId As Integer, input As IdentifierInput) As OperationResult
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.Edit) Then
            Return OperationResult.Fail(NotAllowed)
        End If
        Return SaveCore(employeeId, input)
    End Function

    ''' <summary>Same as Save without the permission check. EmployeeService calls it after its own check.</summary>
    Friend Function SaveCore(employeeId As Integer, input As IdentifierInput) As OperationResult
        If input Is Nothing Then Throw New ArgumentNullException(NameOf(input))
        If employeeId <= 0 Then
            Return OperationResult.Fail("Pick an employee.")
        End If
        If Not input.HasChanges Then
            Return OperationResult.Ok()
        End If
        Dim problems = IdentifierValidator.Validate(input)
        If problems.Count > 0 Then
            Return OperationResult.Fail(String.Join(" ", problems))
        End If
        Dim user = _session.Current
        If user Is Nothing Then
            Return OperationResult.Fail("Sign in first.")
        End If
        Try
            _repository.SetIdentifiers(employeeId, BuildWrite(employeeId, user.CompanyId, input), user.UserId)
        Catch ex As EmployeeRuleException
            Return OperationResult.Fail(ex.Message)
        Catch ex As CryptographicException
            Return OperationResult.Fail(Unreadable)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Employees, "SetIdentifiers", "Employee",
                                            employeeId.ToString(CultureInfo.InvariantCulture), "Changed: " & ChangedNames(input)))
    End Function

    ''' <summary>Last four characters of each id. Needs View on Employees.</summary>
    Public Function GetMasked(employeeId As Integer) As OperationResult(Of IdentifierLast4)
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.View) Then
            Return OperationResult(Of IdentifierLast4).Fail(NotAllowed)
        End If
        If employeeId <= 0 Then
            Return OperationResult(Of IdentifierLast4).Fail("Pick an employee.")
        End If
        Return OperationResult(Of IdentifierLast4).Ok(If(_repository.GetIdentifierLast4(employeeId), New IdentifierLast4()))
    End Function

    ''' <summary>Shows ids in clear. Needs Reveal on Employees and writes the reveal log before returning anything.</summary>
    Public Function Reveal(employeeId As Integer, fields As IdField) As OperationResult(Of RevealedIds)
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.Reveal) Then
            Return OperationResult(Of RevealedIds).Fail(NotAllowed)
        End If
        If employeeId <= 0 OrElse fields = IdField.None Then
            Return OperationResult(Of RevealedIds).Fail("Pick an employee and an id.")
        End If
        Dim user = _session.Current
        If user Is Nothing Then
            Return OperationResult(Of RevealedIds).Fail("Sign in first.")
        End If
        Try
            Dim revealed = DecryptRequested(_repository.GetEncryptedIdentifiers(employeeId), fields, user.CompanyId, employeeId)
            If revealed.Any Then
                _repository.LogReveal(user.UserId, employeeId, FieldNames(fields))
            End If
            Return OperationResult(Of RevealedIds).Ok(revealed)
        Catch ex As EmployeeRuleException
            Return OperationResult(Of RevealedIds).Fail(ex.Message)
        Catch ex As CryptographicException
            Return OperationResult(Of RevealedIds).Fail(Unreadable)
        End Try
    End Function

    ''' <summary>Decrypts the named ids of one employee with no permission check and no log row. Only EmployeeExportService calls it, after it has written the batch log.</summary>
    Friend Function DecryptForExport(employeeId As Integer, fields As IdField, companyId As Integer) As RevealedIds
        Return DecryptRequested(_repository.GetEncryptedIdentifiers(employeeId), fields, companyId, employeeId)
    End Function

    Private Function DecryptRequested(stored As EncryptedIdentifiers, fields As IdField, companyId As Integer, employeeId As Integer) As RevealedIds
        Dim result As New RevealedIds()
        If stored Is Nothing Then
            Return result
        End If
        If fields.HasFlag(IdField.Uan) Then result.Uan = Open(stored.Uan, stored.KeyVersion, FieldContext.Uan, companyId, employeeId)
        If fields.HasFlag(IdField.EsiIp) Then result.EsiIp = Open(stored.EsiIp, stored.KeyVersion, FieldContext.EsiIp, companyId, employeeId)
        If fields.HasFlag(IdField.Pan) Then result.Pan = Open(stored.Pan, stored.KeyVersion, FieldContext.Pan, companyId, employeeId)
        If fields.HasFlag(IdField.Aadhaar) Then result.Aadhaar = Open(stored.Aadhaar, stored.KeyVersion, FieldContext.Aadhaar, companyId, employeeId)
        Return result
    End Function

    Private Function Open(cipher As Byte(), keyVersion As Byte, field As String, companyId As Integer, employeeId As Integer) As String
        If cipher Is Nothing Then
            Return String.Empty
        End If
        Return _crypto.Decrypt(cipher, keyVersion, FieldContext.Build(companyId, employeeId, field))
    End Function

    Private Function BuildWrite(employeeId As Integer, companyId As Integer, input As IdentifierInput) As IdentifierWrite
        Dim stored = _repository.GetEncryptedIdentifiers(employeeId)
        Dim moveKey As Boolean = stored IsNot Nothing AndAlso stored.HasAny AndAlso stored.KeyVersion <> _crypto.CurrentKeyVersion
        Dim storedVersion As Byte = If(stored Is Nothing, CByte(0), stored.KeyVersion)
        Return New IdentifierWrite(_crypto.CurrentKeyVersion,
            Resolve(FieldContext.Uan, input.Uan, If(stored Is Nothing, Nothing, stored.Uan), storedVersion, moveKey, companyId, employeeId),
            Resolve(FieldContext.EsiIp, input.EsiIp, If(stored Is Nothing, Nothing, stored.EsiIp), storedVersion, moveKey, companyId, employeeId),
            Resolve(FieldContext.Pan, input.Pan, If(stored Is Nothing, Nothing, stored.Pan), storedVersion, moveKey, companyId, employeeId),
            Resolve(FieldContext.Aadhaar, input.Aadhaar, If(stored Is Nothing, Nothing, stored.Aadhaar), storedVersion, moveKey, companyId, employeeId))
    End Function

    Private Function Resolve(field As String, edit As IdEdit, storedCipher As Byte(), storedVersion As Byte,
                             moveKey As Boolean, companyId As Integer, employeeId As Integer) As IdChange
        Select Case edit.Action
            Case IdAction.Clear
                Return IdChange.Cleared
            Case IdAction.Replace
                Return IdChange.ReplacedWith(Protect(field, edit.Plain, companyId, employeeId))
            Case Else
                If moveKey AndAlso storedCipher IsNot Nothing Then
                    Dim plain = _crypto.Decrypt(storedCipher, storedVersion, FieldContext.Build(companyId, employeeId, field))
                    Return IdChange.ReplacedWith(Protect(field, plain, companyId, employeeId))
                End If
                Return IdChange.Unchanged
        End Select
    End Function

    Private Function Protect(field As String, plain As String, companyId As Integer, employeeId As Integer) As ProtectedId
        Dim id As String = IdNormalizer.Compact(plain).ToUpperInvariant()
        Dim cipher = _crypto.Encrypt(id, FieldContext.Build(companyId, employeeId, field))
        Dim hash = _crypto.BlindIndex(companyId, field, id)
        Return New ProtectedId(cipher, hash, id.Substring(Math.Max(0, id.Length - 4)))
    End Function

    Private Shared Function ChangedNames(input As IdentifierInput) As String
        Dim names As New List(Of String)()
        If input.Uan.Action <> IdAction.Unchanged Then names.Add("UAN")
        If input.EsiIp.Action <> IdAction.Unchanged Then names.Add("ESI IP")
        If input.Pan.Action <> IdAction.Unchanged Then names.Add("PAN")
        If input.Aadhaar.Action <> IdAction.Unchanged Then names.Add("Aadhaar")
        Return String.Join(", ", names)
    End Function

    Private Shared Function FieldNames(fields As IdField) As String
        Dim names As New List(Of String)()
        If fields.HasFlag(IdField.Uan) Then names.Add("UAN")
        If fields.HasFlag(IdField.EsiIp) Then names.Add("ESIIP")
        If fields.HasFlag(IdField.Pan) Then names.Add("PAN")
        If fields.HasFlag(IdField.Aadhaar) Then names.Add("AADHAAR")
        Return String.Join(",", names)
    End Function
End Class
