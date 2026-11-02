Imports System.Globalization
Imports System.Text.Json

''' <summary>The month rows of a claim as the one JSON array pay.usp_ArrearsClaim_Add reads. Dates are written as yyyy-MM-dd. Pure.</summary>
Public NotInheritable Class ArrearsMonthsJson
    Private Sub New()
    End Sub

    Public Shared Function Build(months As IReadOnlyList(Of ArrearsMonthRecord)) As String
        If months Is Nothing Then Throw New ArgumentNullException(NameOf(months))
        Dim items As New List(Of Dictionary(Of String, Object))()
        For Each m As ArrearsMonthRecord In months
            items.Add(New Dictionary(Of String, Object) From {
                {"wageMonth", m.WageMonth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}, {"paidGross", m.PaidGross}, {"dueGross", m.DueGross},
                {"grossDifference", m.GrossDifference}, {"epfWages", m.EpfWages}, {"epsWages", m.EpsWages}, {"edliWages", m.EdliWages},
                {"employeeEpf", m.EmployeeEpf}, {"employerEpf", m.EmployerEpf}, {"employerEps", m.EmployerEps}, {"edli", m.Edli}, {"admin", m.Admin},
                {"esiEmployee", m.EsiEmployee}, {"esiEmployer", m.EsiEmployer}})
        Next
        Return JsonSerializer.Serialize(items)
    End Function
End Class
