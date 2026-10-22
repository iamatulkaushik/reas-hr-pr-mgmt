Imports System.Security.Cryptography
Imports Microsoft.Data.SqlClient
Imports Revolution.Core

''' <summary>
''' Bulk id release for statutory and bank file exports (ADR 0019). Export permission on Statutory (ECR, ESIC) or on Payroll
''' (bank file) is enough; no Reveal grant is needed. The batch log is written first, one row per employee. If it fails,
''' nothing is decrypted. Only the ids the file needs are released. Plain values are not logged, cached or written anywhere.
''' The caller (the Payroll file service) checks that the run is approved or locked and writes the one audit entry per export.
''' </summary>
Public NotInheritable Class EmployeeExportService
    Public Const MaxEmployees As Integer = 5000

    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const NotLogged As String = "The export could not be logged, so no ids were released. Try again or contact support."
    Private Const Unreadable As String = "An id could not be read. Check that the key file is the right one."

    Private ReadOnly _repository As IEmployeeRepository
    Private ReadOnly _identifiers As EmployeeIdentifierService
    Private ReadOnly _bank As EmployeeBankService
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _session As ISessionContext

    Public Sub New(repository As IEmployeeRepository, identifiers As EmployeeIdentifierService, bank As EmployeeBankService,
                   permissions As IPermissionService, session As ISessionContext)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If identifiers Is Nothing Then Throw New ArgumentNullException(NameOf(identifiers))
        If bank Is Nothing Then Throw New ArgumentNullException(NameOf(bank))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        _repository = repository
        _identifiers = identifiers
        _bank = bank
        _permissions = permissions
        _session = session
    End Sub

    ''' <summary>Releases the plain ids for the employees of one export. Returns one entry per employee id given.</summary>
    Public Function RevealForExport(kind As ExportKind, employeeIds As IReadOnlyCollection(Of Integer)) As OperationResult(Of IReadOnlyDictionary(Of Integer, ExportIds))
        If Not [Enum].IsDefined(GetType(ExportKind), kind) Then Throw New ArgumentOutOfRangeException(NameOf(kind))
        If Not _permissions.IsAllowed(If(kind = ExportKind.BankFile, ModuleKey.Payroll, ModuleKey.Statutory), PermissionAction.Export) Then
            Return OperationResult(Of IReadOnlyDictionary(Of Integer, ExportIds)).Fail(NotAllowed)
        End If
        Dim problem As String = CheckIds(employeeIds)
        If problem.Length > 0 Then Return OperationResult(Of IReadOnlyDictionary(Of Integer, ExportIds)).Fail(problem)
        Dim user = _session.Current
        If user Is Nothing Then Return OperationResult(Of IReadOnlyDictionary(Of Integer, ExportIds)).Fail("Sign in first.")

        Try
            _repository.LogRevealBatch(user.UserId, ReasonFor(kind), FieldsFor(kind), employeeIds)
        Catch ex As EmployeeRuleException
            Return OperationResult(Of IReadOnlyDictionary(Of Integer, ExportIds)).Fail(NotLogged)
        Catch ex As SqlException
            Return OperationResult(Of IReadOnlyDictionary(Of Integer, ExportIds)).Fail(NotLogged)
        End Try

        Try
            Return OperationResult(Of IReadOnlyDictionary(Of Integer, ExportIds)).Ok(Decrypt(kind, employeeIds, user.CompanyId))
        Catch ex As CryptographicException
            Return OperationResult(Of IReadOnlyDictionary(Of Integer, ExportIds)).Fail(Unreadable)
        End Try
    End Function

    Private Shared Function CheckIds(employeeIds As IReadOnlyCollection(Of Integer)) As String
        If employeeIds Is Nothing OrElse employeeIds.Count = 0 Then Return "There are no employees to export."
        If employeeIds.Count > MaxEmployees Then Return $"At most {MaxEmployees} employees can be exported at once."
        If employeeIds.Any(Function(id) id <= 0) OrElse employeeIds.Distinct().Count() <> employeeIds.Count Then
            Return "The employee list is not valid."
        End If
        Return String.Empty
    End Function

    Private Function Decrypt(kind As ExportKind, employeeIds As IReadOnlyCollection(Of Integer), companyId As Integer) As IReadOnlyDictionary(Of Integer, ExportIds)
        Dim result As New Dictionary(Of Integer, ExportIds)()
        For Each employeeId In employeeIds
            Dim ids As New ExportIds()
            Select Case kind
                Case ExportKind.Ecr
                    ids.Uan = _identifiers.DecryptForExport(employeeId, IdField.Uan, companyId).Uan
                Case ExportKind.Esic
                    Dim revealed = _identifiers.DecryptForExport(employeeId, IdField.Uan Or IdField.EsiIp, companyId)
                    ids.Uan = revealed.Uan
                    ids.EsiIp = revealed.EsiIp
                Case Else
                    ids.AccountNumber = _bank.DecryptAccountForExport(employeeId, companyId)
            End Select
            result(employeeId) = ids
        Next
        Return result
    End Function

    Private Shared Function ReasonFor(kind As ExportKind) As String
        Select Case kind
            Case ExportKind.Ecr
                Return "ECR export"
            Case ExportKind.Esic
                Return "ESIC export"
            Case Else
                Return "Bank file"
        End Select
    End Function

    Private Shared Function FieldsFor(kind As ExportKind) As String
        Select Case kind
            Case ExportKind.Ecr
                Return "UAN"
            Case ExportKind.Esic
                Return "UAN,ESIIP"
            Case Else
                Return "BANKACCOUNT"
        End Select
    End Function
End Class
