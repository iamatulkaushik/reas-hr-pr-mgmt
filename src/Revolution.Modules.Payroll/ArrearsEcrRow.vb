''' <summary>One member line of the arrear ECR: the eight fields EPFO takes for one wage month.</summary>
Public NotInheritable Class ArrearsEcrRow
    Public Sub New(uan As String, name As String, epfWages As Decimal, epsWages As Decimal, edliWages As Decimal, employeeEpf As Decimal, epsShare As Decimal, employerEpf As Decimal)
        Me.Uan = uan
        Me.Name = name
        Me.EpfWages = epfWages
        Me.EpsWages = epsWages
        Me.EdliWages = edliWages
        Me.EmployeeEpf = employeeEpf
        Me.EpsShare = epsShare
        Me.EmployerEpf = employerEpf
    End Sub

    Public ReadOnly Property Uan As String
    Public ReadOnly Property Name As String
    Public ReadOnly Property EpfWages As Decimal
    Public ReadOnly Property EpsWages As Decimal
    Public ReadOnly Property EdliWages As Decimal
    Public ReadOnly Property EmployeeEpf As Decimal
    Public ReadOnly Property EpsShare As Decimal
    Public ReadOnly Property EmployerEpf As Decimal

    Public Shared Function FromMonth(uan As String, name As String, month As ArrearsMonthResult) As ArrearsEcrRow
        If month Is Nothing Then Throw New ArgumentNullException(NameOf(month))
        Return New ArrearsEcrRow(uan, name, month.EpfWages, month.EpsWages, month.EdliWages, month.EmployeeEpf, month.EmployerEps, month.EmployerEpf)
    End Function
End Class
