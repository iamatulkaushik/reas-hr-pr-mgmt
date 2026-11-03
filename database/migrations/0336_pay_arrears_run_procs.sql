/* 0336_pay_arrears_run_procs.sql
   Arrears procedures for step 7e-2. CREATE OR ALTER replaces the 0329 and 0331 versions where it says so; nothing in 0329 or 0331 is edited. Idempotent.
   The company always comes from SESSION_CONTEXT. Status: 1 Open, 2 Cancelled, 3 Paid. A claim that is Open or Paid counts as taken for the month: the same month cannot be claimed again,
   and the arrear ECR and the report include both (Sigma's note on 0330 and 0331). A Paid claim cannot be cancelled.
   usp_ArrearsClaim_Add now also takes each month's heads, overtime difference and ESI wage from the JSON, and checks that heads plus overtime add up to the gross difference.
   usp_ArrearsClaim_Apply marks open claims Paid by one run, all or none. usp_ArrearsClaim_Release puts them back to Open when the run could not be filled, only while the run has no lines.
   usp_ArrearsRun_NextNo gives the next free run number (2 or more) for a pay period.
   Error numbers: 50002 no company in session, 53100 employee not found, 53101 a month is already claimed, 53102 no months or a month given twice or a month's heads do not add up,
   53103 claim not found, 53104 claim already cancelled, 53105 a paid claim cannot be cancelled, 53106 a claim is not open, 53107 the run is not found, 53108 the run already has lines. */
SET XACT_ABORT ON;
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
                   WHERE c.CompanyID = @CompanyID AND c.EmployeeID = @EmployeeID AND c.StatusID IN (1, 3))
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
      AND (@IncludeCancelled = 1 OR c.StatusID IN (1, 3))
    ORDER BY c.ClaimID DESC;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsClaim_Get
    @ClaimID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.ArrearsClaim WHERE CompanyID = @CompanyID AND ClaimID = @ClaimID) THROW 53103, N'Arrears claim not found.', 1;
    SELECT c.ClaimID, c.EmployeeID, e.EmpCode, e.FullName, c.FromMonth, c.ToMonth, c.Reason, c.StatusID, c.GrossTotal, c.EmployeeDeductions,
           c.EmployerContributions, c.NetBeforeTax, c.CreatedUtc, c.AppliedRunID
    FROM pay.ArrearsClaim c
    JOIN mst.Employee e ON e.CompanyID = c.CompanyID AND e.EmployeeID = c.EmployeeID
    WHERE c.CompanyID = @CompanyID AND c.ClaimID = @ClaimID;
    SELECT WageMonth, PaidGross, DueGross, GrossDifference, EpfWages, EpsWages, EdliWages, EmployeeEpf, EmployerEpf, EmployerEps, Edli, Admin, EsiEmployee, EsiEmployer,
           OvertimeDifference, EsiWages
    FROM pay.ArrearsClaimMonth WHERE CompanyID = @CompanyID AND ClaimID = @ClaimID ORDER BY WageMonth;
    SELECT WageMonth, ComponentID, Amount
    FROM pay.ArrearsClaimHead WHERE CompanyID = @CompanyID AND ClaimID = @ClaimID ORDER BY WageMonth, ComponentID;
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
    WHERE m.CompanyID = @CompanyID AND c.StatusID IN (1, 3) AND m.WageMonth >= @FromMonth AND m.WageMonth <= @ToMonth
    ORDER BY e.EmpCode, m.WageMonth;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsEcr_List
    @WageMonth DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT c.EmployeeID, e.EmpCode, e.FullName,
           SUM(m.EpfWages) AS EpfWages, SUM(m.EpsWages) AS EpsWages, SUM(m.EdliWages) AS EdliWages,
           SUM(m.EmployeeEpf) AS EmployeeEpf, SUM(m.EmployerEps) AS EmployerEps, SUM(m.EmployerEpf) AS EmployerEpf
    FROM pay.ArrearsClaimMonth m
    JOIN pay.ArrearsClaim c ON c.CompanyID = m.CompanyID AND c.ClaimID = m.ClaimID
    JOIN mst.Employee e ON e.CompanyID = c.CompanyID AND e.EmployeeID = c.EmployeeID
    WHERE m.CompanyID = @CompanyID AND c.StatusID IN (1, 3) AND m.WageMonth = @WageMonth
    GROUP BY c.EmployeeID, e.EmpCode, e.FullName
    ORDER BY e.EmpCode;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsClaim_Apply
    @RunID INT, @ClaimIdsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID AND RunNo > 1) THROW 53107, N'The arrears run is not found.', 1;
    DECLARE @Ids TABLE (ClaimID INT NOT NULL PRIMARY KEY);
    INSERT @Ids SELECT DISTINCT CAST([value] AS INT) FROM OPENJSON(@ClaimIdsJson);
    IF NOT EXISTS (SELECT 1 FROM @Ids) THROW 53106, N'A claim is not open.', 1;
    BEGIN TRANSACTION;
        UPDATE c SET StatusID = 3, AppliedRunID = @RunID, AppliedUtc = SYSUTCDATETIME()
        FROM pay.ArrearsClaim c WITH (UPDLOCK, HOLDLOCK)
        JOIN @Ids i ON i.ClaimID = c.ClaimID
        WHERE c.CompanyID = @CompanyID AND c.StatusID = 1;
        IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @Ids) THROW 53106, N'A claim is not open.', 1;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsClaim_Release
    @RunID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF EXISTS (SELECT 1 FROM pay.PayrollRunLine WHERE CompanyID = @CompanyID AND RunID = @RunID) THROW 53108, N'The arrears run already has lines.', 1;
    UPDATE pay.ArrearsClaim SET StatusID = 1, AppliedRunID = NULL, AppliedUtc = NULL
    WHERE CompanyID = @CompanyID AND AppliedRunID = @RunID AND StatusID = 3;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsRun_NextNo
    @PeriodFrom DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT CAST(ISNULL(MAX(RunNo), 1) + 1 AS INT) AS NextRunNo FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND PeriodFrom = @PeriodFrom;
END
GO
