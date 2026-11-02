Imports Revolution.Core
Imports Revolution.Modules.Arrears
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class ArrearsPrepareServiceTests
    Private ReadOnly _source As New FakeArrearsSource()
    Private ReadOnly _runs As New FakeArrearsRunRepository()
    Private ReadOnly _perms As New FakeArrearsPermissions()
    Private ReadOnly _plan As New FakeArrearsEntitlements()
    Private ReadOnly _service As ArrearsPrepareService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.Create)
        _service = New ArrearsPrepareService(_source, _runs, New FakeArrearsSalary(), New FakeArrearsRates(), _perms, _plan)
        ' October 2026: paid on basic 10,000, now due on basic 12,000.
        _source.Runs(New Date(2026, 10, 1)) = ArrearsTestData.Header(55, PayrollRunStatus.Locked)
        _runs.Lines(55) = New List(Of RunLineResult) From {ArrearsTestData.Paid(ArrearsTestData.Facts(10000D))}
        _runs.Facts(New Date(2026, 10, 1)) = New List(Of RunEmployeeFacts) From {ArrearsTestData.Facts(12000D)}
    End Sub

    <Fact>
    Public Sub OneMonth_PaidAgainstDue()
        Dim r As OperationResult(Of ArrearsPreparation) = _service.Prepare(101, New Date(2026, 10, 14), New Date(2026, 10, 20))
        Assert.True(r.Succeeded)
        Assert.Equal(1, r.Value.Inputs.Count)
        Assert.True(r.Value.HasDifference)
        Assert.Equal(2000D, r.Value.Result.GrossTotal)
        Assert.Equal(240D, r.Value.Result.EmployeeDeductionTotal)
        Assert.Equal(1760D, r.Value.Result.NetBeforeTax)
    End Sub

    <Fact>
    Public Sub NoChange_HasNoDifference()
        _runs.Facts(New Date(2026, 10, 1)) = New List(Of RunEmployeeFacts) From {ArrearsTestData.Facts(10000D)}
        Dim r As OperationResult(Of ArrearsPreparation) = _service.Prepare(101, New Date(2026, 10, 1), New Date(2026, 10, 1))
        Assert.True(r.Succeeded)
        Assert.False(r.Value.HasDifference)
    End Sub

    <Fact>
    Public Sub MonthWithoutARun_NamesTheMonth()
        Dim r As OperationResult(Of ArrearsPreparation) = _service.Prepare(101, New Date(2026, 9, 1), New Date(2026, 10, 1))
        Assert.False(r.Succeeded)
        Assert.Contains("Sep 2026", r.Message, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub RunNotApproved_IsRefused()
        _source.Runs(New Date(2026, 10, 1)) = ArrearsTestData.Header(55, PayrollRunStatus.Processed)
        Dim r As OperationResult(Of ArrearsPreparation) = _service.Prepare(101, New Date(2026, 10, 1), New Date(2026, 10, 1))
        Assert.False(r.Succeeded)
        Assert.Contains("not approved", r.Message, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub EmployeeNotInTheRun_IsRefused()
        Dim r As OperationResult(Of ArrearsPreparation) = _service.Prepare(999, New Date(2026, 10, 1), New Date(2026, 10, 1))
        Assert.False(r.Succeeded)
        Assert.Contains("not paid", r.Message, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub NeedsCreate_AndThePlan()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.Prepare(101, New Date(2026, 10, 1), New Date(2026, 10, 1)).Succeeded)
        _perms.Allowed.Add(PermissionAction.Create)
        _plan.PayrollEnabled = False
        Assert.False(_service.Prepare(101, New Date(2026, 10, 1), New Date(2026, 10, 1)).Succeeded)
    End Sub

    <Fact>
    Public Sub BadRanges_AreRefused()
        Assert.False(_service.Prepare(0, New Date(2026, 10, 1), New Date(2026, 10, 1)).Succeeded)
        Assert.False(_service.Prepare(101, New Date(2026, 10, 1), New Date(2026, 9, 1)).Succeeded)
        Assert.False(_service.Prepare(101, New Date(2020, 1, 1), New Date(2026, 10, 1)).Succeeded)
    End Sub
End Class
