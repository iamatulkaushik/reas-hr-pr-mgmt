Imports Revolution.Modules.Payroll

Friend NotInheritable Class FakeBonusSource
    Implements IBonusSource

    Public ReadOnly Months As New List(Of BonusMonthFact)()
    Public ReadOnly Paid As New Dictionary(Of Integer, Decimal)()
    Public ReadOnly Flags As New Dictionary(Of Integer, BonusEmployeeFlags)()
    Public LoadedYear As Date
    Public NextNo As Integer = 2
    Public RegisteredRun As Integer
    Public RegisteredYear As Date
    Public RegisteredPct As Decimal
    Public FailRegister As String

    Public Function Load(yearFrom As Date) As BonusFacts Implements IBonusSource.Load
        LoadedYear = yearFrom
        Return New BonusFacts(Months, Paid, Flags)
    End Function

    Public Function NextRunNo(period As RunPeriod) As Integer Implements IBonusSource.NextRunNo
        Return NextNo
    End Function

    Public Sub Register(runId As Integer, yearFrom As Date, pct As Decimal, userId As Integer?) Implements IBonusSource.Register
        If FailRegister IsNot Nothing Then Throw New PayrollRuleException(FailRegister, 53121, Nothing)
        RegisteredRun = runId
        RegisteredYear = yearFrom
        RegisteredPct = pct
    End Sub

    ''' <summary>Twelve months April 2025 to March 2026 of the same Basic plus DA and 30 paid days.</summary>
    Public Sub Year(employeeId As Integer, code As String, wage As Decimal, Optional paidDays As Decimal = 30D, Optional factoryId As Integer = 1)
        For i As Integer = 0 To 11
            Dim d As Date = New Date(2025, 4, 1).AddMonths(i)
            Months.Add(New BonusMonthFact(employeeId, code, factoryId, d.Year * 100 + d.Month, wage, paidDays))
        Next
    End Sub
End Class

''' <summary>Tax facts for employees who earned 2,00,000 a month from April to October 2026, so a bonus in November 2026 is taxed at the top slab.</summary>
Friend NotInheritable Class FakeHighEarnerTaxSource
    Implements IRunTaxSource

    Public Calls As Integer
    Public LastRunId As Integer = -1
    Public LastFinancialYear As Integer
    Public SkipEmployee As Integer

    Public Function Load(runId As Integer, financialYear As Integer, employeeIds As IReadOnlyList(Of Integer)) As IReadOnlyDictionary(Of Integer, EmployeeTaxFacts) Implements IRunTaxSource.Load
        Calls += 1
        LastRunId = runId
        LastFinancialYear = financialYear
        Dim months As New List(Of TdsMonthFacts)()
        For month As Integer = 4 To 10
            months.Add(New TdsMonthFacts(202600 + month, 100000D, 40000D, 200000D))
        Next
        Dim d As New Dictionary(Of Integer, EmployeeTaxFacts)()
        For Each id As Integer In employeeIds
            If id <> SkipEmployee Then d(id) = New EmployeeTaxFacts(id, New Date(1990, 5, 5), months, 0D, Nothing)
        Next
        Return d
    End Function
End Class
