''' <summary>An installment not fully recovered. Outstanding is what is still due on it.</summary>
Public NotInheritable Class OpenInstallment
    Public Sub New(loanId As Integer, installmentNo As Integer, dueMonthKey As Integer, outstanding As Decimal)
        Me.LoanID = loanId
        Me.InstallmentNo = installmentNo
        Me.DueMonthKey = dueMonthKey
        Me.Outstanding = outstanding
    End Sub

    Public ReadOnly Property LoanID As Integer
    Public ReadOnly Property InstallmentNo As Integer
    Public ReadOnly Property DueMonthKey As Integer
    Public ReadOnly Property Outstanding As Decimal
End Class
