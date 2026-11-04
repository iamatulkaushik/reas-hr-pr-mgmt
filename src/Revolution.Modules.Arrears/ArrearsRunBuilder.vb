Imports Revolution.Modules.Payroll

''' <summary>
''' Builds the lines of an arrears run (RunNo 2 and up) from open claims, one line per employee, head by head (Owner, 2026-11-03). The line carries the arrears as earnings,
''' the EPF and ESI taken from the employee and the employer's shares. No tax, no professional tax, no loan and no carry forward: tax is deducted in the regular run only,
''' and that run's year to date already counts this run once it is locked. A claim is left out, and named by employee code, when it is saved without head amounts,
''' when any amount in it is below zero (an overpayment, to be recovered through a regular run) or when the net would be below zero. Pure.
''' </summary>
Public NotInheritable Class ArrearsRunBuilder
    Private Sub New()
    End Sub

    ''' <param name="claims">Open claims with their months and heads.</param>
    ''' <param name="factoryIds">Factory of each employee for the pay period of the run.</param>
    Public Shared Function Build(claims As IReadOnlyList(Of ArrearsClaimDetail), factoryIds As IReadOnlyDictionary(Of Integer, Integer)) As ArrearsRunPlan
        If claims Is Nothing Then Throw New ArgumentNullException(NameOf(claims))
        If factoryIds Is Nothing Then Throw New ArgumentNullException(NameOf(factoryIds))
        Dim problems As New List(Of String)()
        Dim usable As New List(Of ArrearsClaimDetail)()
        For Each claim As ArrearsClaimDetail In claims
            Dim reason As String = Check(claim, factoryIds)
            If reason.Length > 0 Then
                problems.Add(claim.Claim.EmpCode & ": " & reason)
            ElseIf Not IsEmpty(claim) Then
                usable.Add(claim)
            End If
        Next
        Dim lines As New List(Of RunLineResult)()
        Dim included As New List(Of Integer)()
        For Each group As IGrouping(Of Integer, ArrearsClaimDetail) In usable.GroupBy(Function(c As ArrearsClaimDetail) c.Claim.EmployeeId).OrderBy(Function(g As IGrouping(Of Integer, ArrearsClaimDetail)) g.Key)
            Dim built As RunLineResult = LineFor(group.Key, factoryIds(group.Key), group.ToList())
            If built.NetPay < 0D Then
                problems.Add(group.First().Claim.EmpCode & ": the net would be below zero.")
                Continue For
            End If
            lines.Add(built)
            included.AddRange(group.Select(Function(c As ArrearsClaimDetail) c.Claim.ClaimId))
        Next
        Return New ArrearsRunPlan(lines, included, problems)
    End Function

    Private Shared Function Check(claim As ArrearsClaimDetail, factoryIds As IReadOnlyDictionary(Of Integer, Integer)) As String
        If Not factoryIds.ContainsKey(claim.Claim.EmployeeId) Then Return "no pay facts for this period."
        If claim.Months.Count = 0 Then Return "the claim has no months."
        For Each m As ArrearsMonthRecord In claim.Months
            If m.GrossDifference <> m.OvertimeDifference + m.Heads.Sum(Function(h As ArrearsHeadAmount) h.Amount) Then Return "saved without head amounts. Cancel it and make it again."
            If AnyBelowZero(m) Then Return "an amount is below zero. A claim that is an overpayment as a whole is taken back by the regular run; if only some months are below zero, cancel the claim and make it again."
        Next
        Return String.Empty
    End Function

    Private Shared Function AnyBelowZero(m As ArrearsMonthRecord) As Boolean
        If m.GrossDifference < 0D OrElse m.OvertimeDifference < 0D OrElse m.EpfWages < 0D OrElse m.EsiWages < 0D OrElse m.EmployeeEpf < 0D OrElse m.EmployerEpf < 0D Then Return True
        If m.EmployerEps < 0D OrElse m.Edli < 0D OrElse m.Admin < 0D OrElse m.EsiEmployee < 0D OrElse m.EsiEmployer < 0D Then Return True
        Return m.Heads.Any(Function(h As ArrearsHeadAmount) h.Amount < 0D)
    End Function

    Private Shared Function IsEmpty(claim As ArrearsClaimDetail) As Boolean
        Return claim.Months.All(Function(m As ArrearsMonthRecord) m.GrossDifference = 0D AndAlso m.EmployeeEpf = 0D AndAlso m.EsiEmployee = 0D AndAlso m.EmployerEpf = 0D AndAlso m.EsiEmployer = 0D)
    End Function

    Private Shared Function LineFor(employeeId As Integer, factoryId As Integer, claims As List(Of ArrearsClaimDetail)) As RunLineResult
        Dim months As List(Of ArrearsMonthRecord) = claims.SelectMany(Function(c As ArrearsClaimDetail) c.Months).ToList()
        Dim gross As Decimal = months.Sum(Function(m As ArrearsMonthRecord) m.GrossDifference)
        Dim epfEmployee As Decimal = months.Sum(Function(m As ArrearsMonthRecord) m.EmployeeEpf)
        Dim esiEmployee As Decimal = months.Sum(Function(m As ArrearsMonthRecord) m.EsiEmployee)
        Dim earnings As List(Of EarningAmount) = months.SelectMany(Function(m As ArrearsMonthRecord) m.Heads).GroupBy(Function(h As ArrearsHeadAmount) h.ComponentId).
            OrderBy(Function(g As IGrouping(Of Integer, ArrearsHeadAmount)) g.Key).
            Select(Function(g As IGrouping(Of Integer, ArrearsHeadAmount)) New EarningAmount(g.Key, g.Sum(Function(h As ArrearsHeadAmount) h.Amount))).
            Where(Function(e As EarningAmount) e.Amount <> 0D).ToList()
        Dim totalDeductions As Decimal = epfEmployee + esiEmployee
        Return New RunLineResult(employeeId, factoryId, 0D, 0D, months.Sum(Function(m As ArrearsMonthRecord) m.OvertimeDifference), gross,
                                 months.Sum(Function(m As ArrearsMonthRecord) m.EpfWages), months.Sum(Function(m As ArrearsMonthRecord) m.EsiWages), epfEmployee,
                                 months.Sum(Function(m As ArrearsMonthRecord) m.EmployerEpf), months.Sum(Function(m As ArrearsMonthRecord) m.EmployerEps),
                                 months.Sum(Function(m As ArrearsMonthRecord) m.Edli), months.Sum(Function(m As ArrearsMonthRecord) m.Admin), esiEmployee,
                                 months.Sum(Function(m As ArrearsMonthRecord) m.EsiEmployer), 0D, 0D, 0D, 0D, 0D, 0D, totalDeductions, gross - totalDeductions, earnings)
    End Function
End Class
