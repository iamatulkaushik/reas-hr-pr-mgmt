''' <summary>A claim ready to store. The database works out the header totals from the months.</summary>
Public NotInheritable Class NewArrearsClaim
    Public Sub New(employeeId As Integer, reason As String, months As IReadOnlyList(Of ArrearsMonthRecord), userId As Integer?)
        If months Is Nothing Then Throw New ArgumentNullException(NameOf(months))
        Me.EmployeeId = employeeId
        Me.Reason = reason
        Me.Months = months
        Me.UserId = userId
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property Reason As String
    Public ReadOnly Property Months As IReadOnlyList(Of ArrearsMonthRecord)
    Public ReadOnly Property UserId As Integer?
End Class
