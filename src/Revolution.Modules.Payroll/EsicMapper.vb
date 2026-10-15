''' <summary>
''' Maps run lines to ESIC rows. Only ESI members. Days round up to a whole number. Wages are the ESI wage.
''' Reason code and last working day come from the employee record; empty reason means 0.
''' MapDetails also carries the figures for the optional detail sheet.
''' </summary>
Public NotInheritable Class EsicMapper
    Private Sub New()
    End Sub

    Public Shared Function Map(employees As IEnumerable(Of PayrollFileEmployee)) As MapResult(Of EsicRow)
        Dim details As MapResult(Of EsicDetail) = MapDetails(employees)
        Return New MapResult(Of EsicRow)(details.Items.Select(Function(d) d.Row).ToList(), details.Problems)
    End Function

    Public Shared Function MapDetails(employees As IEnumerable(Of PayrollFileEmployee)) As MapResult(Of EsicDetail)
        If employees Is Nothing Then
            Throw New ArgumentNullException(NameOf(employees))
        End If
        Dim items As New List(Of EsicDetail)()
        Dim problems As New List(Of String)()
        For Each e As PayrollFileEmployee In employees.Where(Function(x) x.IsEsiMember)
            If String.IsNullOrWhiteSpace(e.EsiIpNumber) Then
                problems.Add($"Employee {e.EmployeeID}: ESI IP number is missing. Left out of ESIC sheet.")
            Else
                Dim row As New EsicRow(e.EsiIpNumber.Trim(), e.Name, CInt(Math.Ceiling(e.PaidDays)),
                                       WholeRupee.Round(e.EsiWage), e.EsiReasonCode, e.LastWorkingDay)
                items.Add(New EsicDetail(e.EmployeeCode, row, e.GrossEarnings, e.EsiEmployee, e.EsiEmployer))
            End If
        Next
        Return New MapResult(Of EsicDetail)(items, problems)
    End Function
End Class
