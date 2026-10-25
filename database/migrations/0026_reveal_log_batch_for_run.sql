/* 0026_reveal_log_batch_for_run.sql
   Change request 0017, ADR 0019 rule 2 and its amendment. aud.usp_Reveal_InsertBatch (0024) accepts any employee ids of the company.
   The rule says ids are released only for the employees of one approved, locked or paid payroll run. The module was the only check.
   aud.usp_Reveal_InsertBatchForRun moves that check into SQL: the run must exist in the session company, its status must be
   Approved (4), Locked (5) or Paid (6), and every employee id must be a line of that run. Otherwise nothing is logged.
   The reason is stored with the run number, for example "ECR export run 123", so the reveal list shows which export it was.
   The company always comes from SESSION_CONTEXT. The user comes from SESSION_CONTEXT UserID when the app set it, otherwise @UserID.
   Errors: 50044 no reason, 50045 list not valid, 50046 more than 5000 (same meaning as 0024),
   50052 run not found, 50053 run not approved, 50054 an employee is not a line of this run.
   The 0024 procedure stays until the Employees module calls this one; a later migration removes it. Idempotent. Never edit after merge. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE aud.usp_Reveal_InsertBatchForRun
    @UserID INT, @RunID INT, @Reason NVARCHAR(40), @Fields NVARCHAR(100), @EmployeeIdsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @ByUserID INT = COALESCE(TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID')), @UserID);
    IF @Reason IS NULL OR LEN(LTRIM(RTRIM(@Reason))) = 0
        THROW 50044, N'A reveal needs a reason.', 1;
    DECLARE @Status TINYINT;
    SELECT @Status = StatusID FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID;
    IF @Status IS NULL THROW 50052, N'Payroll run not found.', 1;
    IF @Status NOT IN (4, 5, 6) THROW 50053, N'The run must be approved before ids are released.', 1;
    IF @EmployeeIdsJson IS NULL OR ISJSON(@EmployeeIdsJson) <> 1
        THROW 50045, N'The employee list is not valid.', 1;
    DECLARE @Ids TABLE (EmployeeID INT NOT NULL PRIMARY KEY);
    INSERT @Ids (EmployeeID)
    SELECT DISTINCT TRY_CONVERT(INT, [value]) FROM OPENJSON(@EmployeeIdsJson) WHERE TRY_CONVERT(INT, [value]) IS NOT NULL;
    DECLARE @Listed INT = (SELECT COUNT(*) FROM OPENJSON(@EmployeeIdsJson));
    DECLARE @Distinct INT = (SELECT COUNT(*) FROM @Ids);
    IF @Distinct = 0 OR @Distinct <> @Listed
        THROW 50045, N'The employee list must be a non-empty list of distinct whole numbers.', 1;
    IF @Distinct > 5000
        THROW 50046, N'At most 5000 employees can be revealed in one export.', 1;
    IF EXISTS (SELECT 1 FROM @Ids i
               WHERE NOT EXISTS (SELECT 1 FROM pay.PayrollRunLine l
                                 WHERE l.CompanyID = @CompanyID AND l.RunID = @RunID AND l.EmployeeID = i.EmployeeID))
        THROW 50054, N'An employee in the list is not part of this payroll run.', 1;
    DECLARE @Why NVARCHAR(50) = LEFT(LTRIM(RTRIM(@Reason)) + N' run ' + CAST(@RunID AS NVARCHAR(10)), 50);
    INSERT aud.RevealLog (CompanyID, UserID, EmployeeID, Fields, RowsRevealed, Reason)
    SELECT @CompanyID, @ByUserID, i.EmployeeID, @Fields, 1, @Why FROM @Ids i;
END
GO
