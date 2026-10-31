/* 0325_pay_fnf_facts_proc.sql
   Phase 7 step 6d. Brave lane 0300-0399. Idempotent (CREATE OR ALTER). Never edit after merge.
   pay.usp_Fnf_Facts: what the database knows about one separation, in five result sets, read in one call for FnfService.Prepare. The company comes from SESSION_CONTEXT.
   1. The separation: employee, join and birth dates, employment type, reason, notice, last working day, and the company's daily wage divisor.
   2. The salary structure in force on the last working day: total gross and Basic plus DA (zero rows of salary give zeros).
   3. Loan outstanding: every installment of the employee's active loans less what runs have recovered.
   4. Bonus months: one row per month (yyyymm of PeriodTo) of the accounting year (April to March of the last working day) from Locked or Paid runs, the final run left out: Basic plus DA and paid days.
   5. The final run line: the latest run, at least Processed, whose period covers the last working day and that has a line for the employee: run id, month, net pay, gross, paid days, tax deducted, Basic plus DA, HRA.
      No row means no run covers the exit date.
   Errors: 50002 no company in session, 50354 separation not found. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_Fnf_Facts @SeparationID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @EmployeeID INT, @LastDay DATE;
    SELECT @EmployeeID = EmployeeID, @LastDay = LastWorkingDay FROM mst.EmployeeSeparation WHERE CompanyID = @CompanyID AND SeparationID = @SeparationID;
    IF @EmployeeID IS NULL THROW 50354, N'Separation not found.', 1;

    DECLARE @FinalRunID INT = (SELECT TOP (1) r.RunID FROM pay.PayrollRun r
                               JOIN pay.PayrollRunLine l ON l.CompanyID = r.CompanyID AND l.RunID = r.RunID AND l.EmployeeID = @EmployeeID
                               WHERE r.CompanyID = @CompanyID AND r.StatusID >= 2 AND @LastDay BETWEEN r.PeriodFrom AND r.PeriodTo
                               ORDER BY r.RunID DESC);
    DECLARE @YearFrom DATE = DATEFROMPARTS(YEAR(@LastDay) - CASE WHEN MONTH(@LastDay) < 4 THEN 1 ELSE 0 END, 4, 1);
    DECLARE @YearTo DATE = DATEADD(DAY, -1, DATEADD(YEAR, 1, @YearFrom));
    DECLARE @SalaryID INT = (SELECT TOP (1) SalaryID FROM pay.EmployeeSalary
                             WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND EffectiveFrom <= @LastDay ORDER BY EffectiveFrom DESC);

    SELECT e.EmployeeID, e.JoinDate, e.DateOfBirth, e.EmploymentTypeID, s.ReasonID, s.NoticeDate, s.LastWorkingDay, s.NoticePeriodDays,
           ISNULL((SELECT DailyWageDivisor FROM pay.CompanyPolicy WHERE CompanyID = @CompanyID), 26) AS DailyWageDivisor
    FROM mst.EmployeeSeparation s
    JOIN mst.Employee e ON e.CompanyID = s.CompanyID AND e.EmployeeID = s.EmployeeID
    WHERE s.CompanyID = @CompanyID AND s.SeparationID = @SeparationID;

    SELECT ISNULL(SUM(l.Amount), 0) AS MonthlyGross,
           ISNULL(SUM(CASE WHEN c.IsBasic = 1 OR c.IsDa = 1 THEN l.Amount ELSE 0 END), 0) AS MonthlyBasicPlusDa
    FROM pay.EmployeeSalaryLine l
    JOIN pay.SalaryComponent c ON c.CompanyID = l.CompanyID AND c.ComponentID = l.ComponentID
    WHERE l.CompanyID = @CompanyID AND l.SalaryID = @SalaryID;

    SELECT ISNULL(SUM(i.Amount), 0) - ISNULL(SUM(rec.Recovered), 0) AS LoanOutstanding
    FROM pay.Loan lo
    JOIN pay.LoanInstallment i ON i.CompanyID = lo.CompanyID AND i.LoanID = lo.LoanID
    OUTER APPLY (SELECT SUM(Amount) AS Recovered FROM pay.LoanRecovery r
                 WHERE r.CompanyID = i.CompanyID AND r.LoanID = i.LoanID AND r.InstallmentNo = i.InstallmentNo) rec
    WHERE lo.CompanyID = @CompanyID AND lo.EmployeeID = @EmployeeID AND lo.StatusID = 1;

    SELECT YEAR(r.PeriodTo) * 100 + MONTH(r.PeriodTo) AS MonthKey,
           SUM(ISNULL(x.BasicDa, 0)) AS BasicPlusDa,
           SUM(l.PaidDays)           AS PaidDays
    FROM pay.PayrollRunLine l
    JOIN pay.PayrollRun r ON r.CompanyID = l.CompanyID AND r.RunID = l.RunID
    OUTER APPLY (SELECT SUM(pe.Amount) AS BasicDa FROM pay.PayrollRunEarning pe
                 JOIN pay.SalaryComponent c ON c.CompanyID = pe.CompanyID AND c.ComponentID = pe.ComponentID
                 WHERE pe.CompanyID = l.CompanyID AND pe.RunID = l.RunID AND pe.EmployeeID = l.EmployeeID AND (c.IsBasic = 1 OR c.IsDa = 1)) x
    WHERE l.CompanyID = @CompanyID AND l.EmployeeID = @EmployeeID AND r.StatusID IN (5, 6)
          AND r.PeriodTo BETWEEN @YearFrom AND @YearTo AND r.RunID <> ISNULL(@FinalRunID, 0)
    GROUP BY YEAR(r.PeriodTo) * 100 + MONTH(r.PeriodTo)
    ORDER BY MonthKey;

    SELECT r.RunID, YEAR(r.PeriodTo) * 100 + MONTH(r.PeriodTo) AS MonthKey, l.NetPay, l.GrossEarnings, l.PaidDays, l.TdsAmount,
           ISNULL(x.BasicDa, 0) AS BasicPlusDa, ISNULL(x.Hra, 0) AS Hra
    FROM pay.PayrollRun r
    JOIN pay.PayrollRunLine l ON l.CompanyID = r.CompanyID AND l.RunID = r.RunID AND l.EmployeeID = @EmployeeID
    OUTER APPLY (SELECT SUM(CASE WHEN c.IsBasic = 1 OR c.IsDa = 1 THEN pe.Amount ELSE 0 END) AS BasicDa,
                        SUM(CASE WHEN c.IsHra = 1 THEN pe.Amount ELSE 0 END) AS Hra
                 FROM pay.PayrollRunEarning pe
                 JOIN pay.SalaryComponent c ON c.CompanyID = pe.CompanyID AND c.ComponentID = pe.ComponentID
                 WHERE pe.CompanyID = l.CompanyID AND pe.RunID = l.RunID AND pe.EmployeeID = l.EmployeeID) x
    WHERE r.CompanyID = @CompanyID AND r.RunID = @FinalRunID;
END
GO
