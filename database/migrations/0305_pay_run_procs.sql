/* 0305_pay_run_procs.sql
   Payroll run procedures. CREATE OR ALTER keeps it idempotent.
   The company always comes from SESSION_CONTEXT, never from a parameter. No company in the session means no read and no write.
   Status changes follow one table inside pay.usp_Run_ChangeStatus. Every change writes one pay.PayrollRunEvent row. That table is insert-only:
   no procedure updates or deletes it, and the app login has EXECUTE only.
   Error numbers: 50002 no company in session, 50311 run not found, 50312 run cannot be edited in this status, 50313 status change not allowed,
   50314 a run already exists for this period, 50315 totals changed since the run was submitted, 50316 reason too short (10 characters),
   50317 run lines not valid, 50318 an attendance month in the period is not closed, 50319 period dates not valid,
   50324 loan recoveries of this run are still applied (reverse them first, lane 0306). */
SET XACT_ABORT ON;
GO
/* Checksum over the lines of a run. Compared at approval to catch any change after submit. */
CREATE OR ALTER FUNCTION pay.fn_RunChecksum (@CompanyID INT, @RunID INT)
RETURNS BINARY(32)
AS
BEGIN
    DECLARE @Text NVARCHAR(MAX) =
        (SELECT STRING_AGG(CONCAT(EmployeeID, N':', GrossEarnings, N':', TotalDeductions, N':', NetPay), N'|') WITHIN GROUP (ORDER BY EmployeeID)
         FROM pay.PayrollRunLine WHERE CompanyID = @CompanyID AND RunID = @RunID);
    RETURN HASHBYTES('SHA2_256', ISNULL(@Text, N''));
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Run_Create @PeriodFrom DATE, @PeriodTo DATE, @RunNo TINYINT = 1, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @PeriodTo < @PeriodFrom OR DATEDIFF(DAY, @PeriodFrom, @PeriodTo) > 30 THROW 50319, N'The period dates are not valid.', 1;
    IF EXISTS (SELECT 1 FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND PeriodFrom = @PeriodFrom AND RunNo = @RunNo)
        THROW 50314, N'A run already exists for this period.', 1;
    BEGIN TRANSACTION;
        INSERT pay.PayrollRun (CompanyID, PeriodFrom, PeriodTo, RunNo, CreatedByUserID) VALUES (@CompanyID, @PeriodFrom, @PeriodTo, @RunNo, @UserID);
        DECLARE @RunID INT = CAST(SCOPE_IDENTITY() AS INT);
        INSERT pay.PayrollRunEvent (CompanyID, RunID, FromStatusID, ToStatusID, Action, UserID) VALUES (@CompanyID, @RunID, 0, 1, N'Create', @UserID);
    COMMIT TRANSACTION;
    SELECT @RunID AS RunID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Run_List @Top INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT TOP (@Top) RunID, PeriodFrom, PeriodTo, RunNo, StatusID, EmployeeCount, TotalGross, TotalDeductions, TotalNet
    FROM pay.PayrollRun WHERE CompanyID = @CompanyID
    ORDER BY PeriodFrom DESC, RunNo DESC;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Run_Get @RunID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT RunID, PeriodFrom, PeriodTo, RunNo, StatusID, EmployeeCount, TotalGross, TotalDeductions, TotalNet, TotalsChecksum
    FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_RunLine_List @RunID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT EmployeeID, FactoryID, PaidDays, OvertimeHours, OvertimeAmount, GrossEarnings, EpfWage, EsiWage, EpfEmployee, EpfEmployerEpf, EpfEmployerEps,
           Edli, EpfAdmin, EsiEmployee, EsiEmployer, ProfessionalTax, LoanEmi, LoanEmiDeferred, OtherDeductions,
           CarryForwardIn, CarryForwardOut, TotalDeductions, NetPay
    FROM pay.PayrollRunLine WHERE CompanyID = @CompanyID AND RunID = @RunID
    ORDER BY EmployeeID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_RunEvent_List @RunID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT EventID, FromStatusID, ToStatusID, Action, UserID, Reason, EventUtc
    FROM pay.PayrollRunEvent WHERE CompanyID = @CompanyID AND RunID = @RunID
    ORDER BY EventID;
END
GO
/* Replaces all lines of a Draft or Processed run and sets it to Processed.
   @LinesJson: array of objects with the line columns. @EarningsJson: array of {"EmployeeID","ComponentID","Amount"}. */
CREATE OR ALTER PROCEDURE pay.usp_Run_ReplaceLines @RunID INT, @LinesJson NVARCHAR(MAX), @EarningsJson NVARCHAR(MAX), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Status TINYINT, @From DATE, @To DATE;
    SELECT @Status = StatusID, @From = PeriodFrom, @To = PeriodTo FROM pay.PayrollRun WITH (UPDLOCK) WHERE CompanyID = @CompanyID AND RunID = @RunID;
    IF @Status IS NULL THROW 50311, N'Run not found.', 1;
    IF @Status NOT IN (1, 2) THROW 50312, N'The run cannot be edited in this status.', 1;
    IF EXISTS (SELECT 1 FROM pay.LoanRecovery WHERE CompanyID = @CompanyID AND RunID = @RunID)
        THROW 50324, N'Loan recoveries of this run are still applied.', 1;
    IF EXISTS (SELECT 1 FROM (SELECT YEAR(d) * 100 + MONTH(d) AS MonthKey
                              FROM (SELECT DATEADD(MONTH, n.n, DATEFROMPARTS(YEAR(@From), MONTH(@From), 1)) AS d
                                    FROM (VALUES (0), (1)) n (n)) m
                              WHERE d <= @To) k
               WHERE NOT EXISTS (SELECT 1 FROM att.MonthLock l WHERE l.CompanyID = @CompanyID AND l.MonthKey = k.MonthKey AND l.IsClosed = 1))
        THROW 50318, N'An attendance month in the period is not closed.', 1;
    DECLARE @Lines TABLE
    (
        EmployeeID INT NOT NULL PRIMARY KEY, FactoryID INT NOT NULL, PaidDays DECIMAL(7,2) NOT NULL, OvertimeHours DECIMAL(7,2) NOT NULL, OvertimeAmount DECIMAL(14,2) NOT NULL,
        GrossEarnings DECIMAL(14,2) NOT NULL, EpfWage DECIMAL(14,2) NOT NULL, EsiWage DECIMAL(14,2) NOT NULL, EpfEmployee DECIMAL(14,2) NOT NULL,
        EpfEmployerEpf DECIMAL(14,2) NOT NULL, EpfEmployerEps DECIMAL(14,2) NOT NULL, Edli DECIMAL(14,2) NOT NULL, EpfAdmin DECIMAL(14,2) NOT NULL,
        EsiEmployee DECIMAL(14,2) NOT NULL, EsiEmployer DECIMAL(14,2) NOT NULL, ProfessionalTax DECIMAL(14,2) NOT NULL, LoanEmi DECIMAL(14,2) NOT NULL,
        LoanEmiDeferred DECIMAL(14,2) NOT NULL, OtherDeductions DECIMAL(14,2) NOT NULL, CarryForwardIn DECIMAL(14,2) NOT NULL,
        CarryForwardOut DECIMAL(14,2) NOT NULL, TotalDeductions DECIMAL(14,2) NOT NULL, NetPay DECIMAL(14,2) NOT NULL
    );
    DECLARE @Earn TABLE (EmployeeID INT NOT NULL, ComponentID INT NOT NULL, Amount DECIMAL(14,2) NOT NULL, PRIMARY KEY (EmployeeID, ComponentID));
    BEGIN TRY
        INSERT @Lines
        SELECT EmployeeID, FactoryID, PaidDays, OvertimeHours, OvertimeAmount, GrossEarnings, EpfWage, EsiWage, EpfEmployee, EpfEmployerEpf, EpfEmployerEps, Edli, EpfAdmin,
               EsiEmployee, EsiEmployer, ProfessionalTax, LoanEmi, LoanEmiDeferred, OtherDeductions, CarryForwardIn, CarryForwardOut, TotalDeductions, NetPay
        FROM OPENJSON(@LinesJson) WITH (
            EmployeeID INT, FactoryID INT, PaidDays DECIMAL(7,2), OvertimeHours DECIMAL(7,2), OvertimeAmount DECIMAL(14,2), GrossEarnings DECIMAL(14,2), EpfWage DECIMAL(14,2),
            EsiWage DECIMAL(14,2), EpfEmployee DECIMAL(14,2), EpfEmployerEpf DECIMAL(14,2), EpfEmployerEps DECIMAL(14,2), Edli DECIMAL(14,2),
            EpfAdmin DECIMAL(14,2), EsiEmployee DECIMAL(14,2), EsiEmployer DECIMAL(14,2), ProfessionalTax DECIMAL(14,2), LoanEmi DECIMAL(14,2),
            LoanEmiDeferred DECIMAL(14,2), OtherDeductions DECIMAL(14,2), CarryForwardIn DECIMAL(14,2), CarryForwardOut DECIMAL(14,2),
            TotalDeductions DECIMAL(14,2), NetPay DECIMAL(14,2));
        INSERT @Earn
        SELECT EmployeeID, ComponentID, Amount
        FROM OPENJSON(@EarningsJson) WITH (EmployeeID INT, ComponentID INT, Amount DECIMAL(14,2));
    END TRY
    BEGIN CATCH
        THROW 50317, N'The run lines are not valid.', 1;
    END CATCH
    IF EXISTS (SELECT 1 FROM @Earn e WHERE NOT EXISTS (SELECT 1 FROM @Lines l WHERE l.EmployeeID = e.EmployeeID))
        THROW 50317, N'The run lines are not valid.', 1;
    BEGIN TRANSACTION;
        DELETE FROM pay.PayrollRunEarning WHERE CompanyID = @CompanyID AND RunID = @RunID;
        DELETE FROM pay.PayrollRunLine WHERE CompanyID = @CompanyID AND RunID = @RunID;
        INSERT pay.PayrollRunLine (CompanyID, RunID, EmployeeID, FactoryID, PaidDays, OvertimeHours, OvertimeAmount, GrossEarnings, EpfWage, EsiWage, EpfEmployee,
               EpfEmployerEpf, EpfEmployerEps, Edli, EpfAdmin, EsiEmployee, EsiEmployer, ProfessionalTax, LoanEmi, LoanEmiDeferred, OtherDeductions,
               CarryForwardIn, CarryForwardOut, TotalDeductions, NetPay)
        SELECT @CompanyID, @RunID, EmployeeID, FactoryID, PaidDays, OvertimeHours, OvertimeAmount, GrossEarnings, EpfWage, EsiWage, EpfEmployee,
               EpfEmployerEpf, EpfEmployerEps, Edli, EpfAdmin, EsiEmployee, EsiEmployer, ProfessionalTax, LoanEmi, LoanEmiDeferred, OtherDeductions,
               CarryForwardIn, CarryForwardOut, TotalDeductions, NetPay FROM @Lines;
        INSERT pay.PayrollRunEarning (CompanyID, RunID, EmployeeID, ComponentID, Amount)
        SELECT @CompanyID, @RunID, EmployeeID, ComponentID, Amount FROM @Earn;
        UPDATE pay.PayrollRun
        SET StatusID = 2, TotalsChecksum = NULL,
            EmployeeCount = (SELECT COUNT(*) FROM @Lines),
            TotalGross = (SELECT ISNULL(SUM(GrossEarnings), 0) FROM @Lines),
            TotalDeductions = (SELECT ISNULL(SUM(TotalDeductions), 0) FROM @Lines),
            TotalNet = (SELECT ISNULL(SUM(NetPay), 0) FROM @Lines)
        WHERE CompanyID = @CompanyID AND RunID = @RunID;
        INSERT pay.PayrollRunEvent (CompanyID, RunID, FromStatusID, ToStatusID, Action, UserID) VALUES (@CompanyID, @RunID, @Status, 2, N'Process', @UserID);
    COMMIT TRANSACTION;
END
GO
/* Allowed changes: 2>1 and 2>3 and 2>4 (no approval needed), 3>4, 3>1 (cancel or reject), 4>5, 5>6, 5>1 (reopen, written reason).
   1>2 happens only through pay.usp_Run_ReplaceLines. Submit (to 3) and approve-without-submit (2>4) store the checksum. Approve (3>4) compares it. */
CREATE OR ALTER PROCEDURE pay.usp_Run_ChangeStatus @RunID INT, @ToStatusID TINYINT, @Action NVARCHAR(30), @Reason NVARCHAR(500) = NULL, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @From TINYINT, @Stored BINARY(32);
    SELECT @From = StatusID, @Stored = TotalsChecksum FROM pay.PayrollRun WITH (UPDLOCK) WHERE CompanyID = @CompanyID AND RunID = @RunID;
    IF @From IS NULL THROW 50311, N'Run not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM (VALUES (2,1), (2,3), (2,4), (3,4), (3,1), (4,5), (5,6), (5,1)) t (F, T) WHERE t.F = @From AND t.T = @ToStatusID)
        THROW 50313, N'This status change is not allowed.', 1;
    IF @From = 5 AND @ToStatusID = 1 AND (@Reason IS NULL OR LEN(LTRIM(RTRIM(@Reason))) < 10)
        THROW 50316, N'A written reason of at least 10 characters is required.', 1;
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
/* Deletes a Draft or Processed run. The event history stays. */
CREATE OR ALTER PROCEDURE pay.usp_Run_DeleteDraft @RunID INT, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Status TINYINT = (SELECT StatusID FROM pay.PayrollRun WITH (UPDLOCK) WHERE CompanyID = @CompanyID AND RunID = @RunID);
    IF @Status IS NULL THROW 50311, N'Run not found.', 1;
    IF @Status NOT IN (1, 2) THROW 50312, N'The run cannot be edited in this status.', 1;
    BEGIN TRANSACTION;
        DELETE FROM pay.PayrollRunEarning WHERE CompanyID = @CompanyID AND RunID = @RunID;
        DELETE FROM pay.PayrollRunLine WHERE CompanyID = @CompanyID AND RunID = @RunID;
        DELETE FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID;
        INSERT pay.PayrollRunEvent (CompanyID, RunID, FromStatusID, ToStatusID, Action, UserID) VALUES (@CompanyID, @RunID, @Status, 0, N'DeleteDraft', @UserID);
    COMMIT TRANSACTION;
END
GO
