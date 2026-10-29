/* 0324_pay_run_tax_procs.sql
   Change request 0019, step 5b. Brave lane 0300-0399. Idempotent (CREATE OR ALTER). Never edit after merge.
   usp_RunInput_List: the 0311 procedure with IsHra added as the last column of the salary result set.
   usp_RunTax_Facts: the facts income tax needs for a run, in two result sets, for the employees in @EmployeeIdsJson (a JSON array of ids).
   1. One row per employee and month (yyyymm of PeriodTo) of the financial year from Locked or Paid runs of the company, arrears runs included and the run being processed left out:
      TaxableSalary (gross), BasicPlusDa and HraReceived (from the run's earnings by the Basic, DA and HRA flags) and TaxDeducted (the lines' TdsAmount).
   2. One row per employee: DateOfBirth.
   The company comes from SESSION_CONTEXT. Errors: 50002 no company in session, 50311 run not found. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_RunInput_List @PeriodFrom DATE, @PeriodTo DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @PeriodTo < @PeriodFrom OR DATEDIFF(DAY, @PeriodFrom, @PeriodTo) > 30 THROW 50319, N'The period dates are not valid.', 1;

    SELECT e.EmployeeID, e.FactoryID, e.EmpCode,
           CAST(CASE WHEN e.PayTypeID = 1 THEN 1 ELSE 0 END AS BIT) AS IsDailyWage,
           CAST(CASE e.Gender WHEN 'M' THEN 1 WHEN 'F' THEN 2 ELSE 0 END AS INT) AS GenderID,
           CAST(CASE WHEN ISNULL(s.PtApplicable, 1) = 1 THEN f.StateID END AS INT) AS PtStateID,
           CAST(ISNULL(s.IsEpfMember, 0) AS BIT) AS IsEpfMember,
           CAST(ISNULL(s.EpfOnHigherWage, 0) AS BIT) AS EpfOnHigherWage,
           CAST(ISNULL(s.EpsOnHigherWage, 0) AS BIT) AS EpsOnHigherWage,
           CAST(ISNULL(s.IsEsiMember, 0) AS BIT) AS IsEsiMember,
           CAST(ISNULL(a.PaidDays, 0) AS DECIMAL(7,2)) AS PaidDays,
           CAST(ROUND(ISNULL(a.OvertimeMinutes, 0) / 60.0, 2) AS DECIMAL(7,2)) AS OvertimeHours,
           CAST(sh.FullDayMinutes / 60.0 AS DECIMAL(7,2)) AS HoursPerDay,
           CAST(w.WindowDays - ISNULL(a.MarkedDays, 0) AS INT) AS UnmarkedDays,
           CAST(ISNULL(cf.CarryForwardOut, 0) AS DECIMAL(14,2)) AS CarryForwardIn
    FROM mst.Employee e
    JOIN mst.Factory f ON f.CompanyID = e.CompanyID AND f.FactoryID = e.FactoryID
    LEFT JOIN mst.EmployeeStatutory s ON s.CompanyID = e.CompanyID AND s.EmployeeID = e.EmployeeID
    CROSS APPLY (SELECT CASE WHEN e.JoinDate > @PeriodFrom THEN e.JoinDate ELSE @PeriodFrom END AS WinFrom,
                        CASE WHEN e.ExitDate IS NOT NULL AND e.ExitDate < @PeriodTo THEN e.ExitDate ELSE @PeriodTo END AS WinTo) win
    CROSS APPLY (SELECT DATEDIFF(DAY, win.WinFrom, win.WinTo) + 1 AS WindowDays) w
    OUTER APPLY (SELECT COUNT(*) AS MarkedDays,
                        SUM(CASE x.StatusID WHEN 1 THEN 1.0 WHEN 3 THEN 0.5 WHEN 4 THEN 1.0 WHEN 5 THEN 1.0 WHEN 6 THEN 1.0 ELSE 0.0 END) AS PaidDays,
                        SUM(CAST(x.OvertimeMinutes AS INT)) AS OvertimeMinutes
                 FROM att.AttendanceDaily x
                 WHERE x.CompanyID = e.CompanyID AND x.EmployeeID = e.EmployeeID AND x.WorkDate >= win.WinFrom AND x.WorkDate <= win.WinTo) a
    OUTER APPLY (SELECT TOP (1) t.FullDayMinutes
                 FROM att.EmployeeShift es
                 JOIN att.Shift t ON t.ShiftID = es.ShiftID
                 WHERE es.CompanyID = e.CompanyID AND es.EmployeeID = e.EmployeeID AND es.EffectiveFrom <= win.WinTo
                   AND (es.EffectiveTo IS NULL OR es.EffectiveTo >= win.WinTo)
                 ORDER BY es.EffectiveFrom DESC) sh
    OUTER APPLY (SELECT TOP (1) l.CarryForwardOut
                 FROM pay.PayrollRunLine l
                 JOIN pay.PayrollRun r ON r.CompanyID = l.CompanyID AND r.RunID = l.RunID
                 WHERE l.CompanyID = e.CompanyID AND l.EmployeeID = e.EmployeeID AND r.StatusID IN (5, 6) AND r.PeriodFrom < @PeriodFrom
                 ORDER BY r.PeriodFrom DESC, r.RunNo DESC) cf
    WHERE e.CompanyID = @CompanyID AND e.JoinDate <= @PeriodTo AND (e.ExitDate IS NULL OR e.ExitDate >= @PeriodFrom)
      AND (e.IsActive = 1 OR e.ExitDate IS NOT NULL)
    ORDER BY e.EmployeeID;

    SELECT s.EmployeeID, l.ComponentID, c.Code, c.Name, c.IsBasic, c.IsDa, c.IsOtBase, l.Amount, c.IsHra
    FROM pay.EmployeeSalary s
    JOIN pay.EmployeeSalaryLine l ON l.CompanyID = s.CompanyID AND l.SalaryID = s.SalaryID
    JOIN pay.SalaryComponent c ON c.CompanyID = l.CompanyID AND c.ComponentID = l.ComponentID
    WHERE s.CompanyID = @CompanyID
      AND s.EffectiveFrom = (SELECT MAX(x.EffectiveFrom) FROM pay.EmployeeSalary x
                             WHERE x.CompanyID = s.CompanyID AND x.EmployeeID = s.EmployeeID AND x.EffectiveFrom <= @PeriodTo)
    ORDER BY s.EmployeeID, c.SortOrder, c.Code;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_RunTax_Facts @RunID INT, @FinancialYear INT, @EmployeeIdsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID) THROW 50311, N'Run not found.', 1;
    DECLARE @FromDate DATE = DATEFROMPARTS(@FinancialYear, 4, 1);
    DECLARE @ToDate DATE = DATEFROMPARTS(@FinancialYear + 1, 3, 31);
    DECLARE @Ids TABLE (EmployeeID INT NOT NULL PRIMARY KEY);
    INSERT @Ids SELECT DISTINCT CAST([value] AS INT) FROM OPENJSON(@EmployeeIdsJson);

    SELECT l.EmployeeID,
           YEAR(r.PeriodTo) * 100 + MONTH(r.PeriodTo) AS MonthKey,
           SUM(l.GrossEarnings)                      AS TaxableSalary,
           SUM(ISNULL(x.BasicDa, 0))                 AS BasicPlusDa,
           SUM(ISNULL(x.Hra, 0))                     AS HraReceived,
           SUM(l.TdsAmount)                          AS TaxDeducted
    FROM pay.PayrollRunLine l
    JOIN pay.PayrollRun r ON r.CompanyID = l.CompanyID AND r.RunID = l.RunID
    JOIN @Ids i ON i.EmployeeID = l.EmployeeID
    OUTER APPLY (SELECT SUM(CASE WHEN c.IsBasic = 1 OR c.IsDa = 1 THEN pe.Amount ELSE 0 END) AS BasicDa,
                        SUM(CASE WHEN c.IsHra = 1 THEN pe.Amount ELSE 0 END) AS Hra
                 FROM pay.PayrollRunEarning pe
                 JOIN pay.SalaryComponent c ON c.CompanyID = pe.CompanyID AND c.ComponentID = pe.ComponentID
                 WHERE pe.CompanyID = l.CompanyID AND pe.RunID = l.RunID AND pe.EmployeeID = l.EmployeeID) x
    WHERE l.CompanyID = @CompanyID AND r.StatusID IN (5, 6) AND r.RunID <> @RunID AND r.PeriodTo BETWEEN @FromDate AND @ToDate
    GROUP BY l.EmployeeID, YEAR(r.PeriodTo) * 100 + MONTH(r.PeriodTo)
    ORDER BY l.EmployeeID, MonthKey;

    SELECT e.EmployeeID, e.DateOfBirth
    FROM mst.Employee e JOIN @Ids i ON i.EmployeeID = e.EmployeeID
    WHERE e.CompanyID = @CompanyID;
END
GO
