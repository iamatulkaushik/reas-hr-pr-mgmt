Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers by hand with the test fixtures: basic 10,000, DA 2,000, HRA 4,000, October (31 days). EPF and ESI on 12,000 wage, PT 200 on 16,000 gross. The CA signs the expected values.</summary>
Public Class PayrollRunCalculatorTests
    Private Shared Function Run(facts As RunEmployeeFacts) As OperationResult(Of RunLineResult)
        Return PayrollRunCalculator.Calculate(facts, RunTestData.October(), RunTestData.Policy(), RunTestData.Rates())
    End Function

    <Fact>
    Public Sub FullMonth_AllStatutory()
        Dim r As OperationResult(Of RunLineResult) = Run(RunTestData.Facts())
        Assert.True(r.Succeeded)
        Dim l As RunLineResult = r.Value
        Assert.Equal(16000D, l.GrossEarnings)
        Assert.Equal(12000D, l.EpfWage)
        Assert.Equal(1440D, l.EpfEmployee)
        Assert.Equal(440D, l.EpfEmployerEpf)
        Assert.Equal(1000D, l.EpfEmployerEps)
        Assert.Equal(60D, l.Edli)
        Assert.Equal(60D, l.EpfAdmin)
        Assert.Equal(90D, l.EsiEmployee)
        Assert.Equal(390D, l.EsiEmployer)
        Assert.Equal(200D, l.ProfessionalTax)
        Assert.Equal(1730D, l.TotalDeductions)
        Assert.Equal(14270D, l.NetPay)
        Assert.Equal(0D, l.CarryForwardOut)
    End Sub

    <Fact>
    Public Sub NetPay_EqualsGrossLessTotalDeductions()
        Dim l As RunLineResult = Run(RunTestData.Facts()).Value
        Assert.Equal(l.GrossEarnings - l.TotalDeductions, l.NetPay)
    End Sub

    <Fact>
    Public Sub Earnings_OnePerHead_WithComponentIds()
        Dim l As RunLineResult = Run(RunTestData.Facts()).Value
        Assert.Equal(3, l.Earnings.Count)
        Assert.Equal(1, l.Earnings(0).ComponentId)
        Assert.Equal(10000D, l.Earnings(0).Amount)
        Assert.Equal(3, l.Earnings(2).ComponentId)
        Assert.Equal(4000D, l.Earnings(2).Amount)
    End Sub

    <Fact>
    Public Sub NotEpfMember_NoEpf()
        Dim l As RunLineResult = Run(RunTestData.Facts(epfMember:=False)).Value
        Assert.Equal(0D, l.EpfEmployee)
        Assert.Equal(0D, l.EpfEmployerEpf)
        Assert.Equal(0D, l.Edli)
        Assert.Equal(290D, l.TotalDeductions)
    End Sub

    <Fact>
    Public Sub NotEsiMember_NoEsi()
        Dim l As RunLineResult = Run(RunTestData.Facts(esiMember:=False)).Value
        Assert.Equal(0D, l.EsiEmployee)
        Assert.Equal(0D, l.EsiEmployer)
        Assert.Equal(1640D, l.TotalDeductions)
    End Sub

    <Fact>
    Public Sub PtNotApplicable_NoPt()
        Dim l As RunLineResult = Run(RunTestData.Facts(ptStateId:=Nothing)).Value
        Assert.Equal(0D, l.ProfessionalTax)
        Assert.Equal(1530D, l.TotalDeductions)
    End Sub

    <Fact>
    Public Sub HalfMonth_ProRata()
        Dim l As RunLineResult = Run(RunTestData.Facts(paidDays:=15.5D)).Value
        Assert.Equal(8000D, l.GrossEarnings)
        Assert.Equal(6000D, l.EpfWage)
    End Sub

    <Fact>
    Public Sub Loan_EmiTakenInFull_WhenUnderCap()
        Dim open As New List(Of OpenInstallment) From {New OpenInstallment(1, 1, 202610, 3000D)}
        Dim l As RunLineResult = Run(RunTestData.Facts(installments:=open)).Value
        Assert.Equal(3000D, l.LoanEmi)
        Assert.Equal(0D, l.LoanEmiDeferred)
        Assert.Equal(11270D, l.NetPay)
        Assert.Equal(4730D, l.TotalDeductions)
    End Sub

    <Fact>
    Public Sub Loan_EmiCappedAt25PercentOfPay()
        Dim open As New List(Of OpenInstallment) From {New OpenInstallment(1, 1, 202610, 5000D)}
        Dim l As RunLineResult = Run(RunTestData.Facts(installments:=open)).Value
        Assert.Equal(3567D, l.LoanEmi)
        Assert.Equal(1433D, l.LoanEmiDeferred)
        Assert.Equal(10703D, l.NetPay)
    End Sub

    <Fact>
    Public Sub Loan_InstallmentDueNextMonth_NotScheduled()
        Dim open As New List(Of OpenInstallment) From {New OpenInstallment(1, 1, 202611, 3000D)}
        Dim l As RunLineResult = Run(RunTestData.Facts(installments:=open)).Value
        Assert.Equal(0D, l.LoanEmi)
    End Sub

    <Fact>
    Public Sub CarryIn_ReducesPay()
        Dim l As RunLineResult = Run(RunTestData.Facts(carryIn:=1000D)).Value
        Assert.Equal(13270D, l.NetPay)
        Assert.Equal(1000D, l.CarryForwardIn)
        Assert.Equal(2730D, l.TotalDeductions)
    End Sub

    <Fact>
    Public Sub PayBelowDeductions_NetZero_ShortfallCarriesOut()
        Dim l As RunLineResult = Run(RunTestData.Facts(carryIn:=15000D)).Value
        Assert.Equal(0D, l.NetPay)
        Assert.Equal(730D, l.CarryForwardOut)
        Assert.Equal(16000D, l.TotalDeductions)
    End Sub

    <Fact>
    Public Sub NoSalary_FailsWithEmployeeCode()
        Dim r As OperationResult(Of RunLineResult) = Run(RunTestData.Facts(code:="E205", withSalary:=False))
        Assert.False(r.Succeeded)
        Assert.Contains("E205", r.Message)
    End Sub

    <Fact>
    Public Sub UnmarkedDays_Fail()
        Dim r As OperationResult(Of RunLineResult) = Run(RunTestData.Facts(unmarked:=2))
        Assert.False(r.Succeeded)
        Assert.Contains("E101", r.Message)
    End Sub

    <Fact>
    Public Sub PaidDaysAbovePeriod_Fail_ForMonthly()
        Assert.False(Run(RunTestData.Facts(paidDays:=32D)).Succeeded)
    End Sub

    <Fact>
    Public Sub Overtime_WithoutShift_Fails()
        Assert.False(Run(RunTestData.Facts(overtimeHours:=4D, hoursPerDay:=Nothing)).Succeeded)
    End Sub

    <Fact>
    Public Sub NoOvertime_WithoutShift_Passes()
        Assert.True(Run(RunTestData.Facts(hoursPerDay:=Nothing)).Succeeded)
    End Sub

    <Fact>
    Public Sub Overtime_AddsToGross_NotToEpfWage()
        Dim l As RunLineResult = Run(RunTestData.Facts(overtimeHours:=8D)).Value
        Assert.Equal(774D, l.OvertimeAmount)
        Assert.Equal(16774D, l.GrossEarnings)
        Assert.Equal(12000D, l.EpfWage)
    End Sub

    <Fact>
    Public Sub PtStateWithoutRule_Fails()
        Dim rates As New RunRates(StatutoryTestRates.Epf(), StatutoryTestRates.Esi(), StatutoryTestRates.CodeWage(), New Dictionary(Of Integer, Revolution.Core.PtRule)())
        Dim r As OperationResult(Of RunLineResult) = PayrollRunCalculator.Calculate(RunTestData.Facts(), RunTestData.October(), RunTestData.Policy(), rates)
        Assert.False(r.Succeeded)
        Assert.Contains("E101", r.Message)
    End Sub

    <Fact>
    Public Sub FemaleGross_UsesFemaleSlab()
        Dim f As New RunEmployeeFacts(101, 1, "E101", False, 2, 1, True, False, False, True, False, 31D, 0D, 8D, 0, 0D, 0D, RunTestData.Salary(), Nothing)
        Dim l As RunLineResult = Run(f).Value
        Assert.Equal(0D, l.ProfessionalTax)
    End Sub
End Class
