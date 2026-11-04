/* 0337_pay_arrears_recovery.sql
   Recovery of overpaid arrears claims through a regular run (step 7e-2b, Owner decision 2026-11-03: overpayments are recovered through a regular run).
   Brave lane 0300-0399 (taken over by Sigma, 2026-11-04). Idempotent. Never edit after merge. Tenant policy already covers pay.ArrearsClaim (0328), so there is no new table.
   An overpaid claim is an Open claim whose NetBeforeTax is below zero (gross difference less the employee's EPF and ESI on the difference). The amount to recover is minus that figure.
   StatusID 4 Recovered: the claim is held by a regular run (RunNo 1) in AppliedRunID. It is set when the run is processed and put back to Open when the run goes back to Draft, is reopened or is
   processed again, so a claim cannot be taken by two runs. Once the run is Locked the status stays. A recovered claim counts as taken for its months (no new claim for them) and stays in the
   list and the report. It is left out of the arrear ECR, which cannot carry a negative amount. It cannot be cancelled while a run holds it.
   usp_ArrearsRecovery_Due lists the open overpaid claims and the ones this run already holds, one row per claim. usp_ArrearsRecovery_Reserve holds claims for a regular run, all or none (a claim the run already holds counts, so processing the run again is safe). usp_ArrearsRecovery_Release puts a run's claims back.
   usp_ArrearsClaim_Add, _List, _Cancel and usp_ArrearsReport are replaced (CREATE OR ALTER) with the status 4 change only; nothing in 0336 is edited.
   Error numbers: 53109 a claim held by a regular run cannot be cancelled, 53110 a claim is not open for recovery, 53111 the regular run is not found or cannot take recoveries. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'pay.CK_ArrearsClaim_Status', N'C') IS NOT NULL
    ALTER TABLE pay.ArrearsClaim DROP CONSTRAINT CK_ArrearsClaim_Status;
GO
ALTER TABLE pay.ArrearsClaim ADD CONSTRAINT CK_ArrearsClaim_Status CHECK (StatusID BETWEEN 1 AND 4);
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsClaim_Add
    @EmployeeID INT, @Reason NVARCHAR(200), @UserID INT = NULL, @MonthsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID) THROW 53100, N'Employee not found.', 1;
    DECLARE @Months TABLE
    (
        WageMonth DATE NOT NULL PRIMARY KEY, PaidGross DECIMAL(14,2) NOT NULL, DueGross DECIMAL(14,2) NOT NULL, GrossDifference DECIMAL(14,2) NOT NULL,
        EpfWages DECIMAL(14,2) NOT NULL, EpsWages DECIMAL(14,2) NOT NULL, EdliWages DECIMAL(14,2) NOT NULL, EmployeeEpf DECIMAL(14,2) NOT NULL,
        EmployerEpf DECIMAL(14,2) NOT NULL, EmployerEps DECIMAL(14,2) NOT NULL, Edli DECIMAL(14,2) NOT NULL, Admin DECIMAL(14,2) NOT NULL,
        EsiEmployee DECIMAL(14,2) NOT NULL, EsiEmployer DECIMAL(14,2) NOT NULL, OvertimeDifference DECIMAL(14,2) NOT NULL, EsiWages DECIMAL(14,2) NOT NULL,
        HeadsJson NVARCHAR(MAX) NULL
    );
    DECLARE @Heads TABLE (WageMonth DATE NOT NULL, ComponentID INT NOT NULL, Amount DECIMAL(14,2) NOT NULL, PRIMARY KEY (WageMonth, ComponentID));
    DECLARE @Given INT = (SELECT COUNT(*) FROM OPENJSON(@MonthsJson));
    IF @Given = 0 THROW 53102, N'Give at least one wage month, each month once.', 1;
    BEGIN TRY
        INSERT @Months
        SELECT WageMonth, PaidGross, DueGross, GrossDifference, EpfWages, EpsWages, EdliWages, EmployeeEpf, EmployerEpf, EmployerEps, Edli, Admin, EsiEmployee, EsiEmployer,
               OvertimeDifference, EsiWages, HeadsJson
        FROM OPENJSON(@MonthsJson) WITH
        (
            WageMonth DATE '$.wageMonth', PaidGross DECIMAL(14,2) '$.paidGross', DueGross DECIMAL(14,2) '$.dueGross',
            GrossDifference DECIMAL(14,2) '$.grossDifference', EpfWages DECIMAL(14,2) '$.epfWages', EpsWages DECIMAL(14,2) '$.epsWages',
            EdliWages DECIMAL(14,2) '$.edliWages', EmployeeEpf DECIMAL(14,2) '$.employeeEpf', EmployerEpf DECIMAL(14,2) '$.employerEpf',
            EmployerEps DECIMAL(14,2) '$.employerEps', Edli DECIMAL(14,2) '$.edli', Admin DECIMAL(14,2) '$.admin',
            EsiEmployee DECIMAL(14,2) '$.esiEmployee', EsiEmployer DECIMAL(14,2) '$.esiEmployer',
            OvertimeDifference DECIMAL(14,2) '$.overtimeDifference', EsiWages DECIMAL(14,2) '$.esiWages', HeadsJson NVARCHAR(MAX) '$.heads' AS JSON
        );
        INSERT @Heads
        SELECT m.WageMonth, h.ComponentID, h.Amount
        FROM @Months m
        CROSS APPLY OPENJSON(m.HeadsJson) WITH (ComponentID INT '$.componentId', Amount DECIMAL(14,2) '$.amount') h
        WHERE m.HeadsJson IS NOT NULL;
    END TRY
    BEGIN CATCH
        THROW 53102, N'Give at least one wage month, each month once.', 1;
    END CATCH
    IF (SELECT COUNT(*) FROM @Months) <> @Given THROW 53102, N'Give at least one wage month, each month once.', 1;
    IF EXISTS (SELECT 1 FROM @Months WHERE DAY(WageMonth) <> 1) THROW 53102, N'Give at least one wage month, each month once.', 1;
    IF EXISTS (SELECT 1 FROM @Months m WHERE m.GrossDifference <> m.OvertimeDifference + ISNULL((SELECT SUM(h.Amount) FROM @Heads h WHERE h.WageMonth = m.WageMonth), 0))
        THROW 53102, N'Give at least one wage month, each month once.', 1;
    BEGIN TRANSACTION;
        IF EXISTS (SELECT 1 FROM pay.ArrearsClaimMonth m WITH (UPDLOCK, HOLDLOCK)
                   JOIN pay.ArrearsClaim c ON c.CompanyID = m.CompanyID AND c.ClaimID = m.ClaimID
                   JOIN @Months n ON n.WageMonth = m.WageMonth
                   WHERE c.CompanyID = @CompanyID AND c.EmployeeID = @EmployeeID AND c.StatusID IN (1, 3, 4))
            THROW 53101, N'A month is already under an arrears claim for this employee.', 1;
        INSERT pay.ArrearsClaim (CompanyID, EmployeeID, FromMonth, ToMonth, Reason, StatusID, GrossTotal, EmployeeDeductions, EmployerContributions, NetBeforeTax, CreatedByUserID)
        SELECT @CompanyID, @EmployeeID, MIN(WageMonth), MAX(WageMonth), @Reason, 1, SUM(GrossDifference), SUM(EmployeeEpf + EsiEmployee),
               SUM(EmployerEpf + EmployerEps + Edli + Admin + EsiEmployer), SUM(GrossDifference) - SUM(EmployeeEpf + EsiEmployee), @UserID
        FROM @Months;
        DECLARE @ClaimID INT = CAST(SCOPE_IDENTITY() AS INT);
        INSERT pay.ArrearsClaimMonth (CompanyID, ClaimID, WageMonth, PaidGross, DueGross, GrossDifference, EpfWages, EpsWages, EdliWages, EmployeeEpf, EmployerEpf,
                                      EmployerEps, Edli, Admin, EsiEmployee, EsiEmployer, OvertimeDifference, EsiWages)
        SELECT @CompanyID, @ClaimID, WageMonth, PaidGross, DueGross, GrossDifference, EpfWages, EpsWages, EdliWages, EmployeeEpf, EmployerEpf,
               EmployerEps, Edli, Admin, EsiEmployee, EsiEmployer, OvertimeDifference, EsiWages
        FROM @Months;
        INSERT pay.ArrearsClaimHead (CompanyID, ClaimID, WageMonth, ComponentID, Amount)
        SELECT @CompanyID, @ClaimID, WageMonth, ComponentID, Amount FROM @Heads;
    COMMIT TRANSACTION;
    SELECT @ClaimID AS ClaimID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsClaim_List
    @EmployeeID INT = NULL, @FromMonth DATE = NULL, @ToMonth DATE = NULL, @IncludeCancelled BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT c.ClaimID, c.EmployeeID, e.EmpCode, e.FullName, c.FromMonth, c.ToMonth, c.Reason, c.StatusID, c.GrossTotal, c.EmployeeDeductions,
           c.EmployerContributions, c.NetBeforeTax, c.CreatedUtc, c.AppliedRunID
    FROM pay.ArrearsClaim c
    JOIN mst.Employee e ON e.CompanyID = c.CompanyID AND e.EmployeeID = c.EmployeeID
    WHERE c.CompanyID = @CompanyID
      AND (@EmployeeID IS NULL OR c.EmployeeID = @EmployeeID)
      AND (@FromMonth IS NULL OR c.ToMonth >= @FromMonth)
      AND (@ToMonth IS NULL OR c.FromMonth <= @ToMonth)
      AND (@IncludeCancelled = 1 OR c.StatusID IN (1, 3, 4))
    ORDER BY c.ClaimID DESC;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsClaim_Cancel
    @ClaimID INT, @UserID INT = NULL, @Reason NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Status TINYINT = (SELECT StatusID FROM pay.ArrearsClaim WHERE CompanyID = @CompanyID AND ClaimID = @ClaimID);
    IF @Status IS NULL THROW 53103, N'Arrears claim not found.', 1;
    IF @Status = 2 THROW 53104, N'The arrears claim is already cancelled.', 1;
    IF @Status = 3 THROW 53105, N'A paid arrears claim cannot be cancelled.', 1;
    IF @Status = 4 THROW 53109, N'A claim that a regular run is recovering cannot be cancelled. Return that run to Draft first.', 1;
    UPDATE pay.ArrearsClaim
    SET StatusID = 2, CancelledUtc = SYSUTCDATETIME(), CancelledByUserID = @UserID, CancelReason = @Reason
    WHERE CompanyID = @CompanyID AND ClaimID = @ClaimID AND StatusID = 1;
    IF @@ROWCOUNT = 0 THROW 53105, N'A paid arrears claim cannot be cancelled.', 1;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsReport
    @FromMonth DATE, @ToMonth DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT c.ClaimID, c.EmployeeID, e.EmpCode, e.FullName, m.WageMonth, m.GrossDifference, m.EmployeeEpf, m.EsiEmployee,
           m.EmployerEpf + m.EmployerEps + m.Edli + m.Admin + m.EsiEmployer AS EmployerTotal
    FROM pay.ArrearsClaimMonth m
    JOIN pay.ArrearsClaim c ON c.CompanyID = m.CompanyID AND c.ClaimID = m.ClaimID
    JOIN mst.Employee e ON e.CompanyID = c.CompanyID AND e.EmployeeID = c.EmployeeID
    WHERE m.CompanyID = @CompanyID AND c.StatusID IN (1, 3, 4) AND m.WageMonth >= @FromMonth AND m.WageMonth <= @ToMonth
    ORDER BY e.EmpCode, m.WageMonth;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsRecovery_Due
    @RunID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT c.ClaimID, c.EmployeeID, e.EmpCode, -c.NetBeforeTax AS Amount
    FROM pay.ArrearsClaim c
    JOIN mst.Employee e ON e.CompanyID = c.CompanyID AND e.EmployeeID = c.EmployeeID
    WHERE c.CompanyID = @CompanyID AND c.NetBeforeTax < 0
      AND (c.StatusID = 1 OR (c.StatusID = 4 AND c.AppliedRunID = @RunID))
    ORDER BY e.EmpCode, c.ClaimID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsRecovery_Reserve
    @RunID INT, @ClaimIdsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID AND RunNo = 1 AND StatusID IN (1, 2))
        THROW 53111, N'The regular run is not found, or it can no longer take recoveries.', 1;
    DECLARE @Ids TABLE (ClaimID INT NOT NULL PRIMARY KEY);
    INSERT @Ids SELECT DISTINCT CAST([value] AS INT) FROM OPENJSON(@ClaimIdsJson);
    IF NOT EXISTS (SELECT 1 FROM @Ids) THROW 53110, N'A claim is not open for recovery. Process the run again.', 1;
    BEGIN TRANSACTION;
        UPDATE c SET StatusID = 4, AppliedRunID = @RunID, AppliedUtc = SYSUTCDATETIME()
        FROM pay.ArrearsClaim c WITH (UPDLOCK, HOLDLOCK)
        JOIN @Ids i ON i.ClaimID = c.ClaimID
        WHERE c.CompanyID = @CompanyID AND c.NetBeforeTax < 0 AND (c.StatusID = 1 OR (c.StatusID = 4 AND c.AppliedRunID = @RunID));
        IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @Ids) THROW 53110, N'A claim is not open for recovery. Process the run again.', 1;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsRecovery_Release
    @RunID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF EXISTS (SELECT 1 FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID AND StatusID IN (4, 5, 6))
        THROW 53111, N'The regular run is approved, locked or paid, so its recoveries stay.', 1;
    UPDATE pay.ArrearsClaim SET StatusID = 1, AppliedRunID = NULL, AppliedUtc = NULL
    WHERE CompanyID = @CompanyID AND AppliedRunID = @RunID AND StatusID = 4;
END
GO
