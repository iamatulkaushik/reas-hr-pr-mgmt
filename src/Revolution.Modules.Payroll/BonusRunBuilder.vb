Imports Revolution.Core

''' <summary>
''' Builds the bonus run of an accounting year from the months of each employee (Owner decision 2026-10-27: percentage inside the rule's range, rule row 0407). One row per employee with pay in the year:
''' BonusCalculator decides eligibility and the amount, and what was already paid for the year is taken off. An employee with something to pay gets a run line: the bonus as gross earnings, provident
''' fund on the bonus as its own wage (members only, the same calculator as the regular run), and the income tax on the bonus when a tax hook is given (Owner 2026-11-04: the bonus run carries PF and
''' income tax; no ESI and no professional tax). Net pay is the bonus less those. Tax that the bonus cannot cover is reported as a shortfall on the line, as in the regular run.
''' Days worked are the days paid in the year, whole days only (half a day does not count towards the minimum). The latest month's Basic plus DA decides eligibility. Pure.
''' </summary>
Public NotInheritable Class BonusRunBuilder
    Private Sub New()
    End Sub

    Public Shared Function Build(rule As BonusRule, facts As BonusFacts, pct As Decimal, epfRate As EpfRate,
                                 Optional taxFor As Func(Of Integer, Decimal, OperationResult(Of Decimal)) = Nothing) As BonusRunPlan
        If rule Is Nothing Then Throw New ArgumentNullException(NameOf(rule))
        If facts Is Nothing Then Throw New ArgumentNullException(NameOf(facts))
        If epfRate Is Nothing Then Throw New ArgumentNullException(NameOf(epfRate))
        Dim rows As New List(Of BonusRow)()
        Dim lines As New List(Of RunLineResult)()
        Dim problems As New List(Of String)()
        For Each group As IGrouping(Of Integer, BonusMonthFact) In facts.Months.GroupBy(Function(m As BonusMonthFact) m.EmployeeId).OrderBy(Function(g As IGrouping(Of Integer, BonusMonthFact)) g.Key)
            Dim months As List(Of BonusMonthFact) = group.OrderBy(Function(m As BonusMonthFact) m.MonthKey).ToList()
            Dim last As BonusMonthFact = months(months.Count - 1)
            Dim days As Integer = CInt(Math.Floor(months.Sum(Function(m As BonusMonthFact) m.PaidDays)))
            Dim paid As Decimal = 0D
            facts.AlreadyPaid.TryGetValue(group.Key, paid)
            Dim result As BonusResult = BonusCalculator.Calculate(rule, last.BasicPlusDa, months.Select(Function(m As BonusMonthFact) m.BasicPlusDa).ToList(), days, pct, paid)
            If Not (result.IsEligible AndAlso result.Amount > 0D) Then
                rows.Add(New BonusRow(group.Key, last.EmpCode, result, days, last.BasicPlusDa))
                Continue For
            End If
            Dim flags As BonusEmployeeFlags = Nothing
            facts.Flags.TryGetValue(group.Key, flags)
            Dim epf As EpfResult = If(flags IsNot Nothing AndAlso flags.IsEpfMember,
                                      EpfCalculator.Calculate(result.Amount, epfRate, flags.EpfOnHigherWage, flags.EpsOnHigherWage AndAlso flags.EpfOnHigherWage),
                                      New EpfResult(0D, 0D, 0D, 0D, 0D, 0D))
            Dim tdsDue As Decimal = 0D
            If taxFor IsNot Nothing Then
                Dim tax As OperationResult(Of Decimal) = taxFor(group.Key, result.Amount)
                If Not tax.Succeeded Then
                    problems.Add($"{last.EmpCode}: {tax.Message}")
                    rows.Add(New BonusRow(group.Key, last.EmpCode, result, days, last.BasicPlusDa))
                    Continue For
                End If
                tdsDue = tax.Value
            End If
            Dim net As NetPayResult = NetPayCalculator.Calculate(result.Amount, epf.EmployeeShare, 0D, 0D, 0D, 0D, tdsDue)
            rows.Add(New BonusRow(group.Key, last.EmpCode, result, days, last.BasicPlusDa, epf.EmployeeShare, net.TdsTaken, net.NetPay))
            lines.Add(Line(group.Key, last.FactoryId, result.Amount, If(flags IsNot Nothing AndAlso flags.IsEpfMember, result.Amount, 0D), epf, net))
        Next
        Return New BonusRunPlan(rows, lines, pct, rule.MinPct, rule.MaxPct, problems)
    End Function

    Private Shared Function Line(employeeId As Integer, factoryId As Integer, amount As Decimal, epfWage As Decimal, epf As EpfResult, net As NetPayResult) As RunLineResult
        Return New RunLineResult(employeeId, factoryId, 0D, 0D, 0D, amount, epfWage, 0D, epf.EmployeeShare, epf.EmployerEpf, epf.EmployerEps, epf.Edli, epf.Admin,
                                 0D, 0D, 0D, 0D, 0D, 0D, 0D, net.CarryForwardOut, amount - net.NetPay, net.NetPay, New List(Of EarningAmount)(), net.TdsTaken, net.TdsShortfall)
    End Function
End Class
