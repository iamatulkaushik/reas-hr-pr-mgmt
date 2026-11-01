Imports Revolution.Core

''' <summary>
''' Arrears for one employee. For each wage month: due earnings minus paid earnings, and contributions on the difference.
''' Contributions are worked as contribution(due) minus contribution(paid), so ceilings and rounding stay right.
''' Pure: the caller supplies the paid and due breakdowns and the rate rows for each month.
''' </summary>
Public NotInheritable Class ArrearsCalculator
    Private Sub New()
    End Sub

    Public Shared Function Calculate(months As IReadOnlyList(Of ArrearsMonthInput)) As ArrearsResult
        If months Is Nothing Then Throw New ArgumentNullException(NameOf(months))
        If months.Count = 0 Then Throw New ArgumentException("Give at least one wage month.", NameOf(months))
        Dim seen As New HashSet(Of Date)()
        Dim results As New List(Of ArrearsMonthResult)()
        For Each input As ArrearsMonthInput In months.OrderBy(Function(m As ArrearsMonthInput) m.WageMonth)
            If Not seen.Add(input.WageMonth) Then
                Throw New ArgumentException("Wage month " & input.WageMonth.ToString("MM-yyyy") & " is given twice.", NameOf(months))
            End If
            results.Add(CalculateMonth(input))
        Next
        Return New ArrearsResult(results)
    End Function

    Public Shared Function CalculateMonth(input As ArrearsMonthInput) As ArrearsMonthResult
        If input Is Nothing Then Throw New ArgumentNullException(NameOf(input))
        Dim epfRate As EpfRate = input.Rates.Epf
        Dim epfDue As EpfResult = EpfCalculator.Calculate(input.Due.EpfWage, epfRate, input.EpfOnActualWage, input.PensionOnActualWage)
        Dim epfPaid As EpfResult = EpfCalculator.Calculate(input.Paid.EpfWage, epfRate, input.EpfOnActualWage, input.PensionOnActualWage)
        Dim esiDue As EsiResult = EsiCalculator.Calculate(input.Due.EsiWage, input.DailyWage, input.Rates.Esi, input.StayEsiEligible)
        Dim esiPaid As EsiResult = EsiCalculator.Calculate(input.Paid.EsiWage, input.DailyWage, input.Rates.Esi, input.StayEsiEligible)
        Dim epsDueWage As Decimal = PensionWage(input.Due.EpfWage, epfRate, input.PensionOnActualWage)
        Dim epsPaidWage As Decimal = PensionWage(input.Paid.EpfWage, epfRate, input.PensionOnActualWage)
        Dim edliDueWage As Decimal = Math.Min(input.Due.EpfWage, epfRate.EdliWageCeiling)
        Dim edliPaidWage As Decimal = Math.Min(input.Paid.EpfWage, epfRate.EdliWageCeiling)
        Return New ArrearsMonthResult(
            input.WageMonth,
            LineDifference(input.Paid.Lines, input.Due.Lines),
            input.Due.OvertimeAmount - input.Paid.OvertimeAmount,
            input.Due.GrossEarnings - input.Paid.GrossEarnings,
            epfDue.ContributoryWage - epfPaid.ContributoryWage,
            epsDueWage - epsPaidWage,
            edliDueWage - edliPaidWage,
            epfDue.EmployeeShare - epfPaid.EmployeeShare,
            epfDue.EmployerEpf - epfPaid.EmployerEpf,
            epfDue.EmployerEps - epfPaid.EmployerEps,
            epfDue.Edli - epfPaid.Edli,
            epfDue.Admin - epfPaid.Admin,
            esiDue.EmployeeShare - esiPaid.EmployeeShare,
            esiDue.EmployerShare - esiPaid.EmployerShare)
    End Function

    Private Shared Function PensionWage(epfWage As Decimal, rate As EpfRate, onActual As Boolean) As Decimal
        Return If(onActual, epfWage, Math.Min(epfWage, rate.WageCeiling))
    End Function

    Private Shared Function LineDifference(paid As IReadOnlyList(Of EarnedLine), due As IReadOnlyList(Of EarnedLine)) As IReadOnlyList(Of EarnedLine)
        Dim codes As New List(Of String)()
        For Each line As EarnedLine In due.Concat(paid)
            If Not codes.Contains(line.Code) Then codes.Add(line.Code)
        Next
        Dim result As New List(Of EarnedLine)()
        For Each code As String In codes
            Dim difference As Decimal = Sum(due, code) - Sum(paid, code)
            If difference <> 0D Then result.Add(New EarnedLine(code, difference))
        Next
        Return result
    End Function

    Private Shared Function Sum(lines As IReadOnlyList(Of EarnedLine), code As String) As Decimal
        Return lines.Where(Function(l As EarnedLine) l.Code = code).Sum(Function(l As EarnedLine) l.Amount)
    End Function
End Class
