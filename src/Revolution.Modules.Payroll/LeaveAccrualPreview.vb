''' <summary>The accrual for a leave type and month as it would be posted. Nothing is stored.</summary>
Public NotInheritable Class LeaveAccrualPreview
    Public Sub New(leaveTypeId As Integer, month As Date, employeesChecked As Integer, entries As IReadOnlyList(Of LeaveAccrualEntry))
        If entries Is Nothing Then Throw New ArgumentNullException(NameOf(entries))
        Me.LeaveTypeId = leaveTypeId
        Me.Month = month
        Me.EmployeesChecked = employeesChecked
        Me.Entries = entries
    End Sub

    Public ReadOnly Property LeaveTypeId As Integer
    Public ReadOnly Property Month As Date
    ''' <summary>Employees who worked in the month, whether or not they earned anything.</summary>
    Public ReadOnly Property EmployeesChecked As Integer
    Public ReadOnly Property Entries As IReadOnlyList(Of LeaveAccrualEntry)

    Public ReadOnly Property TotalDays As Decimal
        Get
            Return Entries.Sum(Function(e As LeaveAccrualEntry) e.Days)
        End Get
    End Property
End Class
