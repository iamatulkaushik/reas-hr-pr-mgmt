''' <summary>Provident fund flags of an employee, as in the run input: member or not, and whether contributions (and pension) are on the actual wage above the ceiling.</summary>
Public NotInheritable Class BonusEmployeeFlags
    Public Sub New(employeeId As Integer, isEpfMember As Boolean, epfOnHigherWage As Boolean, epsOnHigherWage As Boolean)
        Me.EmployeeId = employeeId
        Me.IsEpfMember = isEpfMember
        Me.EpfOnHigherWage = epfOnHigherWage
        Me.EpsOnHigherWage = epsOnHigherWage
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property IsEpfMember As Boolean
    Public ReadOnly Property EpfOnHigherWage As Boolean
    Public ReadOnly Property EpsOnHigherWage As Boolean
End Class
