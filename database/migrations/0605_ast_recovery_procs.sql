/* 0605_ast_recovery_procs.sql
   Asset recovery procedures (Phase 9 step 0601a). CREATE OR ALTER keeps it idempotent. Each reads the company from SESSION_CONTEXT and filters on it (ADR 0007).
   Errors: 50002 no company; 53500 the values are not valid; 53505 asset not found; 53510 only a lost or damaged asset that was issued to an employee can be recovered;
   53511 the asset already has a recovery; 53512 recovery not found; 53513 the recovery is not open, so it cannot be cancelled. StatusID of a recovery: 1 Open, 2 Cancelled, 3 Recovered. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE ast.usp_AssetRecovery_Source
    @AssetID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM ast.Asset WHERE CompanyID = @CompanyID AND AssetID = @AssetID) THROW 53505, N'Asset not found.', 1;
    SELECT a.AssetID, a.TagNo, t.TypeName, a.PurchaseDate, a.PurchaseValue, t.UsefulLifeMonths, a.StatusID,
           i.IssueID, i.EmployeeID, e.EmpCode, e.FullName
    FROM ast.Asset a
    JOIN ast.AssetType t ON t.CompanyID = a.CompanyID AND t.AssetTypeID = a.AssetTypeID
    OUTER APPLY (SELECT TOP 1 x.IssueID, x.EmployeeID FROM ast.AssetIssue x
                 WHERE x.CompanyID = a.CompanyID AND x.AssetID = a.AssetID ORDER BY x.IssueDate DESC, x.IssueID DESC) i
    LEFT JOIN mst.Employee e ON e.CompanyID = a.CompanyID AND e.EmployeeID = i.EmployeeID
    WHERE a.CompanyID = @CompanyID AND a.AssetID = @AssetID;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_AssetRecovery_Add
    @AssetID INT, @BasisDate DATE, @DepreciatedValue DECIMAL(12,2), @Amount DECIMAL(12,2), @Reason NVARCHAR(200) = NULL, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @BasisDate IS NULL OR @DepreciatedValue IS NULL OR @DepreciatedValue < 0 OR @Amount IS NULL OR @Amount <= 0 THROW 53500, N'The values are not valid.', 1;
    IF @Amount <> @DepreciatedValue AND LEN(LTRIM(RTRIM(ISNULL(@Reason, N'')))) = 0 THROW 53500, N'The values are not valid.', 1;
    DECLARE @Status TINYINT, @PurchaseValue DECIMAL(12,2), @IssueID INT, @EmployeeID INT, @RecoveryID INT;
    BEGIN TRANSACTION;
        SELECT @Status = StatusID, @PurchaseValue = PurchaseValue FROM ast.Asset WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND AssetID = @AssetID;
        IF @Status IS NULL THROW 53505, N'Asset not found.', 1;
        SELECT TOP 1 @IssueID = IssueID, @EmployeeID = EmployeeID FROM ast.AssetIssue WHERE CompanyID = @CompanyID AND AssetID = @AssetID ORDER BY IssueDate DESC, IssueID DESC;
        IF @Status NOT IN (3, 4) OR @IssueID IS NULL THROW 53510, N'Only a lost or damaged asset that was issued to an employee can be recovered.', 1;
        IF @Amount > @PurchaseValue THROW 53500, N'The values are not valid.', 1;
        IF EXISTS (SELECT 1 FROM ast.AssetRecovery WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND IssueID = @IssueID AND StatusID IN (1, 3))
            THROW 53511, N'The asset already has a recovery.', 1;
        INSERT ast.AssetRecovery (CompanyID, AssetID, IssueID, EmployeeID, BasisDate, DepreciatedValue, Amount, Reason, StatusID, CreatedByUserID)
        VALUES (@CompanyID, @AssetID, @IssueID, @EmployeeID, @BasisDate, @DepreciatedValue, @Amount, NULLIF(LTRIM(RTRIM(ISNULL(@Reason, N''))), N''), 1, @UserID);
        SET @RecoveryID = CAST(SCOPE_IDENTITY() AS INT);
    COMMIT TRANSACTION;
    SELECT @RecoveryID AS RecoveryID;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_AssetRecovery_Cancel
    @RecoveryID INT, @Reason NVARCHAR(200), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF LEN(LTRIM(RTRIM(ISNULL(@Reason, N'')))) = 0 THROW 53500, N'The values are not valid.', 1;
    DECLARE @Status TINYINT;
    BEGIN TRANSACTION;
        SELECT @Status = StatusID FROM ast.AssetRecovery WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND RecoveryID = @RecoveryID;
        IF @Status IS NULL THROW 53512, N'Recovery not found.', 1;
        IF @Status <> 1 THROW 53513, N'The recovery is not open, so it cannot be cancelled.', 1;
        UPDATE ast.AssetRecovery SET StatusID = 2, CancelledUtc = SYSUTCDATETIME(), CancelReason = LTRIM(RTRIM(@Reason))
        WHERE CompanyID = @CompanyID AND RecoveryID = @RecoveryID;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_AssetRecovery_List
    @EmployeeID INT = NULL, @StatusID TINYINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT r.RecoveryID, r.AssetID, a.TagNo, t.TypeName, r.EmployeeID, e.EmpCode, e.FullName, r.BasisDate, r.DepreciatedValue, r.Amount,
           r.Reason, r.StatusID, r.AppliedRunID
    FROM ast.AssetRecovery r
    JOIN ast.Asset a ON a.CompanyID = r.CompanyID AND a.AssetID = r.AssetID
    JOIN ast.AssetType t ON t.CompanyID = a.CompanyID AND t.AssetTypeID = a.AssetTypeID
    JOIN mst.Employee e ON e.CompanyID = r.CompanyID AND e.EmployeeID = r.EmployeeID
    WHERE r.CompanyID = @CompanyID AND (@EmployeeID IS NULL OR r.EmployeeID = @EmployeeID) AND (@StatusID IS NULL OR r.StatusID = @StatusID)
    ORDER BY r.RecoveryID DESC;
END
GO
