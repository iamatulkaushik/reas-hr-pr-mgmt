''' <summary>A sanctioned loan with what is still to be recovered (all installments less all recoveries).</summary>
Public NotInheritable Class LoanSummary
    Public Sub New(loanId As Integer, loanTypeId As Integer, sanctionDate As Date, principal As Decimal, method As InterestMethod,
                   annualInterestPct As Decimal, installmentCount As Integer, status As LoanStatus, outstanding As Decimal)
        Me.LoanId = loanId
        Me.LoanTypeId = loanTypeId
        Me.SanctionDate = sanctionDate
        Me.Principal = principal
        Me.Method = method
        Me.AnnualInterestPct = annualInterestPct
        Me.InstallmentCount = installmentCount
        Me.Status = status
        Me.Outstanding = outstanding
    End Sub

    Public ReadOnly Property LoanId As Integer
    Public ReadOnly Property LoanTypeId As Integer
    Public ReadOnly Property SanctionDate As Date
    Public ReadOnly Property Principal As Decimal
    Public ReadOnly Property Method As InterestMethod
    Public ReadOnly Property AnnualInterestPct As Decimal
    Public ReadOnly Property InstallmentCount As Integer
    Public ReadOnly Property Status As LoanStatus
    Public ReadOnly Property Outstanding As Decimal
End Class
