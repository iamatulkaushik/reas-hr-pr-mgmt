''' <summary>Storage for loan types, loans and their schedules (pay.usp_LoanType_*, usp_Loan_*, usp_LoanInstallment_List).</summary>
Public Interface ILoanRepository
    Function ListTypes() As IReadOnlyList(Of LoanTypeItem)
    ''' <summary>Adds the type when LoanTypeId is Nothing, otherwise changes it. Returns the id.</summary>
    Function SaveType(loanType As LoanTypeItem) As Integer
    ''' <summary>Stores a loan with its schedule. The database checks that the principal parts add up to the principal. Returns the loan id.</summary>
    Function CreateLoan(employeeId As Integer, loanTypeId As Integer, sanctionDate As Date, principal As Decimal, method As InterestMethod,
                        annualInterestPct As Decimal, schedule As IReadOnlyList(Of LoanInstallmentPlan), userId As Integer?) As Integer
    ''' <summary>Cancels a loan that has no recoveries. The database refuses otherwise.</summary>
    Sub CancelLoan(loanId As Integer)
    Function ListLoans(employeeId As Integer) As IReadOnlyList(Of LoanSummary)
    Function ListInstallments(loanId As Integer) As IReadOnlyList(Of LoanInstallmentView)
End Interface
