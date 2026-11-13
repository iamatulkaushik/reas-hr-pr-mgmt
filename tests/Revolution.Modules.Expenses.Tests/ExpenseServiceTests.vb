Imports Revolution.Core
Imports Revolution.Modules.Expenses
Imports Xunit

Public Class ExpenseServiceTests
    Private ReadOnly _repo As New FakeExpenseRepository()
    Private ReadOnly _perms As New FakeExpensePermissions()
    Private ReadOnly _plan As New FakeExpenseEntitlements()
    Private ReadOnly _audit As New FakeExpenseAudit()
    Private ReadOnly _service As ExpenseService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Create)
        _perms.Allowed.Add(PermissionAction.Edit)
        _perms.Allowed.Add(PermissionAction.Approve)
        _service = New ExpenseService(_repo, _perms, _plan, _audit, New FakeExpenseSession(), Function() New Date(2026, 11, 11))
    End Sub

    Private Shared Function NewClaim() As ExpenseClaimWrite
        Return New ExpenseClaimWrite With {.EmployeeId = 9, .CategoryId = 2, .ExpenseDate = New Date(2026, 11, 5), .Amount = 1250.5D, .BillNo = "INV/889", .BillDate = New Date(2026, 11, 5), .Vendor = "City Taxi", .Note = "Client visit"}
    End Function

    <Fact>
    Public Sub PlanWithoutExpenses_RefusesEverything()
        _plan.ExpensesEnabled = False
        Assert.False(_service.ListCategories(False).Succeeded)
        Assert.False(_service.ListClaims(Nothing, Nothing, Nothing, Nothing).Succeeded)
        Assert.False(_service.AddClaim(NewClaim()).Succeeded)
        Assert.Empty(_repo.AddedClaims)
    End Sub

    <Fact>
    Public Sub WithoutAnyRight_RefusesEverything()
        _perms.Allowed.Clear()
        Assert.False(_service.ListClaims(Nothing, Nothing, Nothing, Nothing).Succeeded)
        Assert.False(_service.AddClaim(NewClaim()).Succeeded)
        Assert.False(_service.SaveCategory(New ExpenseCategoryWrite With {.Name = "Travel"}).Succeeded)
        Assert.False(_service.Decide(1, True, "").Succeeded)
        Assert.False(_service.Pay(1, New Date(2026, 11, 10), ExpensePaymentMode.Cash, "").Succeeded)
        Assert.False(_service.Cancel(1, "mistake").Succeeded)
    End Sub

    <Fact>
    Public Sub Operator_CanEnterButNotDecidePayOrCancel()
        _perms.Allowed.Remove(PermissionAction.Edit)
        _perms.Allowed.Remove(PermissionAction.Approve)
        Assert.True(_service.AddClaim(NewClaim()).Succeeded)
        Assert.False(_service.Decide(1, True, "").Succeeded)
        Assert.False(_service.Pay(1, New Date(2026, 11, 10), ExpensePaymentMode.Cash, "").Succeeded)
        Assert.False(_service.Cancel(1, "mistake").Succeeded)
        Assert.Empty(_repo.Decisions)
    End Sub

    <Fact>
    Public Sub SaveCategory_Add_NeedsCreate_ChangeNeedsEdit()
        Assert.True(_service.SaveCategory(New ExpenseCategoryWrite With {.Name = "  Travel ", .PerClaimLimit = 2000D, .PerMonthLimit = 5000D}).Succeeded)
        Assert.Equal("Travel", _repo.SavedCategories.Single().Name)
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.SaveCategory(New ExpenseCategoryWrite With {.CategoryId = 3, .Name = "Travel"}).Succeeded)
    End Sub

    <Theory>
    <InlineData("", 100, 200)>
    <InlineData("Travel", 0, 200)>
    <InlineData("Travel", 100, -5)>
    <InlineData("Travel", 500, 400)>
    <InlineData("Travel", 10000000, 20000000)>
    Public Sub SaveCategory_BadValues_AreRefused(name As String, perClaim As Integer, perMonth As Integer)
        Assert.False(_service.SaveCategory(New ExpenseCategoryWrite With {.Name = name, .PerClaimLimit = perClaim, .PerMonthLimit = perMonth}).Succeeded)
        Assert.Empty(_repo.SavedCategories)
    End Sub

    <Fact>
    Public Sub SaveCategory_NoLimits_IsAllowed()
        Assert.True(_service.SaveCategory(New ExpenseCategoryWrite With {.Name = "Food"}).Succeeded)
        Assert.Null(_repo.SavedCategories.Single().PerClaimLimit)
    End Sub

    <Fact>
    Public Sub AddClaim_Valid_IsSavedTrimmedAndRounded()
        Dim claim = NewClaim()
        claim.Vendor = "  City Taxi "
        claim.Amount = 100.005D
        Assert.True(_service.AddClaim(claim).Succeeded)
        Dim saved = _repo.AddedClaims.Single()
        Assert.Equal("City Taxi", saved.Vendor)
        Assert.Equal(100.01D, saved.Amount)
    End Sub

    <Fact>
    Public Sub AddClaim_AuditHoldsNoAmountAndNoBill()
        Assert.True(_service.AddClaim(NewClaim()).Succeeded)
        Dim entry = _audit.Entries.Single()
        Assert.DoesNotContain("1250", entry.Details)
        Assert.DoesNotContain("INV/889", entry.Details)
    End Sub

    <Fact>
    Public Sub AddClaim_BillNeedsAVendor()
        Dim claim = NewClaim()
        claim.Vendor = ""
        Assert.False(_service.AddClaim(claim).Succeeded)
    End Sub

    <Fact>
    Public Sub AddClaim_NoBill_IsAllowed()
        Dim claim = NewClaim()
        claim.BillNo = ""
        claim.Vendor = ""
        claim.BillDate = Nothing
        Assert.True(_service.AddClaim(claim).Succeeded)
    End Sub

    <Theory>
    <InlineData(0, 2, 100)>
    <InlineData(9, 0, 100)>
    <InlineData(9, 2, 0)>
    <InlineData(9, 2, 10000000)>
    Public Sub AddClaim_BadValues_AreRefused(employeeId As Integer, categoryId As Integer, amount As Integer)
        Dim claim = NewClaim()
        claim.EmployeeId = employeeId
        claim.CategoryId = categoryId
        claim.Amount = amount
        Assert.False(_service.AddClaim(claim).Succeeded)
        Assert.Empty(_repo.AddedClaims)
    End Sub

    <Fact>
    Public Sub AddClaim_FutureDates_AreRefused()
        Dim claim = NewClaim()
        claim.ExpenseDate = New Date(2026, 11, 12)
        Assert.False(_service.AddClaim(claim).Succeeded)
        claim = NewClaim()
        claim.BillDate = New Date(2026, 11, 12)
        Assert.False(_service.AddClaim(claim).Succeeded)
    End Sub

    <Fact>
    Public Sub AddClaim_BadBillNumber_IsRefused()
        Dim claim = NewClaim()
        claim.BillNo = "bad<>bill"
        Assert.False(_service.AddClaim(claim).Succeeded)
    End Sub

    <Theory>
    <InlineData("The claim is over the limit of 2,000 rupees per claim for this category.")>
    <InlineData("The claims of the month would go over the limit of 5,000 rupees a month for this category.")>
    <InlineData("This bill is already claimed.")>
    Public Sub AddClaim_DatabaseRule_ComesBackAsAFailedResult(message As String)
        _repo.FailWith = message
        Dim r = _service.AddClaim(NewClaim())
        Assert.False(r.Succeeded)
        Assert.Equal(message, r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Decide_Approve_NeedsNoReason()
        Assert.True(_service.Decide(5, True, Nothing).Succeeded)
        Assert.True(_repo.Decisions.Single().Item2)
    End Sub

    <Fact>
    Public Sub Decide_Reject_NeedsAReason()
        Assert.False(_service.Decide(5, False, "").Succeeded)
        Assert.False(_service.Decide(5, False, "no").Succeeded)
        Assert.True(_service.Decide(5, False, "  Not a business expense ").Succeeded)
        Assert.Equal("Not a business expense", _repo.Decisions.Single().Item3)
    End Sub

    <Fact>
    Public Sub Decide_WrongStatus_ComesBackAsAFailedResult()
        _repo.FailWith = "The claim cannot move from its current status."
        Assert.False(_service.Decide(5, True, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub Pay_Cash_NeedsNoReference()
        Assert.True(_service.Pay(5, New Date(2026, 11, 10), ExpensePaymentMode.Cash, "").Succeeded)
        Assert.Equal(ExpensePaymentMode.Cash, _repo.Payments.Single().Item3)
    End Sub

    <Fact>
    Public Sub Pay_Bank_NeedsAReference()
        Assert.False(_service.Pay(5, New Date(2026, 11, 10), ExpensePaymentMode.Bank, "").Succeeded)
        Assert.True(_service.Pay(5, New Date(2026, 11, 10), ExpensePaymentMode.Bank, " UTR778899 ").Succeeded)
        Assert.Equal("UTR778899", _repo.Payments.Single().Item4)
    End Sub

    <Fact>
    Public Sub Pay_FutureDate_IsRefused()
        Assert.False(_service.Pay(5, New Date(2026, 11, 12), ExpensePaymentMode.Cash, "").Succeeded)
    End Sub

    <Fact>
    Public Sub Cancel_NeedsAReason_AndEdit()
        Assert.False(_service.Cancel(5, "").Succeeded)
        Assert.True(_service.Cancel(5, "Entered twice").Succeeded)
        Assert.Equal("Entered twice", _repo.Cancelled.Single().Item2)
    End Sub

    <Fact>
    Public Sub ListClaims_FromAfterTo_IsRefused()
        Assert.False(_service.ListClaims(Nothing, Nothing, New Date(2026, 11, 10), New Date(2026, 11, 1)).Succeeded)
    End Sub

    <Fact>
    Public Sub ListClaims_FiltersByEmployeeAndStatus()
        _repo.Claims.Add(New ExpenseClaimItem(1, 9, "E9", "Asha", 2, "Travel", New Date(2026, 11, 5), 100D, String.Empty, Nothing, String.Empty, String.Empty, ExpenseClaimStatus.Submitted, String.Empty, Nothing, Nothing, String.Empty))
        _repo.Claims.Add(New ExpenseClaimItem(2, 10, "E10", "Ravi", 2, "Travel", New Date(2026, 11, 6), 200D, String.Empty, Nothing, String.Empty, String.Empty, ExpenseClaimStatus.Paid, String.Empty, New Date(2026, 11, 8), ExpensePaymentMode.Cash, String.Empty))
        Assert.Equal(2, _service.ListClaims(Nothing, Nothing, Nothing, Nothing).Value.Count)
        Assert.Equal(1, _service.ListClaims(9, Nothing, Nothing, Nothing).Value.Count)
        Assert.Equal(1, _service.ListClaims(Nothing, ExpenseClaimStatus.Paid, Nothing, Nothing).Value.Count)
    End Sub
End Class
