''' <summary>Storage for expense categories and claims (ADR 0001). A rule the database enforces comes back as ExpensesRuleException.</summary>
Public Interface IExpenseRepository
    Function ListCategories(includeInactive As Boolean) As IReadOnlyList(Of ExpenseCategoryItem)

    ''' <summary>Adds or changes a category. Returns its id.</summary>
    Function SaveCategory(category As ExpenseCategoryWrite, userId As Integer?) As Integer

    ''' <summary>Enters a claim, Submitted. The database checks the category's limits and the bill. Returns the claim id.</summary>
    Function AddClaim(claim As ExpenseClaimWrite, userId As Integer?) As Integer

    Function ListClaims(employeeId As Integer?, status As ExpenseClaimStatus?, fromDate As Date?, toDate As Date?) As IReadOnlyList(Of ExpenseClaimItem)

    ''' <summary>Approves or rejects a Submitted claim. A rejection needs a reason.</summary>
    Sub Decide(claimId As Integer, approve As Boolean, reason As String, userId As Integer?)

    ''' <summary>Records the payment of an Approved claim, in cash or by bank.</summary>
    Sub Pay(claimId As Integer, paidDate As Date, mode As ExpensePaymentMode, reference As String, userId As Integer?)

    ''' <summary>Cancels a Submitted or Approved claim. A Paid claim cannot be cancelled.</summary>
    Sub Cancel(claimId As Integer, reason As String, userId As Integer?)
End Interface
