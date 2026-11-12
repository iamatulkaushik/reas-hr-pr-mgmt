/* 0607_ast_recovery_payroll_procs.sql
   How asset recoveries reach pay (Phase 9 step 0601b). CREATE OR ALTER keeps it idempotent. Each reads the company from SESSION_CONTEXT and filters on it (ADR 0007).
   Regular run (the same way overpaid arrears work, 0337): RunDue lists the open recoveries and the ones this run already holds; RunReserve holds them, all or none; RunRelease puts them back
   until the run is approved, locked or paid. Full and final: FnfDue lists an employee's open recoveries and the ones this settlement holds; FnfUncleared lists the tags of assets the employee still holds
   that have no active recovery and are not written off (these block the settlement until the Owner clears them); FnfReserve holds the open recoveries for a settlement when their total equals the total
   on its lines; FnfRelease puts them back when the settlement is reopened.
   Errors: 50002 no company; 53514 the regular run is not found or can no longer take recoveries; 53515 a recovery is not open, process the run again;
   53516 the asset recoveries changed, calculate the settlement again; 53517 the regular run is approved, locked or paid, so its recoveries stay. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE ast.usp_AssetRecovery_RunDue
    @RunID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT r.RecoveryID, r.EmployeeID, e.EmpCode, r.Amount
    FROM ast.AssetRecovery r
    JOIN mst.Employee e ON e.CompanyID = r.CompanyID AND e.EmployeeID = r.EmployeeID
    WHERE r.CompanyID = @CompanyID AND (r.StatusID = 1 OR (r.StatusID = 3 AND r.AppliedRunID = @RunID))
    ORDER BY e.EmpCode, r.RecoveryID;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_AssetRecovery_RunReserve
    @RunID INT, @RecoveryIdsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID AND RunNo = 1 AND StatusID IN (1, 2))
        THROW 53514, N'The regular run is not found, or it can no longer take recoveries.', 1;
    DECLARE @Ids TABLE (RecoveryID INT NOT NULL PRIMARY KEY);
    INSERT @Ids SELECT DISTINCT CAST([value] AS INT) FROM OPENJSON(@RecoveryIdsJson);
    IF NOT EXISTS (SELECT 1 FROM @Ids) THROW 53515, N'An asset recovery is not open. Process the run again.', 1;
    BEGIN TRANSACTION;
        UPDATE r SET StatusID = 3, AppliedRunID = @RunID, AppliedUtc = SYSUTCDATETIME()
        FROM ast.AssetRecovery r WITH (UPDLOCK, HOLDLOCK)
        JOIN @Ids i ON i.RecoveryID = r.RecoveryID
        WHERE r.CompanyID = @CompanyID AND (r.StatusID = 1 OR (r.StatusID = 3 AND r.AppliedRunID = @RunID));
        IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @Ids) THROW 53515, N'An asset recovery is not open. Process the run again.', 1;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_AssetRecovery_RunRelease
    @RunID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF EXISTS (SELECT 1 FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID AND StatusID IN (4, 5, 6))
        THROW 53517, N'The regular run is approved, locked or paid, so its asset recoveries stay.', 1;
    UPDATE ast.AssetRecovery SET StatusID = 1, AppliedRunID = NULL, AppliedUtc = NULL
    WHERE CompanyID = @CompanyID AND AppliedRunID = @RunID AND StatusID = 3;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_AssetRecovery_FnfDue
    @EmployeeID INT, @FnfID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT r.RecoveryID, r.Amount
    FROM ast.AssetRecovery r
    WHERE r.CompanyID = @CompanyID AND r.EmployeeID = @EmployeeID AND (r.StatusID = 1 OR (r.StatusID = 3 AND r.AppliedFnfID = @FnfID))
    ORDER BY r.RecoveryID;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_AssetRecovery_FnfUncleared
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT a.TagNo
    FROM ast.AssetIssue i
    JOIN ast.Asset a ON a.CompanyID = i.CompanyID AND a.AssetID = i.AssetID
    WHERE i.CompanyID = @CompanyID AND i.EmployeeID = @EmployeeID AND i.ReturnDate IS NULL AND a.StatusID <> 5
      AND NOT EXISTS (SELECT 1 FROM ast.AssetRecovery r WHERE r.CompanyID = i.CompanyID AND r.IssueID = i.IssueID AND r.StatusID IN (1, 3))
    ORDER BY a.TagNo;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_AssetRecovery_FnfReserve
    @FnfID INT, @EmployeeID INT, @Total DECIMAL(14,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    BEGIN TRANSACTION;
        DECLARE @Sum DECIMAL(14,2) = ISNULL((SELECT SUM(Amount) FROM ast.AssetRecovery WITH (UPDLOCK, HOLDLOCK)
                                             WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND (StatusID = 1 OR (StatusID = 3 AND AppliedFnfID = @FnfID))), 0);
        IF @Sum <> @Total THROW 53516, N'The asset recoveries changed. Calculate the settlement again.', 1;
        UPDATE ast.AssetRecovery SET StatusID = 3, AppliedFnfID = @FnfID, AppliedUtc = SYSUTCDATETIME()
        WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND StatusID = 1;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_AssetRecovery_FnfRelease
    @FnfID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    UPDATE ast.AssetRecovery SET StatusID = 1, AppliedFnfID = NULL, AppliedUtc = NULL
    WHERE CompanyID = @CompanyID AND AppliedFnfID = @FnfID AND StatusID = 3;
END
GO
