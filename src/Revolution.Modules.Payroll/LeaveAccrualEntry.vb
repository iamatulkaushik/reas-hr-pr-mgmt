''' <summary>One employee's leave earned for a month, as the preview shows it and as it is posted. Days is more than zero.</summary>
Public NotInheritable Class LeaveAccrualEntry
    Public Sub New(employeeId As Integer, presentDays As Decimal, balance As Decimal, days As Decimal)
        Me.EmployeeId = employeeId
        Me.PresentDays = presentDays
        Me.Balance = balance
        Me.Days = days
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property PresentDays As Decimal
    ''' <summary>The balance before this accrual.</summary>
    Public ReadOnly Property Balance As Decimal
    Public ReadOnly Property Days As Decimal
End Class
