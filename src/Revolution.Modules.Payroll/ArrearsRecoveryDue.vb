''' <summary>One open overpaid arrears claim: the amount to take back from the employee's pay in a regular run (minus the claim's net before tax, whole rupees, above zero).</summary>
Public NotInheritable Class ArrearsRecoveryDue
    Public Sub New(claimId As Integer, employeeId As Integer, empCode As String, amount As Decimal)
        If amount <= 0D Then Throw New ArgumentOutOfRangeException(NameOf(amount), "The amount to recover must be above zero.")
        Me.ClaimId = claimId
        Me.EmployeeId = employeeId
        Me.EmpCode = If(empCode, String.Empty)
        Me.Amount = amount
    End Sub

    Public ReadOnly Property ClaimId As Integer
    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property Amount As Decimal
End Class
