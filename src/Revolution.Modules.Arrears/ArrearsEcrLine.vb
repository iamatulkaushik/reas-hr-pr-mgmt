''' <summary>One employee's arrear amounts for one wage month, from the open claims. Signed whole rupees. No UAN: that is released at the moment the file is built.</summary>
Public NotInheritable Class ArrearsEcrLine
    Public Sub New(employeeId As Integer, empCode As String, fullName As String, epfWages As Decimal, epsWages As Decimal, edliWages As Decimal,
                   employeeEpf As Decimal, employerEps As Decimal, employerEpf As Decimal)
        Me.EmployeeId = employeeId
        Me.EmpCode = empCode
        Me.FullName = fullName
        Me.EpfWages = epfWages
        Me.EpsWages = epsWages
        Me.EdliWages = edliWages
        Me.EmployeeEpf = employeeEpf
        Me.EmployerEps = employerEps
        Me.EmployerEpf = employerEpf
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property FullName As String
    Public ReadOnly Property EpfWages As Decimal
    Public ReadOnly Property EpsWages As Decimal
    Public ReadOnly Property EdliWages As Decimal
    Public ReadOnly Property EmployeeEpf As Decimal
    Public ReadOnly Property EmployerEps As Decimal
    Public ReadOnly Property EmployerEpf As Decimal

    Public ReadOnly Property IsEmpty As Boolean
        Get
            Return EpfWages = 0D AndAlso EpsWages = 0D AndAlso EdliWages = 0D AndAlso EmployeeEpf = 0D AndAlso EmployerEps = 0D AndAlso EmployerEpf = 0D
        End Get
    End Property

    Public ReadOnly Property HasNegative As Boolean
        Get
            Return EpfWages < 0D OrElse EpsWages < 0D OrElse EdliWages < 0D OrElse EmployeeEpf < 0D OrElse EmployerEps < 0D OrElse EmployerEpf < 0D
        End Get
    End Property
End Class
