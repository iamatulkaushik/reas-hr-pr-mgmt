/* 0329_pay_arrears_procs.sql
   Arrears claim procedures. CREATE OR ALTER keeps it idempotent.
   The company always comes from SESSION_CONTEXT, never from a parameter. No company in session means no read and no write.
   usp_ArrearsClaim_Add takes the month rows as JSON (one array, one row per wage month), so the header and every month are written together or not at all.
   The totals on the header are worked out here from the month rows, never trusted from the caller.
   Error numbers: 50002 no company in session, 53100 employee not found, 53101 a month is already under an open claim, 53102 no months or a month given twice,
   53103 claim not found, 53104 claim already cancelled. */
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
        EsiEmployee DECIMAL(14,2) NOT NULL, EsiEmployer DECIMAL(14,2) NOT NULL
    );
    DECLARE @Given INT = (SELECT COUNT(*) FROM OPENJSON(@MonthsJson));
    IF @Given = 0 THROW 53102, N'Give at least one wage month, each month once.', 1;
    BEGIN TRY
        INSERT @Months
        SELECT WageMonth, PaidGross, DueGross, GrossDifference, EpfWages, EpsWages, EdliWages, EmployeeEpf, EmployerEpf, EmployerEps, Edli, Admin, EsiEmployee, EsiEmployer
        FROM OPENJSON(@MonthsJson) WITH
        (
            WageMonth DATE '$.wageMonth', PaidGross DECIMAL(14,2) '$.paidGross', DueGross DECIMAL(14,2) '$.dueGross',
            GrossDifference DECIMAL(14,2) '$.grossDifference', EpfWages DECIMAL(14,2) '$.epfWages', EpsWages DECIMAL(14,2) '$.epsWages',
            EdliWages DECIMAL(14,2) '$.edliWages', EmployeeEpf DECIMAL(14,2) '$.employeeEpf', EmployerEpf DECIMAL(14,2) '$.employerEpf',
            EmployerEps DECIMAL(14,2) '$.employerEps', Edli DECIMAL(14,2) '$.edli', Admin DECIMAL(14,2) '$.admin',
            EsiEmployee DECIMAL(14,2) '$.esiEmployee', EsiEmployer DECIMAL(14,2) '$.esiEmployer'
        );
    END TRY
    BEGIN CATCH
        THROW 53102, N'Give at least one wage month, each month once.', 1;
    END CATCH
    IF (SELECT COUNT(*) FROM @Months) <> @Given THROW 53102, N'Give at least one wage month, each month once.', 1;
    IF EXISTS (SELECT 1 FROM @Months WHERE DAY(WageMonth) <> 1) THROW 53102, N'Give at least one wage month, each month once.', 1;
    BEGIN TRANSACTION;
        IF EXISTS (SELECT 1 FROM pay.ArrearsClaimMonth m WITH (UPDLOCK, HOLDLOCK)
                   JOIN pay.ArrearsClaim c ON c.CompanyID = m.CompanyID AND c.ClaimID = m.ClaimID
                   JOIN @Months n ON n.WageMonth = m.WageMonth
                   WHERE c.CompanyID = @CompanyID AND c.EmployeeID = @EmployeeID AND c.StatusID = 1)
            THROW 53101, N'A month is already under an open arrears claim for this employee.', 1;
        INSERT pay.ArrearsClaim (CompanyID, EmployeeID, FromMonth, ToMonth, Reason, StatusID, GrossTotal, EmployeeDeductions, EmployerContributions, NetBeforeTax, CreatedByUserID)
        SELECT @CompanyID, @EmployeeID, MIN(WageMonth), MAX(WageMonth), @Reason, 1, SUM(GrossDifference), SUM(EmployeeEpf + EsiEmployee),
               SUM(EmployerEpf + EmployerEps + Edli + Admin + EsiEmployer), SUM(GrossDifference) - SUM(EmployeeEpf + EsiEmployee), @UserID
        FROM @Months;
        DECLARE @ClaimID INT = CAST(SCOPE_IDENTITY() AS INT);
        INSERT pay.ArrearsClaimMonth (CompanyID, ClaimID, WageMonth, PaidGross, DueGross, GrossDifference, EpfWages, EpsWages, EdliWages, EmployeeEpf, EmployerEpf,
                                      EmployerEps, Edli, Admin, EsiEmployee, EsiEmployer)
        SELECT @CompanyID, @ClaimID, WageMonth, PaidGross, DueGross, GrossDifference, EpfWages, EpsWages, EdliWages, EmployeeEpf, EmployerEpf,
               EmployerEps, Edli, Admin, EsiEmployee, EsiEmployer
        FROM @Months;
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
           c.EmployerContributions, c.NetBeforeTax, c.CreatedUtc
    FROM pay.ArrearsClaim c
    JOIN mst.Employee e ON e.CompanyID = c.CompanyID AND e.EmployeeID = c.EmployeeID
    WHERE c.CompanyID = @CompanyID
      AND (@EmployeeID IS NULL OR c.EmployeeID = @EmployeeID)
      AND (@FromMonth IS NULL OR c.ToMonth >= @FromMonth)
      AND (@ToMonth IS NULL OR c.FromMonth <= @ToMonth)
      AND (@IncludeCancelled = 1 OR c.StatusID = 1)
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
           c.EmployerContributions, c.NetBeforeTax, c.CreatedUtc
    FROM pay.ArrearsClaim c
    JOIN mst.Employee e ON e.CompanyID = c.CompanyID AND e.EmployeeID = c.EmployeeID
    WHERE c.CompanyID = @CompanyID AND c.ClaimID = @ClaimID;
    SELECT WageMonth, PaidGross, DueGross, GrossDifference, EpfWages, EpsWages, EdliWages, EmployeeEpf, EmployerEpf, EmployerEps, Edli, Admin, EsiEmployee, EsiEmployer
    FROM pay.ArrearsClaimMonth WHERE CompanyID = @CompanyID AND ClaimID = @ClaimID ORDER BY WageMonth;
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
    UPDATE pay.ArrearsClaim
    SET StatusID = 2, CancelledUtc = SYSUTCDATETIME(), CancelledByUserID = @UserID, CancelReason = @Reason
    WHERE CompanyID = @CompanyID AND ClaimID = @ClaimID AND StatusID = 1;
    IF @@ROWCOUNT = 0 THROW 53104, N'The arrears claim is already cancelled.', 1;
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
    WHERE m.CompanyID = @CompanyID AND c.StatusID = 1 AND m.WageMonth >= @FromMonth AND m.WageMonth <= @ToMonth
    ORDER BY e.EmpCode, m.WageMonth;
END
GO
