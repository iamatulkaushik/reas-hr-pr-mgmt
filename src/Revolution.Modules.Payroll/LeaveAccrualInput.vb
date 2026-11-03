''' <summary>What accrual reads for one employee and month: the days present and the leave balance of the type before this accrual.</summary>
Public NotInheritable Class LeaveAccrualInput
    Public Sub New(employeeId As Integer, presentDays As Decimal, balance As Decimal)
        Me.EmployeeId = employeeId
        Me.PresentDays = presentDays
        Me.Balance = balance
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property PresentDays As Decimal
    Public ReadOnly Property Balance As Decimal
End Class
