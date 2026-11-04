/* 0337_pay_arrears_recovery_procs.sql
   Recovery of overpaid arrears claims through the regular run (step 7e-2b, Owner 2026-11-03: overpayments are recovered through a regular run).
   Brave lane 0300-0399. Idempotent (CREATE OR ALTER). Never edit after merge. No new table: a claim whose NetBeforeTax is below zero is a recovery claim.
   While it is Open (StatusID 1) the amount owed is the sum of -NetBeforeTax for the employee; the run service passes it to the regular run as the employee's other deductions,
   so the shortfall rules of the net pay calculator apply (pay is never driven below zero, the rest carries forward).
   At Lock the claims covered by the run line's other deductions are marked StatusID 3 with AppliedRunID = the regular run. At Reopen they go back to Open.
   usp_ArrearsRecovery_Apply is safe to repeat and refuses with 53109 when the run line no longer matches the open recovery claims (process the run again).
   The company comes from SESSION_CONTEXT. Errors: 50002 no company in session, 53109 the recoveries in the run do not match the open recovery claims. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsRecovery_Pending
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT EmployeeID, CAST(SUM(-NetBeforeTax) AS DECIMAL(14,2)) AS Amount
    FROM pay.ArrearsClaim
    WHERE CompanyID = @CompanyID AND StatusID = 1 AND NetBeforeTax < 0
    GROUP BY EmployeeID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsRecovery_Apply
    @RunID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    BEGIN TRANSACTION;
        ;WITH Target AS
        (
            SELECT EmployeeID, SUM(OtherDeductions) AS Amount
            FROM pay.PayrollRunLine WHERE CompanyID = @CompanyID AND RunID = @RunID AND OtherDeductions > 0 GROUP BY EmployeeID
        ),
        Done AS
        (
            SELECT EmployeeID, SUM(-NetBeforeTax) AS Amount
            FROM pay.ArrearsClaim WITH (UPDLOCK, HOLDLOCK)
            WHERE CompanyID = @CompanyID AND AppliedRunID = @RunID AND NetBeforeTax < 0 GROUP BY EmployeeID
        ),
        Need AS
        (
            SELECT t.EmployeeID, t.Amount - ISNULL(d.Amount, 0) AS Remaining
            FROM Target t LEFT JOIN Done d ON d.EmployeeID = t.EmployeeID
        ),
        Pending AS
        (
            SELECT c.ClaimID, c.EmployeeID,
                   SUM(-c.NetBeforeTax) OVER (PARTITION BY c.EmployeeID ORDER BY c.ClaimID ROWS UNBOUNDED PRECEDING) AS Running
            FROM pay.ArrearsClaim c WITH (UPDLOCK, HOLDLOCK)
            WHERE c.CompanyID = @CompanyID AND c.StatusID = 1 AND c.NetBeforeTax < 0
        )
        UPDATE c SET StatusID = 3, AppliedRunID = @RunID, AppliedUtc = SYSUTCDATETIME()
        FROM pay.ArrearsClaim c
        JOIN Pending p ON p.ClaimID = c.ClaimID
        JOIN Need n ON n.EmployeeID = p.EmployeeID
        WHERE c.CompanyID = @CompanyID AND n.Remaining > 0 AND p.Running <= n.Remaining;
        IF EXISTS (SELECT 1
                   FROM (SELECT EmployeeID, SUM(OtherDeductions) AS Amount FROM pay.PayrollRunLine
                         WHERE CompanyID = @CompanyID AND RunID = @RunID AND OtherDeductions > 0 GROUP BY EmployeeID) t
                   FULL JOIN (SELECT EmployeeID, SUM(-NetBeforeTax) AS Amount FROM pay.ArrearsClaim
                              WHERE CompanyID = @CompanyID AND AppliedRunID = @RunID AND NetBeforeTax < 0 GROUP BY EmployeeID) d ON d.EmployeeID = t.EmployeeID
                   WHERE ISNULL(t.Amount, 0) <> ISNULL(d.Amount, 0))
            THROW 53109, N'The arrears recoveries in the run do not match the open recovery claims. Process the run again.', 1;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsRecovery_Reverse
    @RunID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    UPDATE pay.ArrearsClaim SET StatusID = 1, AppliedRunID = NULL, AppliedUtc = NULL
    WHERE CompanyID = @CompanyID AND AppliedRunID = @RunID AND StatusID = 3 AND NetBeforeTax < 0;
END
GO
