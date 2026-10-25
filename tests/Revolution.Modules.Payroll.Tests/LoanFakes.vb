Imports Revolution.Modules.Payroll

Friend NotInheritable Class FakeLoanRepository
    Implements ILoanRepository

    Public ReadOnly Types As New List(Of LoanTypeItem) From {
        New LoanTypeItem(1, "Advance", InterestMethod.Flat, 12D, 12, True),
        New LoanTypeItem(2, "Old scheme", InterestMethod.Reducing, 0D, 6, False)}
    Public ReadOnly SavedTypes As New List(Of LoanTypeItem)()
    Public ReadOnly Created As New List(Of String)()
    Public LastSchedule As IReadOnlyList(Of LoanInstallmentPlan)
    Public LastMethod As InterestMethod
    Public LastRate As Decimal
    Public LastUserId As Integer?
    Public Cancelled As New List(Of Integer)()
    Public Loans As New List(Of LoanSummary)()
    Public Installments As New List(Of LoanInstallmentView)()
    Public FailWith As String

    Public Function ListTypes() As IReadOnlyList(Of LoanTypeItem) Implements ILoanRepository.ListTypes
        Return Types
    End Function

    Public Function SaveType(loanType As LoanTypeItem) As Integer Implements ILoanRepository.SaveType
        ThrowIfFailing()
        SavedTypes.Add(loanType)
        Return If(loanType.LoanTypeId, 77)
    End Function

    Public Function CreateLoan(employeeId As Integer, loanTypeId As Integer, sanctionDate As Date, principal As Decimal, method As InterestMethod,
                               annualInterestPct As Decimal, schedule As IReadOnlyList(Of LoanInstallmentPlan), userId As Integer?) As Integer Implements ILoanRepository.CreateLoan
        ThrowIfFailing()
        Created.Add($"{employeeId}:{loanTypeId}:{sanctionDate:yyyy-MM-dd}:{principal}")
        LastSchedule = schedule
        LastMethod = method
        LastRate = annualInterestPct
        LastUserId = userId
        Return 900
    End Function

    Public Sub CancelLoan(loanId As Integer) Implements ILoanRepository.CancelLoan
        ThrowIfFailing()
        Cancelled.Add(loanId)
    End Sub

    Public Function ListLoans(employeeId As Integer) As IReadOnlyList(Of LoanSummary) Implements ILoanRepository.ListLoans
        Return Loans
    End Function

    Public Function ListInstallments(loanId As Integer) As IReadOnlyList(Of LoanInstallmentView) Implements ILoanRepository.ListInstallments
        Return Installments
    End Function

    Private Sub ThrowIfFailing()
        If FailWith IsNot Nothing Then Throw New PayrollRuleException(FailWith, 50323, Nothing)
    End Sub
End Class
