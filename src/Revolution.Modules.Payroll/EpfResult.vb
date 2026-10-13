''' <summary>EPF amounts for one employee for one month, in whole rupees.</summary>
Public NotInheritable Class EpfResult
    Public Sub New(contributoryWage As Decimal, employeeShare As Decimal, employerEpf As Decimal, employerEps As Decimal, edli As Decimal, admin As Decimal)
        Me.ContributoryWage = contributoryWage
        Me.EmployeeShare = employeeShare
        Me.EmployerEpf = employerEpf
        Me.EmployerEps = employerEps
        Me.Edli = edli
        Me.Admin = admin
    End Sub

    Public ReadOnly Property ContributoryWage As Decimal
    Public ReadOnly Property EmployeeShare As Decimal
    Public ReadOnly Property EmployerEpf As Decimal
    Public ReadOnly Property EmployerEps As Decimal
    Public ReadOnly Property Edli As Decimal
    ''' <summary>Admin charge for this employee. The establishment minimum is applied in the return (Phase 7), not here.</summary>
    Public ReadOnly Property Admin As Decimal

    Public ReadOnly Property EmployerShare As Decimal
        Get
            Return EmployerEpf + EmployerEps
        End Get
    End Property
End Class
