Imports System.Globalization
Imports Revolution.Core
Imports Revolution.Modules.Payroll

''' <summary>
''' Works out an arrears claim for an employee over wage months, to look at before it is saved. For each month: the approved or locked regular run that paid it,
''' the employee's stored line in it, and the same month worked again with the salary in force now. Needs the BasicPayroll plan feature and Payroll Create.
''' Rates are those of each wage month (Owner, 2026-11-01). Nothing is stored here: save with ArrearsService.Create.
''' </summary>
Public NotInheritable Class ArrearsPrepareService
    Private Const MonthFormat As String = "MMM yyyy"

    Private ReadOnly _source As IArrearsSource
    Private ReadOnly _runs As IPayrollRunRepository
    Private ReadOnly _salary As ISalaryRepository
    Private ReadOnly _rates As IArrearsRates
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService

    Public Sub New(source As IArrearsSource, runs As IPayrollRunRepository, salary As ISalaryRepository, rates As IArrearsRates,
                   permissions As IPermissionService, entitlements As IEntitlementService)
        If source Is Nothing Then Throw New ArgumentNullException(NameOf(source))
        If runs Is Nothing Then Throw New ArgumentNullException(NameOf(runs))
        If salary Is Nothing Then Throw New ArgumentNullException(NameOf(salary))
        If rates Is Nothing Then Throw New ArgumentNullException(NameOf(rates))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        _source = source
        _runs = runs
        _salary = salary
        _rates = rates
        _permissions = permissions
        _entitlements = entitlements
    End Sub

    ''' <param name="firstMonth">Any date in the first wage month.</param>
    ''' <param name="lastMonth">Any date in the last wage month.</param>
    Public Function Prepare(employeeId As Integer, firstMonth As Date, lastMonth As Date) As OperationResult(Of ArrearsPreparation)
        If Not _entitlements.IsEnabled(PlanFeature.BasicPayroll) Then Return OperationResult(Of ArrearsPreparation).Fail("Payroll is not part of your plan.")
        If Not _permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.Create) Then Return OperationResult(Of ArrearsPreparation).Fail("You are not allowed to do this.")
        If employeeId <= 0 Then Return OperationResult(Of ArrearsPreparation).Fail("Pick an employee.")
        Dim first As New Date(firstMonth.Year, firstMonth.Month, 1)
        Dim last As New Date(lastMonth.Year, lastMonth.Month, 1)
        If last < first Then Return OperationResult(Of ArrearsPreparation).Fail("The last month cannot be before the first month.")
        Dim count As Integer = (last.Year - first.Year) * 12 + last.Month - first.Month + 1
        If count > ArrearsService.MaxMonths Then Return OperationResult(Of ArrearsPreparation).Fail($"A claim covers at most {ArrearsService.MaxMonths.ToString(CultureInfo.InvariantCulture)} months.")
        Dim policy As PolicySettings = _salary.GetPolicy()
        Dim inputs As New List(Of ArrearsMonthInput)()
        For i As Integer = 0 To count - 1
            Dim month As Date = first.AddMonths(i)
            Dim one As OperationResult(Of ArrearsMonthInput) = PrepareMonth(employeeId, month, policy)
            If Not one.Succeeded Then Return OperationResult(Of ArrearsPreparation).Fail(one.Message)
            inputs.Add(one.Value)
        Next
        Return OperationResult(Of ArrearsPreparation).Ok(New ArrearsPreparation(inputs, ArrearsCalculator.Calculate(inputs)))
    End Function

    Private Function PrepareMonth(employeeId As Integer, month As Date, policy As PolicySettings) As OperationResult(Of ArrearsMonthInput)
        Dim label As String = month.ToString(MonthFormat, CultureInfo.InvariantCulture)
        Dim period As RunPeriod = PayCycle.PeriodFor(month.Year, month.Month, policy.PayCycleStartDay)
        Dim header As PayrollRunHeader = _source.FindRegularRun(period)
        If header Is Nothing Then Return OperationResult(Of ArrearsMonthInput).Fail($"No payroll run was made for {label}.")
        If header.Status < PayrollRunStatus.Approved Then
            Return OperationResult(Of ArrearsMonthInput).Fail($"The run for {label} is not approved yet. Arrears are claimed against approved or locked runs.")
        End If
        Dim paid As RunLineResult = _runs.ListLines(header.RunId).FirstOrDefault(Function(l As RunLineResult) l.EmployeeId = employeeId)
        If paid Is Nothing Then Return OperationResult(Of ArrearsMonthInput).Fail($"The employee was not paid in the run for {label}.")
        Dim facts As RunEmployeeFacts = _runs.LoadInputs(period).FirstOrDefault(Function(f As RunEmployeeFacts) f.EmployeeId = employeeId)
        If facts Is Nothing Then Return OperationResult(Of ArrearsMonthInput).Fail($"The employee has no pay facts for {label}.")
        Dim rates As OperationResult(Of RunRates) = LoadRates(period, facts)
        If Not rates.Succeeded Then Return OperationResult(Of ArrearsMonthInput).Fail($"{label}: {rates.Message}")
        Dim input As OperationResult(Of ArrearsMonthInput) = ArrearsMonthPreparer.Prepare(paid, facts, period, policy, rates.Value)
        If Not input.Succeeded Then Return OperationResult(Of ArrearsMonthInput).Fail($"{label}: {input.Message}")
        Return input
    End Function

    Private Function LoadRates(period As RunPeriod, facts As RunEmployeeFacts) As OperationResult(Of RunRates)
        Dim epf As OperationResult(Of EpfRate) = _rates.GetEpfRate(period.RateDate)
        If Not epf.Succeeded Then Return OperationResult(Of RunRates).Fail(epf.Message)
        Dim esi As OperationResult(Of EsiRate) = _rates.GetEsiRate(period.RateDate)
        If Not esi.Succeeded Then Return OperationResult(Of RunRates).Fail(esi.Message)
        Dim wageRule As OperationResult(Of WageRule) = _rates.GetWageRule(period.RateDate)
        If Not wageRule.Succeeded Then Return OperationResult(Of RunRates).Fail(wageRule.Message)
        Dim pt As New Dictionary(Of Integer, PtRule)()
        If facts.PtStateId.HasValue Then
            Dim rule As OperationResult(Of PtRule) = _rates.GetPtRule(facts.PtStateId.Value, period.RateDate)
            If Not rule.Succeeded Then Return OperationResult(Of RunRates).Fail(rule.Message)
            pt(facts.PtStateId.Value) = rule.Value
        End If
        Return OperationResult(Of RunRates).Ok(New RunRates(epf.Value, esi.Value, wageRule.Value, pt))
    End Function
End Class
