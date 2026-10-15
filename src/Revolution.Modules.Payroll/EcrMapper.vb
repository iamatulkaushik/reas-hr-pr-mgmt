''' <summary>
''' Maps run lines to ECR members. Only EPF members. EPS and EDLI wages are capped at the ceiling from the EPF rate row.
''' NCP days = base days less paid days, never below zero. Higher wage option is not handled yet.
''' </summary>
Public NotInheritable Class EcrMapper
    Private Sub New()
    End Sub

    Public Shared Function Map(employees As IEnumerable(Of PayrollFileEmployee), wageCeiling As Decimal, ncpBaseDays As Integer) As MapResult(Of EcrMember)
        If employees Is Nothing Then
            Throw New ArgumentNullException(NameOf(employees))
        End If
        Dim items As New List(Of EcrMember)()
        Dim problems As New List(Of String)()
        For Each e As PayrollFileEmployee In employees.Where(Function(x) x.IsEpfMember)
            If String.IsNullOrWhiteSpace(e.Uan) Then
                problems.Add($"Employee {e.EmployeeID}: UAN is missing. Left out of ECR.")
            Else
                items.Add(ToMember(e, wageCeiling, ncpBaseDays))
            End If
        Next
        Return New MapResult(Of EcrMember)(items, problems)
    End Function

    Private Shared Function ToMember(e As PayrollFileEmployee, wageCeiling As Decimal, ncpBaseDays As Integer) As EcrMember
        Dim epfWage As Decimal = WholeRupee.Round(e.EpfWage)
        Dim cappedWage As Decimal = Math.Min(epfWage, WholeRupee.Round(wageCeiling))
        Dim epsWage As Decimal = If(e.EpfEmployerEps > 0D, cappedWage, 0D)
        Dim edliWage As Decimal = If(e.Edli > 0D, cappedWage, 0D)
        Dim ncp As Integer = Math.Max(0, ncpBaseDays - CInt(Math.Ceiling(e.PaidDays)))
        Return New EcrMember(e.Uan.Trim(), e.Name, WholeRupee.Round(e.GrossEarnings), epfWage, epsWage, edliWage,
                             WholeRupee.Round(e.EpfEmployee), WholeRupee.Round(e.EpfEmployerEps),
                             WholeRupee.Round(e.EpfEmployerEpf), ncp, 0D)
    End Function
End Class
