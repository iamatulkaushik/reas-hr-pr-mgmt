/* 0342_pay_run_tax_facts_no_run.sql
   Bonus run (step 7g). Brave lane 0300-0399 (taken over by Sigma, 2026-11-04). Idempotent (CREATE OR ALTER). Never edit after merge.
   usp_RunTax_Facts is the 0324 procedure with one change: @RunID = 0 means no run is left out, so the tax on a bonus can be worked out before its run is made (preview and make).
   Every other line is as in 0324. Errors: 50002 no company in session, 50311 run not found (not raised for 0). */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_RunTax_Facts @RunID INT, @FinancialYear INT, @EmployeeIdsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @RunID <> 0 AND NOT EXISTS (SELECT 1 FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID) THROW 50311, N'Run not found.', 1;
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
