/* 0700_rpt_payroll_procs.sql
   Payroll report data. Brave lane 0700-0799. Needs schema rpt, created by Sigma in 0014 (ADR 0012). CREATE OR ALTER keeps it idempotent.
   Read-only: these procedures only read. They hold no tables and change nothing, so the rpt guard can check them.
   The company always comes from SESSION_CONTEXT. No company in the session means no data.
   Payslip result set 1 returns UAN, ESI number and bank account as stored: ciphertext and last four characters, with key versions.
   The application decrypts them, writes one reveal log entry, and hands the helper a temporary file. Nothing is decrypted here.
   A report on a run below Approved (StatusID under 4) must show a DRAFT mark. The status is in every first result set.
   Error numbers: 50002 no company in session, 50311 run not found. */
SET XACT_ABORT ON;
GO
/* Payslip. Result set 1: one row per employee. Result set 2: earnings per employee. @EmployeeID NULL means the whole run. */
CREATE OR ALTER PROCEDURE rpt.usp_Payslip_Get @RunID INT, @EmployeeID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID) THROW 50311, N'Run not found.', 1;
    SELECT r.RunID, r.StatusID AS RunStatusID, r.PeriodFrom, r.PeriodTo,
           ISNULL(cp.LegalName, c.CompanyName) AS CompanyName, cp.AddressLine1 AS CompanyAddress1, cp.AddressLine2 AS CompanyAddress2,
           f.FactoryName, f.EpfSubCode, f.EsiSubCode,
           e.EmployeeID, e.EmpCode, e.FullName, e.FatherOrSpouseName, e.JoinDate,
           es.PfNumber, es.UanLast4, es.UanEnc, es.EsiIpLast4, es.EsiIpEnc, es.IdKeyVersion AS StatutoryKeyVersion,
           bk.BankName, eb.Ifsc, eb.AccountLast4, eb.AccountEnc, eb.IdKeyVersion AS BankKeyVersion,
           d.DesignationName, dp.DepartmentName,
           l.PaidDays, l.OvertimeHours, l.OvertimeAmount, l.GrossEarnings,
           l.EpfEmployee, l.EsiEmployee, l.ProfessionalTax, l.LoanEmi, l.OtherDeductions, l.CarryForwardIn,
           l.TotalDeductions, l.NetPay, l.LoanEmiDeferred, l.CarryForwardOut
    FROM pay.PayrollRun r
    JOIN pay.PayrollRunLine l ON l.CompanyID = r.CompanyID AND l.RunID = r.RunID
    JOIN mst.Employee e ON e.CompanyID = l.CompanyID AND e.EmployeeID = l.EmployeeID
    JOIN mst.Company c ON c.CompanyID = r.CompanyID
    LEFT JOIN mst.CompanyProfile cp ON cp.CompanyID = r.CompanyID
    JOIN mst.Factory f ON f.CompanyID = l.CompanyID AND f.FactoryID = l.FactoryID
    LEFT JOIN mst.Designation d ON d.CompanyID = e.CompanyID AND d.DesignationID = e.DesignationID
    LEFT JOIN mst.Department dp ON dp.CompanyID = e.CompanyID AND dp.DepartmentID = e.DepartmentID
    LEFT JOIN mst.EmployeeStatutory es ON es.CompanyID = e.CompanyID AND es.EmployeeID = e.EmployeeID
    LEFT JOIN mst.EmployeeBank eb ON eb.CompanyID = e.CompanyID AND eb.EmployeeID = e.EmployeeID
    LEFT JOIN mst.Bank bk ON bk.BankID = eb.BankID
    WHERE r.CompanyID = @CompanyID AND r.RunID = @RunID AND (@EmployeeID IS NULL OR l.EmployeeID = @EmployeeID)
    ORDER BY f.FactoryName, e.EmpCode;

    SELECT x.EmployeeID, sc.Code AS ComponentCode, sc.Name AS ComponentName, sc.SortOrder, x.Amount
    FROM pay.PayrollRunEarning x
    JOIN pay.SalaryComponent sc ON sc.CompanyID = x.CompanyID AND sc.ComponentID = x.ComponentID
    WHERE x.CompanyID = @CompanyID AND x.RunID = @RunID AND (@EmployeeID IS NULL OR x.EmployeeID = @EmployeeID)
    ORDER BY x.EmployeeID, sc.SortOrder, sc.Code;
END
GO
/* Wage register: one row per employee with every amount of the run, grouped by factory. */
CREATE OR ALTER PROCEDURE rpt.usp_WageRegister_Get @RunID INT, @FactoryID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID) THROW 50311, N'Run not found.', 1;
    SELECT r.RunID, r.StatusID AS RunStatusID, r.PeriodFrom, r.PeriodTo,
           ISNULL(cp.LegalName, c.CompanyName) AS CompanyName,
           f.FactoryID, f.FactoryName,
           e.EmployeeID, e.EmpCode, e.FullName, d.DesignationName, e.PayTypeID,
           l.PaidDays, l.OvertimeHours, l.OvertimeAmount, l.GrossEarnings, l.EpfWage, l.EsiWage,
           l.EpfEmployee, l.EpfEmployerEpf, l.EpfEmployerEps, l.Edli, l.EpfAdmin, l.EsiEmployee, l.EsiEmployer,
           l.ProfessionalTax, l.LoanEmi, l.LoanEmiDeferred, l.OtherDeductions, l.CarryForwardIn, l.CarryForwardOut,
           l.TotalDeductions, l.NetPay
    FROM pay.PayrollRun r
    JOIN pay.PayrollRunLine l ON l.CompanyID = r.CompanyID AND l.RunID = r.RunID
    JOIN mst.Employee e ON e.CompanyID = l.CompanyID AND e.EmployeeID = l.EmployeeID
    JOIN mst.Company c ON c.CompanyID = r.CompanyID
    LEFT JOIN mst.CompanyProfile cp ON cp.CompanyID = r.CompanyID
    JOIN mst.Factory f ON f.CompanyID = l.CompanyID AND f.FactoryID = l.FactoryID
    LEFT JOIN mst.Designation d ON d.CompanyID = e.CompanyID AND d.DesignationID = e.DesignationID
    WHERE r.CompanyID = @CompanyID AND r.RunID = @RunID AND (@FactoryID IS NULL OR l.FactoryID = @FactoryID)
    ORDER BY f.FactoryName, e.EmpCode;
END
GO
/* Run summary: totals for each factory and for the run. Result set 1 per factory. Result set 2 the run total. */
CREATE OR ALTER PROCEDURE rpt.usp_PayrollSummary_Get @RunID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID) THROW 50311, N'Run not found.', 1;
    SELECT f.FactoryID, f.FactoryName, COUNT(*) AS EmployeeCount,
           SUM(l.GrossEarnings) AS GrossEarnings, SUM(l.EpfEmployee) AS EpfEmployee, SUM(l.EpfEmployerEpf + l.EpfEmployerEps) AS EpfEmployer,
           SUM(l.Edli) AS Edli, SUM(l.EpfAdmin) AS EpfAdmin, SUM(l.EsiEmployee) AS EsiEmployee, SUM(l.EsiEmployer) AS EsiEmployer,
           SUM(l.ProfessionalTax) AS ProfessionalTax, SUM(l.LoanEmi) AS LoanEmi, SUM(l.OtherDeductions) AS OtherDeductions,
           SUM(l.TotalDeductions) AS TotalDeductions, SUM(l.NetPay) AS NetPay
    FROM pay.PayrollRunLine l
    JOIN mst.Factory f ON f.CompanyID = l.CompanyID AND f.FactoryID = l.FactoryID
    WHERE l.CompanyID = @CompanyID AND l.RunID = @RunID
    GROUP BY f.FactoryID, f.FactoryName
    ORDER BY f.FactoryName;

    SELECT r.RunID, r.StatusID AS RunStatusID, r.PeriodFrom, r.PeriodTo, ISNULL(cp.LegalName, c.CompanyName) AS CompanyName,
           r.EmployeeCount, r.TotalGross, r.TotalDeductions, r.TotalNet
    FROM pay.PayrollRun r
    JOIN mst.Company c ON c.CompanyID = r.CompanyID
    LEFT JOIN mst.CompanyProfile cp ON cp.CompanyID = r.CompanyID
    WHERE r.CompanyID = @CompanyID AND r.RunID = @RunID;
END
GO
