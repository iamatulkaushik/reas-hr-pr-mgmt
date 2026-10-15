''' <summary>One member line of the EPFO ECR file. All money is whole rupees.</summary>
Public NotInheritable Class EcrMember
    Public Sub New(uan As String, name As String, grossWages As Decimal, epfWages As Decimal, epsWages As Decimal,
                   edliWages As Decimal, employeeEpf As Decimal, epsContribution As Decimal,
                   employerDifference As Decimal, ncpDays As Integer, refundOfAdvances As Decimal)
        Me.Uan = uan
        Me.Name = name
        Me.GrossWages = grossWages
        Me.EpfWages = epfWages
        Me.EpsWages = epsWages
        Me.EdliWages = edliWages
        Me.EmployeeEpf = employeeEpf
        Me.EpsContribution = epsContribution
        Me.EmployerDifference = employerDifference
        Me.NcpDays = ncpDays
        Me.RefundOfAdvances = refundOfAdvances
    End Sub

    Public ReadOnly Property Uan As String
    ''' <summary>Name as per the UAN record, not the HR name.</summary>
    Public ReadOnly Property Name As String
    Public ReadOnly Property GrossWages As Decimal
    Public ReadOnly Property EpfWages As Decimal
    Public ReadOnly Property EpsWages As Decimal
    Public ReadOnly Property EdliWages As Decimal
    Public ReadOnly Property EmployeeEpf As Decimal
    Public ReadOnly Property EpsContribution As Decimal
    ''' <summary>Employer EPF share: employer total less EPS.</summary>
    Public ReadOnly Property EmployerDifference As Decimal
    Public ReadOnly Property NcpDays As Integer
    Public ReadOnly Property RefundOfAdvances As Decimal
End Class
