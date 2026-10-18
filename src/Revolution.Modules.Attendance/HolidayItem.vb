''' <summary>A company holiday (att.Holiday).</summary>
Public NotInheritable Class HolidayItem
    Public Sub New(day As Date, name As String)
        Me.Day = day.Date
        Me.Name = If(name, String.Empty)
    End Sub

    Public ReadOnly Property Day As Date
    Public ReadOnly Property Name As String
End Class
