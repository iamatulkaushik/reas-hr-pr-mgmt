/* 0341_pay_bonus_procs.sql
   Bonus run procedures (step 7f). Brave lane 0300-0399 (taken over by Sigma, 2026-11-04). CREATE OR ALTER keeps them idempotent. The company always comes from SESSION_CONTEXT.
   pay.usp_Bonus_Facts @YearFrom: three result sets for the accounting year that starts on @YearFrom.
     1. One row per employee and month of the regular runs (RunNo 1) that are Locked or Paid with a period end in the year: EmployeeID, EmpCode, FactoryID, MonthKey (yyyymm of PeriodTo),
        BasicPlusDa (Basic plus DA earned in that month) and PaidDays.
     2. What each employee was already paid for the year: the gross of the lines of the bonus runs of that year, and any bonus paid in full and final for an exit date in the year.
     3. One row per employee of the first set: IsEpfMember, EpfOnHigherWage, EpsOnHigherWage (the bonus run carries provident fund, Owner 2026-11-04).   pay.usp_BonusRun_Register marks an off-cycle run as the bonus run of the year. Error numbers: 50002 no company in session, 53120 the run is not found or is not an off-cycle run,
   53121 the run is already a bonus run, 53122 the accounting year must start on the 1st of April. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_Bonus_Facts
    @YearFrom DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF DAY(@YearFrom) <> 1 OR MONTH(@YearFrom) <> 4 THROW 53122, N'The accounting year must start on the 1st of April.', 1;
    DECLARE @YearTo DATE = DATEADD(DAY, -1, DATEADD(YEAR, 1, @YearFrom));
    SELECT l.EmployeeID, e.EmpCode, l.FactoryID, YEAR(r.PeriodTo) * 100 + MONTH(r.PeriodTo) AS MonthKey,
           ISNULL(x.BasicDa, 0) AS BasicPlusDa, l.PaidDays
    FROM pay.PayrollRunLine l
    JOIN pay.PayrollRun r ON r.CompanyID = l.CompanyID AND r.RunID = l.RunID
    JOIN mst.Employee e ON e.CompanyID = l.CompanyID AND e.EmployeeID = l.EmployeeID
    OUTER APPLY (SELECT SUM(pe.Amount) AS BasicDa FROM pay.PayrollRunEarning pe
                 JOIN pay.SalaryComponent c ON c.CompanyID = pe.CompanyID AND c.ComponentID = pe.ComponentID
                 WHERE pe.CompanyID = l.CompanyID AND pe.RunID = l.RunID AND pe.EmployeeID = l.EmployeeID AND (c.IsBasic = 1 OR c.IsDa = 1)) x
    WHERE l.CompanyID = @CompanyID AND r.RunNo = 1 AND r.StatusID IN (5, 6) AND r.PeriodTo BETWEEN @YearFrom AND @YearTo
    ORDER BY l.EmployeeID, MonthKey;

    SELECT p.EmployeeID, SUM(p.Amount) AS Amount
    FROM (SELECT l.EmployeeID, l.GrossEarnings AS Amount
          FROM pay.BonusRun b
          JOIN pay.PayrollRunLine l ON l.CompanyID = b.CompanyID AND l.RunID = b.RunID
          WHERE b.CompanyID = @CompanyID AND b.YearFrom = @YearFrom
          UNION ALL
          SELECT s.EmployeeID, c.Amount
          FROM pay.FnfSettlement s
          JOIN pay.FnfComponent c ON c.CompanyID = s.CompanyID AND c.FnfID = s.FnfID AND c.ComponentTypeID = 3
          WHERE s.CompanyID = @CompanyID AND s.ExitDate BETWEEN @YearFrom AND @YearTo) p
    GROUP BY p.EmployeeID;

    SELECT e.EmployeeID,
           CAST(ISNULL(st.IsEpfMember, 0) AS BIT) AS IsEpfMember,
           CAST(ISNULL(st.EpfOnHigherWage, 0) AS BIT) AS EpfOnHigherWage,
           CAST(ISNULL(st.EpsOnHigherWage, 0) AS BIT) AS EpsOnHigherWage
    FROM mst.Employee e
    LEFT JOIN mst.EmployeeStatutory st ON st.CompanyID = e.CompanyID AND st.EmployeeID = e.EmployeeID
    WHERE e.CompanyID = @CompanyID
      AND EXISTS (SELECT 1 FROM pay.PayrollRunLine l JOIN pay.PayrollRun r ON r.CompanyID = l.CompanyID AND r.RunID = l.RunID
                  WHERE l.CompanyID = e.CompanyID AND l.EmployeeID = e.EmployeeID AND r.RunNo = 1 AND r.StatusID IN (5, 6) AND r.PeriodTo BETWEEN @YearFrom AND @YearTo)
    ORDER BY e.EmployeeID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_BonusRun_Register
    @RunID INT, @YearFrom DATE, @Pct DECIMAL(5,2), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF DAY(@YearFrom) <> 1 OR MONTH(@YearFrom) <> 4 THROW 53122, N'The accounting year must start on the 1st of April.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID AND RunNo > 1)
        THROW 53120, N'The bonus run is not found, or it is not an off-cycle run.', 1;
    IF EXISTS (SELECT 1 FROM pay.BonusRun WHERE CompanyID = @CompanyID AND RunID = @RunID) THROW 53121, N'The run is already a bonus run.', 1;
    INSERT pay.BonusRun (RunID, CompanyID, YearFrom, Pct, CreatedByUserID) VALUES (@RunID, @CompanyID, @YearFrom, @Pct, @UserID);
END
GO
