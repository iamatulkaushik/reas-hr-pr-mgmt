/* 0610_ast_expense_procs.sql
   Expense procedures (Phase 9 step 0602). CREATE OR ALTER keeps it idempotent. Each reads the company from SESSION_CONTEXT and filters on it (ADR 0007).
   Errors: 50002 no company; 53520 the values are not valid; 53521 the category name is already used; 53522 category not found or not active; 53523 employee not found; 53524 this bill is already claimed;
   53525 the claim is over the category's limit per claim; 53526 the claims of the month would go over the category's limit per month; 53527 claim not found; 53528 the claim cannot move from its current status.
   Claim StatusID: 1 Submitted, 2 Approved, 3 Rejected, 4 Paid, 5 Cancelled. PaymentMode: 1 Cash, 2 Bank. The monthly limit counts one employee's Submitted, Approved and Paid claims of the category in the calendar month of the expense date. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE ast.usp_ExpenseCategory_Save
    @CategoryID INT = NULL, @CategoryName NVARCHAR(60), @PerClaimLimit DECIMAL(12,2) = NULL, @PerMonthLimit DECIMAL(12,2) = NULL, @IsActive BIT = 1, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF LEN(LTRIM(RTRIM(ISNULL(@CategoryName, N'')))) = 0 OR @PerClaimLimit <= 0 OR @PerMonthLimit <= 0 OR (@PerClaimLimit IS NOT NULL AND @PerMonthLimit IS NOT NULL AND @PerMonthLimit < @PerClaimLimit)
        THROW 53520, N'The values are not valid.', 1;
    BEGIN TRANSACTION;
        IF EXISTS (SELECT 1 FROM ast.ExpenseCategory WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND CategoryName = LTRIM(RTRIM(@CategoryName)) AND (@CategoryID IS NULL OR CategoryID <> @CategoryID))
            THROW 53521, N'The category name is already used.', 1;
        IF @CategoryID IS NULL
        BEGIN
            INSERT ast.ExpenseCategory (CompanyID, CategoryName, PerClaimLimit, PerMonthLimit, IsActive, CreatedByUserID)
            VALUES (@CompanyID, LTRIM(RTRIM(@CategoryName)), @PerClaimLimit, @PerMonthLimit, @IsActive, @UserID);
            SET @CategoryID = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE ast.ExpenseCategory SET CategoryName = LTRIM(RTRIM(@CategoryName)), PerClaimLimit = @PerClaimLimit, PerMonthLimit = @PerMonthLimit, IsActive = @IsActive
            WHERE CompanyID = @CompanyID AND CategoryID = @CategoryID;
            IF @@ROWCOUNT = 0 THROW 53522, N'Category not found or not active.', 1;
        END
    COMMIT TRANSACTION;
    SELECT @CategoryID AS CategoryID;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_ExpenseCategory_List
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT c.CategoryID, c.CategoryName, c.PerClaimLimit, c.PerMonthLimit, c.IsActive
    FROM ast.ExpenseCategory c
    WHERE c.CompanyID = @CompanyID AND (@IncludeInactive = 1 OR c.IsActive = 1)
    ORDER BY c.CategoryName;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_ExpenseClaim_Add
    @EmployeeID INT, @CategoryID INT, @ExpenseDate DATE, @Amount DECIMAL(12,2), @BillNo NVARCHAR(40) = NULL, @BillDate DATE = NULL, @Vendor NVARCHAR(80) = NULL,
    @Note NVARCHAR(200) = NULL, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @ExpenseDate IS NULL OR @Amount IS NULL OR @Amount <= 0 OR (@BillNo IS NOT NULL AND LEN(LTRIM(RTRIM(@BillNo))) > 0 AND LEN(LTRIM(RTRIM(ISNULL(@Vendor, N'')))) = 0)
        THROW 53520, N'The values are not valid.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID) THROW 53523, N'Employee not found.', 1;
    DECLARE @PerClaim DECIMAL(12,2), @PerMonth DECIMAL(12,2), @Active BIT, @ClaimID INT, @Used DECIMAL(14,2), @Text NVARCHAR(200);
    DECLARE @Bill NVARCHAR(40) = NULLIF(LTRIM(RTRIM(ISNULL(@BillNo, N''))), N'');
    DECLARE @VendorText NVARCHAR(80) = NULLIF(LTRIM(RTRIM(ISNULL(@Vendor, N''))), N'');
    BEGIN TRANSACTION;
        SELECT @PerClaim = PerClaimLimit, @PerMonth = PerMonthLimit, @Active = IsActive FROM ast.ExpenseCategory WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND CategoryID = @CategoryID;
        IF @Active IS NULL OR @Active = 0 THROW 53522, N'Category not found or not active.', 1;
        IF @PerClaim IS NOT NULL AND @Amount > @PerClaim
        BEGIN
            SET @Text = CONCAT(N'The claim is over the limit of ', FORMAT(@PerClaim, N'N0'), N' rupees per claim for this category.');
            THROW 53525, @Text, 1;
        END
        IF @PerMonth IS NOT NULL
        BEGIN
            DECLARE @MonthStart DATE = DATEFROMPARTS(YEAR(@ExpenseDate), MONTH(@ExpenseDate), 1);
            SELECT @Used = ISNULL(SUM(Amount), 0) FROM ast.ExpenseClaim WITH (UPDLOCK, HOLDLOCK)
            WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND CategoryID = @CategoryID AND StatusID IN (1, 2, 4)
              AND ExpenseDate >= @MonthStart AND ExpenseDate < DATEADD(MONTH, 1, @MonthStart);
            IF @Used + @Amount > @PerMonth
            BEGIN
                SET @Text = CONCAT(N'The claims of the month would go over the limit of ', FORMAT(@PerMonth, N'N0'), N' rupees a month for this category.');
                THROW 53526, @Text, 1;
            END
        END
        IF @Bill IS NOT NULL AND EXISTS (SELECT 1 FROM ast.ExpenseClaim WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND Vendor = @VendorText AND BillNo = @Bill AND StatusID IN (1, 2, 4))
            THROW 53524, N'This bill is already claimed.', 1;
        INSERT ast.ExpenseClaim (CompanyID, EmployeeID, CategoryID, ExpenseDate, Amount, BillNo, BillDate, Vendor, Note, StatusID, EnteredByUserID)
        VALUES (@CompanyID, @EmployeeID, @CategoryID, @ExpenseDate, @Amount, @Bill, @BillDate, @VendorText, NULLIF(LTRIM(RTRIM(ISNULL(@Note, N''))), N''), 1, @UserID);
        SET @ClaimID = CAST(SCOPE_IDENTITY() AS INT);
    COMMIT TRANSACTION;
    SELECT @ClaimID AS ClaimID;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_ExpenseClaim_List
    @EmployeeID INT = NULL, @StatusID TINYINT = NULL, @FromDate DATE = NULL, @ToDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT c.ClaimID, c.EmployeeID, e.EmpCode, e.FullName, c.CategoryID, k.CategoryName, c.ExpenseDate, c.Amount, c.BillNo, c.BillDate, c.Vendor, c.Note,
           c.StatusID, c.DecisionReason, c.PaidDate, c.PaymentMode, c.PaymentRef
    FROM ast.ExpenseClaim c
    JOIN ast.ExpenseCategory k ON k.CompanyID = c.CompanyID AND k.CategoryID = c.CategoryID
    JOIN mst.Employee e ON e.CompanyID = c.CompanyID AND e.EmployeeID = c.EmployeeID
    WHERE c.CompanyID = @CompanyID AND (@EmployeeID IS NULL OR c.EmployeeID = @EmployeeID) AND (@StatusID IS NULL OR c.StatusID = @StatusID)
      AND (@FromDate IS NULL OR c.ExpenseDate >= @FromDate) AND (@ToDate IS NULL OR c.ExpenseDate <= @ToDate)
    ORDER BY c.ClaimID DESC;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_ExpenseClaim_Decide
    @ClaimID INT, @Approve BIT, @Reason NVARCHAR(200) = NULL, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @Approve = 0 AND LEN(LTRIM(RTRIM(ISNULL(@Reason, N'')))) = 0 THROW 53520, N'The values are not valid.', 1;
    DECLARE @Status TINYINT;
    BEGIN TRANSACTION;
        SELECT @Status = StatusID FROM ast.ExpenseClaim WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND ClaimID = @ClaimID;
        IF @Status IS NULL THROW 53527, N'Claim not found.', 1;
        IF @Status <> 1 THROW 53528, N'The claim cannot move from its current status.', 1;
        UPDATE ast.ExpenseClaim SET StatusID = CASE WHEN @Approve = 1 THEN 2 ELSE 3 END, DecidedByUserID = @UserID, DecidedUtc = SYSUTCDATETIME(),
               DecisionReason = NULLIF(LTRIM(RTRIM(ISNULL(@Reason, N''))), N'')
        WHERE CompanyID = @CompanyID AND ClaimID = @ClaimID;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_ExpenseClaim_Pay
    @ClaimID INT, @PaidDate DATE, @PaymentMode TINYINT, @PaymentRef NVARCHAR(60) = NULL, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @PaidDate IS NULL OR @PaymentMode NOT IN (1, 2) OR (@PaymentMode = 2 AND LEN(LTRIM(RTRIM(ISNULL(@PaymentRef, N'')))) = 0) THROW 53520, N'The values are not valid.', 1;
    DECLARE @Status TINYINT, @ExpenseDate DATE;
    BEGIN TRANSACTION;
        SELECT @Status = StatusID, @ExpenseDate = ExpenseDate FROM ast.ExpenseClaim WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND ClaimID = @ClaimID;
        IF @Status IS NULL THROW 53527, N'Claim not found.', 1;
        IF @Status <> 2 THROW 53528, N'The claim cannot move from its current status.', 1;
        IF @PaidDate < @ExpenseDate THROW 53520, N'The values are not valid.', 1;
        UPDATE ast.ExpenseClaim SET StatusID = 4, PaidDate = @PaidDate, PaymentMode = @PaymentMode, PaymentRef = NULLIF(LTRIM(RTRIM(ISNULL(@PaymentRef, N''))), N''), PaidByUserID = @UserID
        WHERE CompanyID = @CompanyID AND ClaimID = @ClaimID;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_ExpenseClaim_Cancel
    @ClaimID INT, @Reason NVARCHAR(200), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF LEN(LTRIM(RTRIM(ISNULL(@Reason, N'')))) = 0 THROW 53520, N'The values are not valid.', 1;
    DECLARE @Status TINYINT;
    BEGIN TRANSACTION;
        SELECT @Status = StatusID FROM ast.ExpenseClaim WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND ClaimID = @ClaimID;
        IF @Status IS NULL THROW 53527, N'Claim not found.', 1;
        IF @Status NOT IN (1, 2) THROW 53528, N'The claim cannot move from its current status.', 1;
        UPDATE ast.ExpenseClaim SET StatusID = 5, CancelledUtc = SYSUTCDATETIME(), CancelReason = LTRIM(RTRIM(@Reason))
        WHERE CompanyID = @CompanyID AND ClaimID = @ClaimID;
    COMMIT TRANSACTION;
END
GO
