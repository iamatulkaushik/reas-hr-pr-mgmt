''' <summary>ESI amounts for one employee for one month, in whole rupees.</summary>
Public NotInheritable Class EsiResult
    Public Sub New(eligible As Boolean, employeeShare As Decimal, employerShare As Decimal)
        Me.Eligible = eligible
        Me.EmployeeShare = employeeShare
        Me.EmployerShare = employerShare
    End Sub

    Public ReadOnly Property Eligible As Boolean
    Public ReadOnly Property EmployeeShare As Decimal
    Public ReadOnly Property EmployerShare As Decimal

    Public ReadOnly Property Total As Decimal
        Get
            Return EmployeeShare + EmployerShare
        End Get
    End Property
End Class
