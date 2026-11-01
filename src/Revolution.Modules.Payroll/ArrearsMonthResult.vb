''' <summary>Difference for one wage month, due minus paid, in whole rupees. A negative amount means the employee was overpaid.</summary>
Public NotInheritable Class ArrearsMonthResult
    Public Sub New(wageMonth As Date, lines As IReadOnlyList(Of EarnedLine), overtimeDifference As Decimal, grossDifference As Decimal,
                   epfWages As Decimal, epsWages As Decimal, edliWages As Decimal,
                   employeeEpf As Decimal, employerEpf As Decimal, employerEps As Decimal, edli As Decimal, admin As Decimal,
                   esiEmployee As Decimal, esiEmployer As Decimal)
        Me.WageMonth = wageMonth
        Me.Lines = lines
        Me.OvertimeDifference = overtimeDifference
        Me.GrossDifference = grossDifference
        Me.EpfWages = epfWages
        Me.EpsWages = epsWages
        Me.EdliWages = edliWages
        Me.EmployeeEpf = employeeEpf
        Me.EmployerEpf = employerEpf
        Me.EmployerEps = employerEps
        Me.Edli = edli
        Me.Admin = admin
        Me.EsiEmployee = esiEmployee
        Me.EsiEmployer = esiEmployer
    End Sub

    Public ReadOnly Property WageMonth As Date
    Public ReadOnly Property Lines As IReadOnlyList(Of EarnedLine)
    Public ReadOnly Property OvertimeDifference As Decimal
    Public ReadOnly Property GrossDifference As Decimal
    Public ReadOnly Property EpfWages As Decimal
    Public ReadOnly Property EpsWages As Decimal
    Public ReadOnly Property EdliWages As Decimal
    Public ReadOnly Property EmployeeEpf As Decimal
    Public ReadOnly Property EmployerEpf As Decimal
    Public ReadOnly Property EmployerEps As Decimal
    Public ReadOnly Property Edli As Decimal
    Public ReadOnly Property Admin As Decimal
    Public ReadOnly Property EsiEmployee As Decimal
    Public ReadOnly Property EsiEmployer As Decimal
    ''' <summary>Employee deductions on this month's difference, before tax.</summary>
    Public ReadOnly Property EmployeeDeductions As Decimal
        Get
            Return EmployeeEpf + EsiEmployee
        End Get
    End Property
End Class
