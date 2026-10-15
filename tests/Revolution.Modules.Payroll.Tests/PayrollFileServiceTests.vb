Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Friend NotInheritable Class FakeFileSource
    Implements IPayrollFileSource

    Public Snapshot As PayrollFileSnapshot
    Public Calls As Integer

    Public Function Load(runId As Integer) As PayrollFileSnapshot Implements IPayrollFileSource.Load
        Calls += 1
        Return Snapshot
    End Function
End Class

Friend NotInheritable Class FakeFilePermissions
    Implements IPermissionService

    Public ReadOnly Allowed As New HashSet(Of String)()

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Return Allowed.Contains(moduleKey.ToString() & ":" & action.ToString())
    End Function
End Class

Friend NotInheritable Class FakeFileAudit
    Implements IAuditService

    Public ReadOnly Entries As New List(Of AuditEntry)()
    Public Fail As Boolean

    Public Function Record(entry As AuditEntry) As OperationResult Implements IAuditService.Record
        If Fail Then
            Return OperationResult.Fail("Audit failed.")
        End If
        Entries.Add(entry)
        Return OperationResult.Ok()
    End Function
End Class

Public Class PayrollFileServiceTests
    Private ReadOnly _source As New FakeFileSource()
    Private ReadOnly _permissions As New FakeFilePermissions()
    Private ReadOnly _audit As New FakeFileAudit()

    Private Function Service() As PayrollFileService
        Return New PayrollFileService(_source, _permissions, _audit)
    End Function

    Private Shared Function Worker(id As Integer, uan As String, ip As String) As PayrollFileEmployee
        Return New PayrollFileEmployee With {
            .EmployeeID = id, .Name = "Ravi Kumar", .PaidDays = 26D, .GrossEarnings = 18000D,
            .EpfWage = 15000D, .EsiWage = 18000D, .EpfEmployee = 1800D, .EpfEmployerEpf = 550D,
            .EpfEmployerEps = 1250D, .Edli = 75D, .NetPay = 15000D,
            .IsEpfMember = True, .Uan = uan, .IsEsiMember = True, .EsiIpNumber = ip,
            .PaysByBank = True, .Ifsc = "SBIN0008586", .AccountNumber = "01004567891"}
    End Function

    Private Sub Arrange(status As PayrollRunStatus, ParamArray staff As PayrollFileEmployee())
        _source.Snapshot = New PayrollFileSnapshot(7, New Date(2026, 9, 26), New Date(2026, 10, 25), status, staff)
        _permissions.Allowed.Add("Statutory:Export")
        _permissions.Allowed.Add("Payroll:Export")
    End Sub

    <Fact>
    Public Sub Ecr_NoPermission_Fails_AndNeverLoadsRun()
        _source.Snapshot = New PayrollFileSnapshot(7, Date.Today, Date.Today, PayrollRunStatus.Approved, New List(Of PayrollFileEmployee)())
        Dim r = Service().BuildEcr(7, 15000D)
        Assert.False(r.Succeeded)
        Assert.Equal(0, _source.Calls)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Ecr_DraftRun_Fails()
        Arrange(PayrollRunStatus.Processed, Worker(1, "100123456789", "2018517470"))
        Assert.False(Service().BuildEcr(7, 15000D).Succeeded)
    End Sub

    <Fact>
    Public Sub Ecr_ApprovedRun_BuildsFile_AndAudits()
        Arrange(PayrollRunStatus.Approved, Worker(1, "100123456789", "2018517470"))
        Dim r = Service().BuildEcr(7, 15000D)
        Assert.True(r.Succeeded)
        Assert.Equal("ECR_202610_7.txt", r.Value.FileName)
        Assert.Contains("#~#", r.Value.Content, StringComparison.Ordinal)
        Assert.Single(_audit.Entries)
        Assert.Equal("BuildEcr", _audit.Entries(0).Action)
        Assert.DoesNotContain("100123456789", _audit.Entries(0).Details, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub Ecr_MissingUan_ListedAndOthersBuilt()
        Arrange(PayrollRunStatus.Locked, Worker(1, "100123456789", "2018517470"), Worker(2, "", "2018517471"))
        Dim r = Service().BuildEcr(7, 15000D)
        Assert.True(r.Succeeded)
        Assert.Single(r.Value.Problems)
    End Sub

    <Fact>
    Public Sub Ecr_AuditFails_NoFileReleased()
        Arrange(PayrollRunStatus.Approved, Worker(1, "100123456789", "2018517470"))
        _audit.Fail = True
        Assert.False(Service().BuildEcr(7, 15000D).Succeeded)
    End Sub

    <Fact>
    Public Sub Ecr_RunNotFound_Fails()
        _permissions.Allowed.Add("Statutory:Export")
        Assert.False(Service().BuildEcr(7, 15000D).Succeeded)
    End Sub

    <Fact>
    Public Sub Esic_ApprovedRun_TwoSheets()
        Arrange(PayrollRunStatus.Paid, Worker(1, "100123456789", "2018517470"))
        Dim r = Service().BuildEsic(7)
        Assert.True(r.Succeeded)
        Assert.Equal("ESIC_202610_7.xls", r.Value.FileName)
        Assert.Equal(2, r.Value.Content.Count)
    End Sub

    <Fact>
    Public Sub Bank_UsesPayrollExportPermission()
        Arrange(PayrollRunStatus.Approved, Worker(1, "100123456789", "2018517470"))
        _permissions.Allowed.Remove("Payroll:Export")
        Dim r = Service().BuildBank(7, New SbiFileWriter(), "01600123456", "08586", New Date(2026, 10, 31), "SAL1026", "SALARY OCT 2026")
        Assert.False(r.Succeeded)
    End Sub

    <Fact>
    Public Sub Bank_ApprovedRun_BuildsBothFiles()
        Arrange(PayrollRunStatus.Approved, Worker(1, "100123456789", "2018517470"))
        Dim r = Service().BuildBank(7, New SbiFileWriter(), "01600123456", "SBIN0008586", New Date(2026, 10, 31), "SAL1026", "SALARY OCT 2026")
        Assert.True(r.Succeeded)
        Assert.Equal("BANK_202610_7", r.Value.FileName)
        Assert.StartsWith("S|A|RAVI KUMAR|", r.Value.Content.BeneficiaryText, StringComparison.Ordinal)
        Assert.StartsWith("01600123456|08586|31/10/2026|15000.00|", r.Value.Content.TransactionText, StringComparison.Ordinal)
        Assert.Equal("BuildBankFile", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub Bank_InvalidBatch_ReturnsMessage()
        Arrange(PayrollRunStatus.Approved, Worker(1, "100123456789", "2018517470"))
        Dim r = Service().BuildBank(7, New SbiFileWriter(), "ABC", "08586", New Date(2026, 10, 31), "SAL1026", "SALARY OCT 2026")
        Assert.False(r.Succeeded)
        Assert.Empty(_audit.Entries)
    End Sub
End Class
