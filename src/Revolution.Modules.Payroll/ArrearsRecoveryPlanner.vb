''' <summary>
''' Adds the overpaid arrears claims to the other deductions of the employees in a regular run (Owner, 2026-11-03: overpayments are recovered through a regular run). Pure.
''' The whole amount goes into other deductions; if pay is too small the net pay calculator carries the shortfall forward, as for any other deduction.
''' </summary>
Public NotInheritable Class ArrearsRecoveryPlanner
    Private Sub New()
    End Sub

    Public Shared Function Plan(facts As IReadOnlyList(Of RunEmployeeFacts), due As IReadOnlyList(Of ArrearsRecoveryDue)) As ArrearsRecoveryPlan
        If facts Is Nothing Then Throw New ArgumentNullException(NameOf(facts))
        If due Is Nothing Then Throw New ArgumentNullException(NameOf(due))
        Dim byEmployee As Dictionary(Of Integer, Decimal) = due.GroupBy(Function(d As ArrearsRecoveryDue) d.EmployeeId).ToDictionary(Function(g As IGrouping(Of Integer, ArrearsRecoveryDue)) g.Key, Function(g As IGrouping(Of Integer, ArrearsRecoveryDue)) g.Sum(Function(d As ArrearsRecoveryDue) d.Amount))
        Dim inRun As New HashSet(Of Integer)()
        Dim adjusted As New List(Of RunEmployeeFacts)(facts.Count)
        For Each f As RunEmployeeFacts In facts
            Dim recover As Decimal = 0D
            If byEmployee.TryGetValue(f.EmployeeId, recover) Then
                inRun.Add(f.EmployeeId)
                adjusted.Add(f.WithOtherDeductions(f.OtherDeductions + recover))
            Else
                adjusted.Add(f)
            End If
        Next
        Dim claimIds As List(Of Integer) = due.Where(Function(d As ArrearsRecoveryDue) inRun.Contains(d.EmployeeId)).Select(Function(d As ArrearsRecoveryDue) d.ClaimId).Distinct().OrderBy(Function(id As Integer) id).ToList()
        Dim missed As List(Of String) = due.Where(Function(d As ArrearsRecoveryDue) Not inRun.Contains(d.EmployeeId)).Select(Function(d As ArrearsRecoveryDue) d.EmpCode).Distinct().OrderBy(Function(c As String) c, StringComparer.Ordinal).ToList()
        Return New ArrearsRecoveryPlan(adjusted, claimIds, missed)
    End Function
End Class
