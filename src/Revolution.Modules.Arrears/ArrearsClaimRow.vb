''' <summary>One line of the arrears list: a claim with its totals. Signed whole rupees.</summary>
Public NotInheritable Class ArrearsClaimRow
    Public Sub New(claimId As Integer, employeeId As Integer, empCode As String, fullName As String, fromMonth As Date, toMonth As Date, reason As String,
                   status As ArrearsClaimStatus, grossTotal As Decimal, employeeDeductions As Decimal, employerContributions As Decimal, netBeforeTax As Decimal, createdUtc As Date,
                   Optional appliedRunId As Integer? = Nothing)
        Me.ClaimId = claimId
        Me.EmployeeId = employeeId
        Me.EmpCode = empCode
        Me.FullName = fullName
        Me.FromMonth = fromMonth
        Me.ToMonth = toMonth
        Me.Reason = reason
        Me.Status = status
        Me.GrossTotal = grossTotal
        Me.EmployeeDeductions = employeeDeductions
        Me.EmployerContributions = employerContributions
        Me.NetBeforeTax = netBeforeTax
        Me.CreatedUtc = createdUtc
        Me.AppliedRunId = appliedRunId
    End Sub

    Public ReadOnly Property ClaimId As Integer
    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property FullName As String
    Public ReadOnly Property FromMonth As Date
    Public ReadOnly Property ToMonth As Date
    Public ReadOnly Property Reason As String
    Public ReadOnly Property Status As ArrearsClaimStatus
    Public ReadOnly Property GrossTotal As Decimal
    Public ReadOnly Property EmployeeDeductions As Decimal
    Public ReadOnly Property EmployerContributions As Decimal
    Public ReadOnly Property NetBeforeTax As Decimal
    Public ReadOnly Property CreatedUtc As Date
    ''' <summary>The arrears run that paid the claim. Nothing until it is Paid.</summary>
    Public ReadOnly Property AppliedRunId As Integer?
End Class
