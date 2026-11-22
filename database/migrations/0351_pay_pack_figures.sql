/* 0351_pay_pack_figures.sql
   ADR 0027, work order docs/phase7b-plan-sigma.md. Brave lane 0300-0399. Idempotent (CREATE OR ALTER). Never edit after merge. Needs 0303, 0349 and 0408.
   pay.usp_Pack_Figures @PeriodStart: what the Monthly Pack needs from the month's payroll, in four result sets:
     1. the company (CompanyName);
     2. the run of the month, none when there is no run: RunID, StatusID, PeriodFrom, PeriodTo. A run belongs to the month when its period ends in it. An approved,
        locked or paid run (StatusID 4 to 6) is chosen before an earlier stage; then the highest run number;
     3. the totals of that run's lines, all zero when the run is not yet approved (figures come only from an approved or locked run, ADR 0027 point 5):
        EPF shares (employee, employer EPF, employer EPS, EDLI, admin), the employees with an EPF share, ESIC shares (employee, employer), the employees with an ESIC share;
     4. professional tax by state of the employee's factory, only states with an amount: StateID, StateName, Amount, EmployeeCount.
   pay.usp_Pack_LwfStates @PeriodStart: the states of the company's active factories that have a labour welfare rule in force whose due months include the month: StateID, StateName, Verified. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_Pack_Figures @PeriodStart DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @PeriodStart IS NULL OR DAY(@PeriodStart) <> 1 THROW 53650, N'The month must start on the first day.', 1;
    DECLARE @RunID INT, @Status TINYINT, @From DATE, @To DATE;
    SELECT TOP (1) @RunID = r.RunID, @Status = r.StatusID, @From = r.PeriodFrom, @To = r.PeriodTo
    FROM pay.PayrollRun r
    WHERE r.CompanyID = @CompanyID AND r.PeriodTo >= @PeriodStart AND r.PeriodTo <= EOMONTH(@PeriodStart)
    ORDER BY CASE WHEN r.StatusID >= 4 THEN 0 ELSE 1 END, r.RunNo DESC;
    DECLARE @Approved BIT = CASE WHEN @RunID IS NOT NULL AND @Status >= 4 THEN 1 ELSE 0 END;

    SELECT c.CompanyName FROM mst.Company c WHERE c.CompanyID = @CompanyID;

    SELECT @RunID AS RunID, @Status AS StatusID, @From AS PeriodFrom, @To AS PeriodTo WHERE @RunID IS NOT NULL;

    SELECT CAST(ISNULL(SUM(l.EpfEmployee), 0) AS DECIMAL(14,2)) AS EpfEmployee,
           CAST(ISNULL(SUM(l.EpfEmployerEpf), 0) AS DECIMAL(14,2)) AS EpfEmployerEpf,
           CAST(ISNULL(SUM(l.EpfEmployerEps), 0) AS DECIMAL(14,2)) AS EpfEmployerEps,
           CAST(ISNULL(SUM(l.Edli), 0) AS DECIMAL(14,2)) AS Edli,
           CAST(ISNULL(SUM(l.EpfAdmin), 0) AS DECIMAL(14,2)) AS EpfAdmin,
           CAST(ISNULL(SUM(CASE WHEN l.EpfEmployee + l.EpfEmployerEpf + l.EpfEmployerEps > 0 THEN 1 ELSE 0 END), 0) AS INT) AS EpfCount,
           CAST(ISNULL(SUM(l.EsiEmployee), 0) AS DECIMAL(14,2)) AS EsiEmployee,
           CAST(ISNULL(SUM(l.EsiEmployer), 0) AS DECIMAL(14,2)) AS EsiEmployer,
           CAST(ISNULL(SUM(CASE WHEN l.EsiEmployee + l.EsiEmployer > 0 THEN 1 ELSE 0 END), 0) AS INT) AS EsiCount
    FROM pay.PayrollRunLine l
    WHERE l.CompanyID = @CompanyID AND l.RunID = @RunID AND @Approved = 1;

    SELECT f.StateID, s.StateName,
           CAST(SUM(l.ProfessionalTax) AS DECIMAL(14,2)) AS Amount,
           CAST(SUM(CASE WHEN l.ProfessionalTax > 0 THEN 1 ELSE 0 END) AS INT) AS EmployeeCount
    FROM pay.PayrollRunLine l
    JOIN mst.Factory f ON f.CompanyID = l.CompanyID AND f.FactoryID = l.FactoryID
    JOIN mst.[State] s ON s.StateID = f.StateID
    WHERE l.CompanyID = @CompanyID AND l.RunID = @RunID AND @Approved = 1
    GROUP BY f.StateID, s.StateName
    HAVING SUM(l.ProfessionalTax) > 0
    ORDER BY s.StateName;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Pack_LwfStates @PeriodStart DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @PeriodStart IS NULL OR DAY(@PeriodStart) <> 1 THROW 53650, N'The month must start on the first day.', 1;
    SELECT st.StateID, st.StateName, CAST(lw.Verified AS BIT) AS Verified
    FROM (SELECT DISTINCT f.StateID FROM mst.Factory f WHERE f.CompanyID = @CompanyID AND f.IsActive = 1) fs
    JOIN mst.[State] st ON st.StateID = fs.StateID
    CROSS APPLY (SELECT TOP (1) r.Verified, r.DueMonths
                 FROM stat.LabourWelfareRule r
                 WHERE r.StateID = fs.StateID AND r.EffectiveFrom <= EOMONTH(@PeriodStart)
                 ORDER BY r.EffectiveFrom DESC) lw
    WHERE N',' + lw.DueMonths + N',' LIKE N'%,' + CAST(MONTH(@PeriodStart) AS NVARCHAR(2)) + N',%'
    ORDER BY st.StateName;
END
GO
