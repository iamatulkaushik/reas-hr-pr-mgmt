/* 0608_ast_expense_tables.sql
   Phase 9 step 0602: employee expense claims (Platinum plan, PlanFeature.AssetsAndExpenses, ModuleKey.Expenses). Brave lane 0600-0699. Idempotent. Never edit after merge.
   Owner decisions, 2026-11-11: claims are entered by the Operator and verified and approved by the Owner only; the Associate has no access (0034). Limits are the Owner's. A claim is paid separately in cash or by bank
   (no payroll line); the payment record is on the claim. Bills are noted (number, date, vendor, note); scanned files are not stored. Tenant policy is 0609, procedures are 0610.
   ast.ExpenseCategory: a kind of expense (travel, food, fuel ...) with an optional limit per claim and an optional limit per employee per calendar month. Rows are never deleted: a retired category is set inactive.
   ast.ExpenseClaim: StatusID 1 Submitted, 2 Approved, 3 Rejected, 4 Paid, 5 Cancelled. A bill (vendor and bill number) can be claimed once while the claim is Submitted, Approved or Paid. Rows are never deleted.
   Errors for expenses: 53520 to 53529 (0610). */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'ast.ExpenseCategory', N'U') IS NULL
BEGIN
    CREATE TABLE ast.ExpenseCategory
    (
        CategoryID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExpenseCategory PRIMARY KEY,
        CompanyID       INT           NOT NULL CONSTRAINT FK_ExpenseCategory_Company REFERENCES mst.Company (CompanyID),
        CategoryName    NVARCHAR(60)  NOT NULL,
        PerClaimLimit   DECIMAL(12,2) NULL CONSTRAINT CK_ExpenseCategory_Claim CHECK (PerClaimLimit IS NULL OR PerClaimLimit > 0),
        PerMonthLimit   DECIMAL(12,2) NULL CONSTRAINT CK_ExpenseCategory_Month CHECK (PerMonthLimit IS NULL OR PerMonthLimit > 0),
        IsActive        BIT           NOT NULL CONSTRAINT DF_ExpenseCategory_IsActive DEFAULT 1,
        CreatedUtc      DATETIME2(0)  NOT NULL CONSTRAINT DF_ExpenseCategory_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CreatedByUserID INT           NULL,
        CONSTRAINT CK_ExpenseCategory_Limits CHECK (PerClaimLimit IS NULL OR PerMonthLimit IS NULL OR PerMonthLimit >= PerClaimLimit),
        CONSTRAINT UQ_ExpenseCategory_Name UNIQUE (CompanyID, CategoryName),
        CONSTRAINT UQ_ExpenseCategory_Company UNIQUE (CompanyID, CategoryID)
    );
END
GO
IF OBJECT_ID(N'ast.ExpenseClaim', N'U') IS NULL
BEGIN
    CREATE TABLE ast.ExpenseClaim
    (
        ClaimID          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExpenseClaim PRIMARY KEY,
        CompanyID        INT           NOT NULL CONSTRAINT FK_ExpenseClaim_Company REFERENCES mst.Company (CompanyID),
        EmployeeID       INT           NOT NULL,
        CategoryID       INT           NOT NULL,
        ExpenseDate      DATE          NOT NULL,
        Amount           DECIMAL(12,2) NOT NULL CONSTRAINT CK_ExpenseClaim_Amount CHECK (Amount > 0),
        BillNo           NVARCHAR(40)  NULL,
        BillDate         DATE          NULL,
        Vendor           NVARCHAR(80)  NULL,
        Note             NVARCHAR(200) NULL,
        StatusID         TINYINT       NOT NULL CONSTRAINT DF_ExpenseClaim_Status DEFAULT 1 CONSTRAINT CK_ExpenseClaim_Status CHECK (StatusID BETWEEN 1 AND 5),
        EnteredByUserID  INT           NULL,
        EnteredUtc       DATETIME2(0)  NOT NULL CONSTRAINT DF_ExpenseClaim_EnteredUtc DEFAULT SYSUTCDATETIME(),
        DecidedByUserID  INT           NULL,
        DecidedUtc       DATETIME2(0)  NULL,
        DecisionReason   NVARCHAR(200) NULL,
        PaidDate         DATE          NULL,
        PaymentMode      TINYINT       NULL CONSTRAINT CK_ExpenseClaim_Mode CHECK (PaymentMode IN (1, 2)),
        PaymentRef       NVARCHAR(60)  NULL,
        PaidByUserID     INT           NULL,
        CancelledUtc     DATETIME2(0)  NULL,
        CancelReason     NVARCHAR(200) NULL,
        CONSTRAINT FK_ExpenseClaim_Category FOREIGN KEY (CompanyID, CategoryID) REFERENCES ast.ExpenseCategory (CompanyID, CategoryID),
        CONSTRAINT FK_ExpenseClaim_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID),
        CONSTRAINT CK_ExpenseClaim_Paid CHECK (StatusID <> 4 OR (PaidDate IS NOT NULL AND PaymentMode IS NOT NULL))
    );
    CREATE UNIQUE INDEX UX_ExpenseClaim_Bill ON ast.ExpenseClaim (CompanyID, Vendor, BillNo) WHERE BillNo IS NOT NULL AND StatusID IN (1, 2, 4);
    CREATE INDEX IX_ExpenseClaim_Employee ON ast.ExpenseClaim (CompanyID, EmployeeID, CategoryID, ExpenseDate);
    CREATE INDEX IX_ExpenseClaim_Status ON ast.ExpenseClaim (CompanyID, StatusID, ExpenseDate);
END
GO
