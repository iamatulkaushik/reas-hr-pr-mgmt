''' <summary>
''' Maps run lines to ECR members. Only EPF members. The employee name is used as the name as per UAN.
''' EPF wages are the run line contributory wage (actual or capped, as the member chose).
''' EPS wages are capped at the ceiling unless the member chose pension on higher wages. EDLI wages are always capped.
''' NCP days = calendar days of the wage month less paid days rounded up, never below zero.
''' </summary>
Public NotInheritable Class EcrMapper
    Private Sub New()
    End Sub

    Public Shared Function Map(employees As IEnumerable(Of PayrollFileEmployee), wageCeiling As Decimal, wageMonth As Date) As MapResult(Of EcrMember)
        If employees Is Nothing Then
            Throw New ArgumentNullException(NameOf(employees))
        End If
        Dim monthDays As Integer = Date.DaysInMonth(wageMonth.Year, wageMonth.Month)
        Dim items As New List(Of EcrMember)()
        Dim problems As New List(Of String)()
        For Each e As PayrollFileEmployee In employees.Where(Function(x) x.IsEpfMember)
            If String.IsNullOrWhiteSpace(e.Uan) Then
                problems.Add($"Employee {e.EmployeeID}: UAN is missing. Left out of ECR.")
            Else
                items.Add(ToMember(e, wageCeiling, monthDays))
            End If
        Next
        Return New MapResult(Of EcrMember)(items, problems)
    End Function

    Private Shared Function ToMember(e As PayrollFileEmployee, wageCeiling As Decimal, monthDays As Integer) As EcrMember
        Dim epfWage As Decimal = WholeRupee.Round(e.EpfWage)
        Dim cappedWage As Decimal = Math.Min(epfWage, WholeRupee.Round(wageCeiling))
        Dim epsWage As Decimal = 0D
        If e.EpfEmployerEps > 0D Then
            epsWage = If(e.EpsOnHigherWage, epfWage, cappedWage)
        End If
        Dim edliWage As Decimal = If(e.Edli > 0D, cappedWage, 0D)
        Dim ncp As Integer = Math.Max(0, monthDays - CInt(Math.Ceiling(e.PaidDays)))
        Return New EcrMember(e.Uan.Trim(), e.Name, WholeRupee.Round(e.GrossEarnings), epfWage, epsWage, edliWage,
                             WholeRupee.Round(e.EpfEmployee), WholeRupee.Round(e.EpfEmployerEps),
                             WholeRupee.Round(e.EpfEmployerEpf), ncp, 0D, e.EpsOnHigherWage)
    End Function
End Class
