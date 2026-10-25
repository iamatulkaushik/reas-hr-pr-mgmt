''' <summary>A loan type a company defines (pay.LoanType). LoanTypeId is Nothing for a type not saved yet. Rate 0 means no interest.</summary>
Public NotInheritable Class LoanTypeItem
    Public Sub New(loanTypeId As Integer?, name As String, method As InterestMethod, annualInterestPct As Decimal, maxInstallments As Integer, isActive As Boolean)
        Me.LoanTypeId = loanTypeId
        Me.Name = name
        Me.Method = method
        Me.AnnualInterestPct = annualInterestPct
        Me.MaxInstallments = maxInstallments
        Me.IsActive = isActive
    End Sub

    Public ReadOnly Property LoanTypeId As Integer?
    Public ReadOnly Property Name As String
    Public ReadOnly Property Method As InterestMethod
    Public ReadOnly Property AnnualInterestPct As Decimal
    Public ReadOnly Property MaxInstallments As Integer
    Public ReadOnly Property IsActive As Boolean
End Class
