Imports System.Security.Cryptography
Imports Revolution.Core
Imports Revolution.Modules.Employees
Imports Xunit

Public Class EmployeeExportServiceTests
    Private Const CompanyId As Integer = 3
    Private Const UserId As Integer = 7

    Private NotInheritable Class ExportPermissions
        Implements IPermissionService

        Public ReadOnly Allowed As New HashSet(Of ModuleKey)()

        Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
            Return action = PermissionAction.Export AndAlso Allowed.Contains(moduleKey)
        End Function
    End Class

    Private ReadOnly _repo As New FakeEmployeeRepository()
    Private ReadOnly _bankRepo As New FakeBankRepository()
    Private ReadOnly _crypto As New FakeFieldCrypto()
    Private ReadOnly _perms As New ExportPermissions()
    Private ReadOnly _service As EmployeeExportService

    Public Sub New()
        _perms.Allowed.Add(ModuleKey.Statutory)
        _perms.Allowed.Add(ModuleKey.Payroll)
        Dim none As New FakePermissions()
        Dim session As New FakeSession()
        Dim identifiers As New EmployeeIdentifierService(_repo, _crypto, none, New FakeAudit(), session)
        Dim bank As New EmployeeBankService(_bankRepo, _crypto, none, New FakeAudit(), session)
        _service = New EmployeeExportService(_repo, identifiers, bank, _perms, session)
        Seed(101, "100123456789", "1234567890", "111122223333")
        Seed(102, "100987654321", "0987654321", "444455556666")
    End Sub

    Private Sub Seed(employeeId As Integer, uan As String, esiIp As String, account As String)
        _repo.Ciphers(employeeId) = New EncryptedIdentifiers(1,
            _crypto.Encrypt(uan, FieldContext.Build(CompanyId, employeeId, FieldContext.Uan)),
            _crypto.Encrypt(esiIp, FieldContext.Build(CompanyId, employeeId, FieldContext.EsiIp)), Nothing, Nothing)
        _bankRepo.Accounts(employeeId) = New EncryptedBankAccount(1, _crypto.Encrypt(account, FieldContext.Build(CompanyId, employeeId, FieldContext.BankAccount)))
    End Sub

    Private Shared Function Ids(ParamArray values As Integer()) As IReadOnlyCollection(Of Integer)
        Return values
    End Function

    <Fact>
    Public Sub Ecr_ReleasesUanOnly_AndLogsFirst()
        Dim r = _service.RevealForExport(ExportKind.Ecr, Ids(101, 102))
        Assert.True(r.Succeeded)
        Assert.Equal("100123456789", r.Value(101).Uan)
        Assert.Equal(String.Empty, r.Value(101).EsiIp)
        Assert.Equal(String.Empty, r.Value(101).AccountNumber)
        Assert.Equal($"{UserId}:ECR export:UAN:101,102", _repo.BatchLogs.Single())
    End Sub

    <Fact>
    Public Sub Esic_ReleasesUanAndEsiIp()
        Dim r = _service.RevealForExport(ExportKind.Esic, Ids(102))
        Assert.Equal("100987654321", r.Value(102).Uan)
        Assert.Equal("0987654321", r.Value(102).EsiIp)
        Assert.Equal(String.Empty, r.Value(102).AccountNumber)
        Assert.Contains("ESIC export:UAN,ESIIP", _repo.BatchLogs.Single())
    End Sub

    <Fact>
    Public Sub BankFile_ReleasesTheAccountOnly()
        Dim r = _service.RevealForExport(ExportKind.BankFile, Ids(101))
        Assert.Equal("111122223333", r.Value(101).AccountNumber)
        Assert.Equal(String.Empty, r.Value(101).Uan)
        Assert.Contains("Bank file:BANKACCOUNT", _repo.BatchLogs.Single())
    End Sub

    <Fact>
    Public Sub EmployeeWithoutAnId_GetsEmptyValues()
        Dim r = _service.RevealForExport(ExportKind.Ecr, Ids(555))
        Assert.True(r.Succeeded)
        Assert.Equal(String.Empty, r.Value(555).Uan)
    End Sub

    <Fact>
    Public Sub StatutoryExport_NeedsExportOnStatutory_BankNeedsExportOnPayroll()
        _perms.Allowed.Remove(ModuleKey.Statutory)
        Assert.False(_service.RevealForExport(ExportKind.Ecr, Ids(101)).Succeeded)
        Assert.False(_service.RevealForExport(ExportKind.Esic, Ids(101)).Succeeded)
        Assert.True(_service.RevealForExport(ExportKind.BankFile, Ids(101)).Succeeded)
        _perms.Allowed.Remove(ModuleKey.Payroll)
        Assert.False(_service.RevealForExport(ExportKind.BankFile, Ids(101)).Succeeded)
        Assert.Single(_repo.BatchLogs)
    End Sub

    <Fact>
    Public Sub NoRevealGrantIsNeeded()
        Assert.True(_service.RevealForExport(ExportKind.Ecr, Ids(101)).Succeeded)
    End Sub

    <Fact>
    Public Sub IfTheLogFails_NothingIsReleased()
        _repo.FailBatchLog = True
        Dim r = _service.RevealForExport(ExportKind.Ecr, Ids(101))
        Assert.False(r.Succeeded)
        Assert.DoesNotContain("100123456789", r.Message)
        Assert.Empty(_repo.BatchLogs)
    End Sub

    <Fact>
    Public Sub UnreadableId_FailsWithoutQuotingAnything()
        _crypto.FailDecrypt = True
        Dim r = _service.RevealForExport(ExportKind.Ecr, Ids(101))
        Assert.False(r.Succeeded)
        Assert.DoesNotContain("101", r.Message)
    End Sub

    <Theory>
    <InlineData(0)>
    <InlineData(-1)>
    Public Sub BadEmployeeId_IsRefused_AndNothingIsLogged(id As Integer)
        Assert.False(_service.RevealForExport(ExportKind.Ecr, Ids(101, id)).Succeeded)
        Assert.Empty(_repo.BatchLogs)
    End Sub

    <Fact>
    Public Sub EmptyDuplicateAndTooManyLists_AreRefused()
        Assert.False(_service.RevealForExport(ExportKind.Ecr, Ids()).Succeeded)
        Assert.False(_service.RevealForExport(ExportKind.Ecr, Ids(101, 101)).Succeeded)
        Dim many = Enumerable.Range(1, EmployeeExportService.MaxEmployees + 1).ToList()
        Assert.False(_service.RevealForExport(ExportKind.Ecr, many).Succeeded)
        Assert.Empty(_repo.BatchLogs)
    End Sub

    <Fact>
    Public Sub UnknownKind_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() _service.RevealForExport(CType(9, ExportKind), Ids(101)))
    End Sub
End Class
