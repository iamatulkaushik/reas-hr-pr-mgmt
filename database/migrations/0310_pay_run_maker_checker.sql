/* 0310_pay_run_maker_checker.sql
   Brave, Phase 5 review finding 3 and 4 (docs/reviews/2026-10-14-sigma-review-phase5.md).
   Replaces pay.usp_Run_ChangeStatus (0305). CREATE OR ALTER keeps it idempotent.
   Changes:
   1. The acting user comes from SESSION_CONTEXT(N'UserID'), as the Associate procedures do (ADR 0014). The @UserID parameter is removed.
   2. Maker and checker: a run cannot go from Submitted (3) to Approved (4) by the user who submitted it. New error 50329.
   3. No user in the session means no status change. New error 50330.
   Skipping approval (2 to 4): this procedure does NOT check whether the plan needs approval. The service gate IApprovalService.IsRequired decides.
   The procedure only enforces the status table, the checksum and the maker-checker rule.
   Error numbers added: 50329 approver is the user who submitted the run, 50330 no user in session.
   Existing numbers are unchanged: 50002, 50311, 50313, 50315, 50316. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_Run_ChangeStatus @RunID INT, @ToStatusID TINYINT, @Action NVARCHAR(30), @Reason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    DECLARE @UserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @UserID IS NULL THROW 50330, N'No user in session.', 1;
    DECLARE @From TINYINT, @Stored BINARY(32);
    SELECT @From = StatusID, @Stored = TotalsChecksum FROM pay.PayrollRun WITH (UPDLOCK) WHERE CompanyID = @CompanyID AND RunID = @RunID;
    IF @From IS NULL THROW 50311, N'Run not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM (VALUES (2,1), (2,3), (2,4), (3,4), (3,1), (4,5), (5,6), (5,1)) t (F, T) WHERE t.F = @From AND t.T = @ToStatusID)
        THROW 50313, N'This status change is not allowed.', 1;
    IF @From = 5 AND @ToStatusID = 1 AND (@Reason IS NULL OR LEN(LTRIM(RTRIM(@Reason))) < 10)
        THROW 50316, N'A written reason of at least 10 characters is required.', 1;
    IF @From = 3 AND @ToStatusID = 4
    BEGIN
        DECLARE @SubmittedBy INT =
            (SELECT TOP (1) UserID FROM pay.PayrollRunEvent
             WHERE CompanyID = @CompanyID AND RunID = @RunID AND ToStatusID = 3
             ORDER BY EventID DESC);
        IF @SubmittedBy IS NOT NULL AND @SubmittedBy = @UserID
            THROW 50329, N'The person who submitted the run cannot approve it.', 1;
    END
    DECLARE @Now BINARY(32) = pay.fn_RunChecksum(@CompanyID, @RunID);
    IF @From = 3 AND @ToStatusID = 4 AND (@Stored IS NULL OR @Stored <> @Now)
        THROW 50315, N'The totals changed after the run was submitted.', 1;
    BEGIN TRANSACTION;
        UPDATE pay.PayrollRun
        SET StatusID = @ToStatusID,
            TotalsChecksum = CASE WHEN @ToStatusID IN (3, 4) AND @From = 2 THEN @Now WHEN @ToStatusID = 1 THEN NULL ELSE TotalsChecksum END
        WHERE CompanyID = @CompanyID AND RunID = @RunID;
        INSERT pay.PayrollRunEvent (CompanyID, RunID, FromStatusID, ToStatusID, Action, UserID, Reason) VALUES (@CompanyID, @RunID, @From, @ToStatusID, @Action, @UserID, @Reason);
    COMMIT TRANSACTION;
END
GO
