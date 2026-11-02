Imports Revolution.Core
Imports Revolution.Modules.Arrears
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class ArrearsEcrServiceTests
    Private Const Uan1 As String = "100000000001"
    Private Const Uan2 As String = "100000000002"

    Private ReadOnly _repo As New FakeArrearsRepository()
    Private ReadOnly _releaser As New FakeArrearsReleaser()
    Private ReadOnly _perms As New FakeArrearsPermissions()
    Private ReadOnly _plan As New FakeArrearsEntitlements()
    Private ReadOnly _audit As New FakeArrearsAudit()
    Private ReadOnly _service As ArrearsEcrService

    Public Sub New()
        _service = New ArrearsEcrService(_repo, _releaser, _perms, _plan, _audit)
        _releaser.Uans(1) = Uan1
        _releaser.Uans(2) = Uan2
        _repo.EcrRows.Add(Line(1, "E001", "Ram Kumar", 2000D))
        _repo.EcrRows.Add(Line(2, "E002", "Sita Devi", 1000D))
    End Sub

    Private Shared Function Line(id As Integer, code As String, name As String, wages As Decimal) As ArrearsEcrLine
        Return New ArrearsEcrLine(id, code, name, wages, wages, wages, wages * 0.12D, wages * 0.0833D, wages * 0.0367D)
    End Function

    Private Sub Allow()
        _perms.Allowed.Add(PermissionAction.Export)
    End Sub

    <Fact>
    Public Sub NeedsExport_OnStatutory()
        Assert.False(_service.Build(New Date(2026, 10, 1)).Succeeded)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub PlanWithoutPayroll_IsRefused()
        Allow()
        _plan.PayrollEnabled = False
        Assert.False(_service.Build(New Date(2026, 10, 1)).Succeeded)
    End Sub

    <Fact>
    Public Sub BuildsOneLinePerEmployee_NamedByMonth()
        Allow()
        Dim r As OperationResult(Of FileOutput(Of String)) = _service.Build(New Date(2026, 10, 17))
        Assert.True(r.Succeeded)
        Assert.Equal("ECR_ARREAR_202610.txt", r.Value.FileName)
        Dim lines As String() = r.Value.Content.Split({vbCrLf}, StringSplitOptions.RemoveEmptyEntries)
        Assert.Equal(2, lines.Length)
        Assert.Equal("100000000001#~#RAM KUMAR#~#2000#~#2000#~#2000#~#240#~#166#~#73", lines(0))
        Assert.Empty(r.Value.Problems)
    End Sub

    <Fact>
    Public Sub AuditHoldsNoIdOrAmount()
        Allow()
        _service.Build(New Date(2026, 10, 1))
        Dim entry As AuditEntry = _audit.Entries.Single()
        Assert.Equal("BuildArrearEcr", entry.Action)
        Assert.DoesNotContain(Uan1, entry.Details, StringComparison.Ordinal)
        Assert.DoesNotContain("2000", entry.Details, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub OnlyEmployeesInTheFile_AreReleased()
        Allow()
        _repo.EcrRows.Add(New ArrearsEcrLine(3, "E003", "No Change", 0D, 0D, 0D, 0D, 0D, 0D))
        _service.Build(New Date(2026, 10, 1))
        Assert.Equal(2, _releaser.LastIds.Count)
        Assert.DoesNotContain(3, _releaser.LastIds)
    End Sub

    <Fact>
    Public Sub NoValidUan_LeavesTheEmployeeOutAndNamesTheCode()
        Allow()
        _releaser.Uans(2) = "123"
        Dim r As OperationResult(Of FileOutput(Of String)) = _service.Build(New Date(2026, 10, 1))
        Assert.True(r.Succeeded)
        Assert.Equal(1, r.Value.Content.Split({vbCrLf}, StringSplitOptions.RemoveEmptyEntries).Length)
        Assert.Contains("E002", r.Value.Problems.Single(), StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub NobodyHasAUan_IsRefused()
        Allow()
        _releaser.Uans.Clear()
        Assert.False(_service.Build(New Date(2026, 10, 1)).Succeeded)
    End Sub

    <Fact>
    Public Sub Overpayment_RefusesTheFile_NamingTheCode()
        Allow()
        _repo.EcrRows.Add(Line(3, "E003", "Over Paid", -500D))
        Dim r As OperationResult(Of FileOutput(Of String)) = _service.Build(New Date(2026, 10, 1))
        Assert.False(r.Succeeded)
        Assert.Contains("E003", r.Message, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub NoClaims_IsRefused()
        Allow()
        _repo.EcrRows.Clear()
        Dim r As OperationResult(Of FileOutput(Of String)) = _service.Build(New Date(2026, 10, 1))
        Assert.False(r.Succeeded)
        Assert.Contains("Oct 2026", r.Message, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub ReleaseFailure_IsPassedOn()
        Allow()
        _releaser.Fail = "You are not allowed to export."
        Assert.False(_service.Build(New Date(2026, 10, 1)).Succeeded)
        Assert.Empty(_audit.Entries)
    End Sub
End Class
