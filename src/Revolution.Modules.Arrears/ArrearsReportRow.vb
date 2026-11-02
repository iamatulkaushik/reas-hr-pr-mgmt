''' <summary>One wage month of one open claim, for the arrears report.</summary>
Public NotInheritable Class ArrearsReportRow
    Public Sub New(claimId As Integer, employeeId As Integer, empCode As String, fullName As String, wageMonth As Date, grossDifference As Decimal,
                   employeeEpf As Decimal, esiEmployee As Decimal, employerTotal As Decimal)
        Me.ClaimId = claimId
        Me.EmployeeId = employeeId
        Me.EmpCode = empCode
        Me.FullName = fullName
        Me.WageMonth = wageMonth
        Me.GrossDifference = grossDifference
        Me.EmployeeEpf = employeeEpf
        Me.EsiEmployee = esiEmployee
        Me.EmployerTotal = employerTotal
    End Sub

    Public ReadOnly Property ClaimId As Integer
    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property FullName As String
    Public ReadOnly Property WageMonth As Date
    Public ReadOnly Property GrossDifference As Decimal
    Public ReadOnly Property EmployeeEpf As Decimal
    Public ReadOnly Property EsiEmployee As Decimal
    Public ReadOnly Property EmployerTotal As Decimal
    Public ReadOnly Property EmployeeDeductions As Decimal
        Get
            Return EmployeeEpf + EsiEmployee
        End Get
    End Property
End Class
