''' <summary>The employee-days found from punches for a date range, with what each would get. Nothing is stored by building it.</summary>
Public NotInheritable Class PunchAttendancePlan
    Public Sub New(fromDate As Date, toDate As Date, rows As IReadOnlyList(Of PunchAttendanceRow))
        If rows Is Nothing Then Throw New ArgumentNullException(NameOf(rows))
        Me.FromDate = fromDate
        Me.ToDate = toDate
        Me.Rows = rows
    End Sub

    Public ReadOnly Property FromDate As Date
    Public ReadOnly Property ToDate As Date
    Public ReadOnly Property Rows As IReadOnlyList(Of PunchAttendanceRow)

    Public Function Count(action As PunchAttendanceAction) As Integer
        Return Rows.Where(Function(r As PunchAttendanceRow) r.Action = action).Count()
    End Function
End Class
