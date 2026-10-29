Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Friend NotInheritable Class FakeRunTaxSource
    Implements IRunTaxSource

    Public Calls As Integer
    Public LastRunId As Integer
    Public LastFinancialYear As Integer
    Public LastEmployees As IReadOnlyList(Of Integer)
    Public SkipEmployee As Integer

    Public Function Load(runId As Integer, financialYear As Integer, employeeIds As IReadOnlyList(Of Integer)) As IReadOnlyDictionary(Of Integer, EmployeeTaxFacts) Implements IRunTaxSource.Load
        Calls += 1
        LastRunId = runId
        LastFinancialYear = financialYear
        LastEmployees = employeeIds
        Dim d As New Dictionary(Of Integer, EmployeeTaxFacts)()
        For Each id As Integer In employeeIds
            If id <> SkipEmployee Then d(id) = New EmployeeTaxFacts(id, New Date(1990, 5, 5), New List(Of TdsMonthFacts)(), 0D, Nothing)
        Next
        Return d
    End Function
End Class

Public Class RunTdsServiceTests
    Private ReadOnly _runs As New FakeRunRepository()
    Private ReadOnly _tax As New FakeRunTaxSource()
    Private ReadOnly _taxRates As New FakeTdsRates()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _service As PayrollRunService

    Public Sub New()
        Dim perms As New FakePayPermissions()
        For Each action As PermissionAction In [Enum].GetValues(GetType(PermissionAction))
            perms.Allowed.Add(action)
        Next
        _service = New PayrollRunService(_runs, New FakeSalaryRepository(), New FakeRates(), New FakeApprovals(), perms, _plan, New FakePayAudit(), New FakePaySession(),
                                         _tax, New TdsProjectionService(_taxRates))
        _runs.Inputs.Add(RunTestData.Facts(101, "E101", paidDays:=30D))
        _runs.Inputs.Add(RunTestData.Facts(102, "E102", paidDays:=30D))
    End Sub

    <Fact>
    Public Sub PlanWithoutTds_TaxSourceIsNotUsed()
        _plan.Disabled.Add(PlanFeature.Tds)
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Draft)
        Assert.True(_service.Process(id).Succeeded)
        Assert.Equal(0, _tax.Calls)
        Assert.Equal(0D, _runs.Lines(id)(0).TdsAmount)
    End Sub

    <Fact>
    Public Sub PlanWithTds_LoadsTaxFactsOnceForTheWholeRun()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Draft)
        Assert.True(_service.Process(id).Succeeded)
        Assert.Equal(1, _tax.Calls)
        Assert.Equal(id, _tax.LastRunId)
        Assert.Equal(2026, _tax.LastFinancialYear)
        Assert.Equal(2, _tax.LastEmployees.Count)
        Assert.Equal(0, _service.TdsWarnings.Count)
    End Sub

    <Fact>
    Public Sub RuleFailure_StopsTheRunWithTheEmployeeCode()
        _taxRates.RuleFailure = "No tax slab row is in force."
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Draft)
        Dim r = _service.Process(id)
        Assert.False(r.Succeeded)
        Assert.Contains("E101", r.Message)
        Assert.Contains("No tax slab row is in force.", r.Message)
        Assert.False(_runs.Lines.ContainsKey(id))
    End Sub

    <Fact>
    Public Sub MissingTaxFacts_StopsTheRunForThatEmployee()
        _tax.SkipEmployee = 102
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Draft)
        Dim r = _service.Process(id)
        Assert.False(r.Succeeded)
        Assert.Contains("E102", r.Message)
    End Sub
End Class
