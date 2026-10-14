''' <summary>One installment of a loan schedule, in whole rupees. DueMonthKey is YYYYMM.</summary>
Public NotInheritable Class LoanInstallmentPlan
    Public Sub New(installmentNo As Integer, dueMonthKey As Integer, principalPart As Decimal, interestPart As Decimal)
        Me.InstallmentNo = installmentNo
        Me.DueMonthKey = dueMonthKey
        Me.PrincipalPart = principalPart
        Me.InterestPart = interestPart
    End Sub

    Public ReadOnly Property InstallmentNo As Integer
    Public ReadOnly Property DueMonthKey As Integer
    Public ReadOnly Property PrincipalPart As Decimal
    Public ReadOnly Property InterestPart As Decimal

    Public ReadOnly Property Amount As Decimal
        Get
            Return PrincipalPart + InterestPart
        End Get
    End Property
End Class
