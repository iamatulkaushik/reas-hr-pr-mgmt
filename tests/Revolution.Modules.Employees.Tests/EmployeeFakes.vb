Imports System.Security.Cryptography
Imports System.Text
Imports Revolution.Core
Imports Revolution.Modules.Employees

''' <summary>Stands in for AES-GCM. Keeps the context and key version inside the "cipher" so a wrong context or version fails like the real one.</summary>
Friend NotInheritable Class FakeFieldCrypto
    Implements IFieldCrypto

    Public Version As Byte = 1
    Public FailDecrypt As Boolean

    Public ReadOnly Property CurrentKeyVersion As Byte Implements IFieldCrypto.CurrentKeyVersion
        Get
            Return Version
        End Get
    End Property

    Public Function Encrypt(plain As String, context As String) As Byte() Implements IFieldCrypto.Encrypt
        Return Encoding.UTF8.GetBytes($"v{Version}|{context}|{plain}")
    End Function

    Public Function Decrypt(cipher As Byte(), keyVersion As Byte, context As String) As String Implements IFieldCrypto.Decrypt
        Dim text = Encoding.UTF8.GetString(cipher)
        Dim head = $"v{keyVersion}|{context}|"
        If FailDecrypt OrElse Not text.StartsWith(head, StringComparison.Ordinal) Then
            Throw New CryptographicException("Wrong key, version or context.")
        End If
        Return text.Substring(head.Length)
    End Function

    Public Function BlindIndex(companyId As Integer, field As String, plain As String) As Byte() Implements IFieldCrypto.BlindIndex
        Return SHA256.HashData(Encoding.UTF8.GetBytes($"{companyId}:{field}:{plain}"))
    End Function
End Class

Friend NotInheritable Class FakeEmployeeRepository
    Implements IEmployeeRepository

    Public ReadOnly Employees As New Dictionary(Of Integer, EmployeeDetail)()
    Public ReadOnly Ciphers As New Dictionary(Of Integer, EncryptedIdentifiers)()
    Public ReadOnly Last4s As New Dictionary(Of Integer, IdentifierLast4)()
    Public ReadOnly Hashes As New Dictionary(Of String, Integer)()
    Public ReadOnly RevealLog As New List(Of String)()
    Public ReadOnly BatchLogs As New List(Of String)()
    Public FailBatchLog As Boolean
    Public ReadOnly Writes As New List(Of IdentifierWrite)()
    Private _nextId As Integer = 100

    Public Function Create(draft As EmployeeDraft, placement As EmployeePlacement, userId As Integer?) As NewEmployee Implements IEmployeeRepository.Create
        _nextId += 1
        Employees(_nextId) = New EmployeeDetail With {
            .EmployeeId = _nextId, .EmpCode = $"EMP-{_nextId:D5}", .FullName = draft.FullName, .JoinDate = draft.JoinDate,
            .FactoryId = placement.FactoryId, .IsActive = True, .Status = EmployeeStatus.Active, .RowVersion = New Byte() {1}}
        Return New NewEmployee(_nextId, Employees(_nextId).EmpCode)
    End Function

    Public Sub Update(employeeId As Integer, rowVersion As Byte(), draft As EmployeeDraft, placement As EmployeePlacement, userId As Integer?) Implements IEmployeeRepository.Update
        If Not Employees.ContainsKey(employeeId) Then Throw New EmployeeRuleException("Employee not found.")
        If rowVersion IsNot Nothing AndAlso rowVersion(0) <> Employees(employeeId).RowVersion(0) Then
            Throw New EmployeeRuleException("The employee was changed by someone else. Reload and try again.")
        End If
        Employees(employeeId).FullName = draft.FullName
        Employees(employeeId).RowVersion = New Byte() {CByte(Employees(employeeId).RowVersion(0) + 1)}
    End Sub

    Public Function GetById(employeeId As Integer) As EmployeeDetail Implements IEmployeeRepository.GetById
        Dim found As EmployeeDetail = Nothing
        Employees.TryGetValue(employeeId, found)
        Return found
    End Function

    Public Function List(search As String, factoryId As Integer?, activeOnly As Boolean) As IReadOnlyList(Of EmployeeSummary) Implements IEmployeeRepository.List
        Return New List(Of EmployeeSummary)()
    End Function

    Public Sub RecordExit(employeeId As Integer, exitDate As Date, status As EmployeeStatus, userId As Integer?) Implements IEmployeeRepository.RecordExit
        Employees(employeeId).IsActive = False
        Employees(employeeId).Status = status
        Employees(employeeId).ExitDate = exitDate
    End Sub

    Public Sub Reactivate(employeeId As Integer, userId As Integer?) Implements IEmployeeRepository.Reactivate
        Employees(employeeId).IsActive = True
        Employees(employeeId).Status = EmployeeStatus.Active
        Employees(employeeId).ExitDate = Nothing
    End Sub

    Public Function GetIdentifierLast4(employeeId As Integer) As IdentifierLast4 Implements IEmployeeRepository.GetIdentifierLast4
        Dim found As IdentifierLast4 = Nothing
        Last4s.TryGetValue(employeeId, found)
        Return found
    End Function

    Public Function GetEncryptedIdentifiers(employeeId As Integer) As EncryptedIdentifiers Implements IEmployeeRepository.GetEncryptedIdentifiers
        Dim found As EncryptedIdentifiers = Nothing
        Ciphers.TryGetValue(employeeId, found)
        Return found
    End Function

    Public Sub SetIdentifiers(employeeId As Integer, write As IdentifierWrite, userId As Integer?) Implements IEmployeeRepository.SetIdentifiers
        For Each change In New IdChange() {write.Uan, write.EsiIp, write.Pan, write.Aadhaar}
            If change.Kind = IdChangeKind.Replaced Then
                Dim key = Convert.ToHexString(change.Value.Hash)
                If Hashes.ContainsKey(key) AndAlso Hashes(key) <> employeeId Then
                    Throw New EmployeeRuleException("This PAN belongs to another employee.")
                End If
            End If
        Next
        Writes.Add(write)
        Dim old = GetEncryptedIdentifiers(employeeId)
        Ciphers(employeeId) = New EncryptedIdentifiers(write.KeyVersion, Apply(write.Uan, If(old Is Nothing, Nothing, old.Uan)),
                                                       Apply(write.EsiIp, If(old Is Nothing, Nothing, old.EsiIp)),
                                                       Apply(write.Pan, If(old Is Nothing, Nothing, old.Pan)),
                                                       Apply(write.Aadhaar, If(old Is Nothing, Nothing, old.Aadhaar)))
        For Each change In New IdChange() {write.Uan, write.EsiIp, write.Pan, write.Aadhaar}
            If change.Kind = IdChangeKind.Replaced Then Hashes(Convert.ToHexString(change.Value.Hash)) = employeeId
        Next
    End Sub

    Public Sub LogReveal(userId As Integer, employeeId As Integer, fields As String) Implements IEmployeeRepository.LogReveal
        RevealLog.Add($"{userId}:{employeeId}:{fields}")
    End Sub

    Public Sub LogRevealBatch(userId As Integer, reason As String, fields As String, employeeIds As IReadOnlyCollection(Of Integer)) Implements IEmployeeRepository.LogRevealBatch
        If FailBatchLog Then Throw New EmployeeRuleException("log down")
        BatchLogs.Add($"{userId}:{reason}:{fields}:{String.Join(",", employeeIds)}")
    End Sub

    Private Shared Function Apply(change As IdChange, current As Byte()) As Byte()
        Select Case change.Kind
            Case IdChangeKind.Replaced
                Return change.Value.Cipher
            Case IdChangeKind.Cleared
                Return Nothing
            Case Else
                Return current
        End Select
    End Function
End Class

Friend NotInheritable Class FakePermissions
    Implements IPermissionService

    Public ReadOnly Allowed As New HashSet(Of PermissionAction)()

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Return moduleKey = ModuleKey.Employees AndAlso Allowed.Contains(action)
    End Function
End Class

Friend NotInheritable Class FakeAudit
    Implements IAuditService

    Public ReadOnly Entries As New List(Of AuditEntry)()
    Public FailWith As String

    Public Function Record(entry As AuditEntry) As OperationResult Implements IAuditService.Record
        If FailWith IsNot Nothing Then Return OperationResult.Fail(FailWith)
        Entries.Add(entry)
        Return OperationResult.Ok()
    End Function
End Class

Friend NotInheritable Class FakeSession
    Implements ISessionContext

    Public ReadOnly Property Current As UserSession Implements ISessionContext.Current
        Get
            Return New UserSession(7, "owner1", UserRole.Owner, 3, PlanTier.Gold)
        End Get
    End Property

    Public ReadOnly Property IsAuthenticated As Boolean Implements ISessionContext.IsAuthenticated
        Get
            Return True
        End Get
    End Property

    Public Sub SignIn(session As UserSession) Implements ISessionContext.SignIn
    End Sub

    Public Sub SignOut() Implements ISessionContext.SignOut
    End Sub
End Class
