''' <summary>One installment of a sanctioned loan with the amount recovered so far.</summary>
Public NotInheritable Class LoanInstallmentView
    Public Sub New(installmentNo As Integer, dueMonthKey As Integer, principalPart As Decimal, interestPart As Decimal, amount As Decimal, recovered As Decimal)
        Me.InstallmentNo = installmentNo
        Me.DueMonthKey = dueMonthKey
        Me.PrincipalPart = principalPart
        Me.InterestPart = interestPart
        Me.Amount = amount
        Me.Recovered = recovered
    End Sub

    Public ReadOnly Property InstallmentNo As Integer
    Public ReadOnly Property DueMonthKey As Integer
    Public ReadOnly Property PrincipalPart As Decimal
    Public ReadOnly Property InterestPart As Decimal
    Public ReadOnly Property Amount As Decimal
    Public ReadOnly Property Recovered As Decimal
    Public ReadOnly Property Outstanding As Decimal
        Get
            Return Amount - Recovered
        End Get
    End Property
End Class
