Imports Revolution.Core

''' <summary>Checks loan type and sanction input before it reaches the schedule calculator and the database. Limits mirror the pay.Loan* columns and checks.</summary>
Public NotInheritable Class LoanValidator
    Public Const MaxNameLength As Integer = 100
    Public Const MaxInstallmentsLimit As Integer = 360
    Public Const MaxPrincipal As Decimal = 999999999999D

    Private Sub New()
    End Sub

    Public Shared Function ValidateType(loanType As LoanTypeItem) As OperationResult
        If loanType Is Nothing Then Return OperationResult.Fail("Loan type is missing.")
        If String.IsNullOrWhiteSpace(loanType.Name) Then Return OperationResult.Fail("Loan type name is required.")
        If loanType.Name.Trim().Length > MaxNameLength Then Return OperationResult.Fail("Loan type name is too long (100 characters at most).")
        If loanType.Method <> InterestMethod.Flat AndAlso loanType.Method <> InterestMethod.Reducing Then Return OperationResult.Fail("Pick flat or reducing balance interest.")
        If loanType.AnnualInterestPct < 0D OrElse loanType.AnnualInterestPct > 100D Then Return OperationResult.Fail("Yearly interest must be between 0 and 100 percent.")
        If loanType.MaxInstallments < 1 OrElse loanType.MaxInstallments > MaxInstallmentsLimit Then Return OperationResult.Fail("Most installments must be between 1 and 360.")
        If loanType.LoanTypeId.HasValue AndAlso loanType.LoanTypeId.Value <= 0 Then Return OperationResult.Fail("Loan type is not valid.")
        Return OperationResult.Ok()
    End Function

    ''' <summary>Employee, sanction date and the loan figures. The first due month must not be before the sanction month.</summary>
    Public Shared Function ValidateSanction(employeeId As Integer, sanctionDate As Date, principal As Decimal, installments As Integer,
                                            firstDueMonthKey As Integer, loanType As LoanTypeItem) As OperationResult
        If employeeId <= 0 Then Return OperationResult.Fail("Pick an employee.")
        If sanctionDate.Year < 2000 Then Return OperationResult.Fail("Sanction date is not valid.")
        Dim terms As OperationResult = ValidateTerms(principal, installments, firstDueMonthKey, loanType)
        If Not terms.Succeeded Then Return terms
        If firstDueMonthKey < sanctionDate.Year * 100 + sanctionDate.Month Then Return OperationResult.Fail("First due month is before the sanction month.")
        Return OperationResult.Ok()
    End Function

    ''' <summary>The loan figures alone: type active, whole principal, installments within the type's limit, a real first due month (YYYYMM).</summary>
    Public Shared Function ValidateTerms(principal As Decimal, installments As Integer, firstDueMonthKey As Integer, loanType As LoanTypeItem) As OperationResult
        If loanType Is Nothing Then Return OperationResult.Fail("Loan type not found.")
        If Not loanType.IsActive Then Return OperationResult.Fail("This loan type is not active.")
        If principal <= 0D Then Return OperationResult.Fail("Principal must be more than zero.")
        If principal <> Decimal.Truncate(principal) Then Return OperationResult.Fail("Principal must be a whole rupee amount.")
        If principal > MaxPrincipal Then Return OperationResult.Fail("Principal is too large.")
        If installments < 1 Then Return OperationResult.Fail("Installments must be at least 1.")
        If installments > loanType.MaxInstallments Then Return OperationResult.Fail($"This loan type allows at most {loanType.MaxInstallments} installments.")
        Dim month As Integer = firstDueMonthKey Mod 100
        If firstDueMonthKey < 200001 OrElse firstDueMonthKey > 999912 OrElse month < 1 OrElse month > 12 Then Return OperationResult.Fail("First due month is not valid.")
        Return OperationResult.Ok()
    End Function
End Class
