/* 0701_rpt_payroll_tds.sql
   Change request 0019, accepted by Sigma 2026-10-28. Sigma writes this in the 0700-0799 lane. Idempotent (CREATE OR ALTER). Never edit after merge.
   Needs 0323, which adds pay.PayrollRunLine.TdsAmount. Apply after it.
   Replaces the three procedures of 0700 with the same text plus the income tax deducted in the pay (TdsAmount):
   rpt.usp_Payslip_Get result set 1 and rpt.usp_WageRegister_Get have l.TdsAmount after ProfessionalTax; rpt.usp_PayrollSummary_Get has SUM(l.TdsAmount) AS TdsAmount after ProfessionalTax.
   Nothing else changes: same parameters, same other columns, same order, same errors (50002 no company in session, 50311 run not found). Read-only.
   The column is 0 for every line written before 0323 and for a company without the Tds plan feature. The app decides whether the line shows (ShowTds report parameter), not this data. */
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
           l.EpfEmployee, l.EsiEmployee, l.ProfessionalTax, l.TdsAmount, l.LoanEmi, l.OtherDeductions, l.CarryForwardIn,
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
           l.ProfessionalTax, l.TdsAmount, l.LoanEmi, l.LoanEmiDeferred, l.OtherDeductions, l.CarryForwardIn, l.CarryForwardOut,
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
           SUM(l.ProfessionalTax) AS ProfessionalTax, SUM(l.TdsAmount) AS TdsAmount, SUM(l.LoanEmi) AS LoanEmi, SUM(l.OtherDeductions) AS OtherDeductions,
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
