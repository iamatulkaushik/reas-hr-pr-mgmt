''' <summary>
''' Maps run lines to ESIC rows. Only ESI members. Days round up to a whole number. Wages are the ESI wage.
''' Reason code and last working day come from the employee record; empty reason means 0.
''' </summary>
Public NotInheritable Class EsicMapper
    Private Sub New()
    End Sub

    Public Shared Function Map(employees As IEnumerable(Of PayrollFileEmployee)) As MapResult(Of EsicRow)
        If employees Is Nothing Then
            Throw New ArgumentNullException(NameOf(employees))
        End If
        Dim items As New List(Of EsicRow)()
        Dim problems As New List(Of String)()
        For Each e As PayrollFileEmployee In employees.Where(Function(x) x.IsEsiMember)
            If String.IsNullOrWhiteSpace(e.EsiIpNumber) Then
                problems.Add($"Employee {e.EmployeeID}: ESI IP number is missing. Left out of ESIC sheet.")
            Else
                items.Add(New EsicRow(e.EsiIpNumber.Trim(), e.Name, CInt(Math.Ceiling(e.PaidDays)),
                                      WholeRupee.Round(e.EsiWage), e.EsiReasonCode, e.LastWorkingDay))
            End If
        Next
        Return New MapResult(Of EsicRow)(items, problems)
    End Function
End Class
