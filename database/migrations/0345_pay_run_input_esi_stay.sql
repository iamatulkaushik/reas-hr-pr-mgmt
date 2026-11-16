/* 0345_pay_run_input_esi_stay.sql
   Change request 0031. Brave lane 0300-0399. Idempotent (CREATE OR ALTER). Never edit after merge.
   usp_RunInput_List: the 0344 procedure with EsiStayEligible added to the first result set, right after EsiHigherCeiling.
   EsiStayEligible is 1 for an ESI member who already paid ESI (EsiEmployer above 0) on a Locked or Paid regular run (RunNo 1, arrears runs left out)
   of this company earlier in the same ESI contribution period. Periods: 1 April to 30 September and 1 October to 31 March, taken from the month of @PeriodTo.
   An employee who has no earlier covered line in the period (first month, or joined mid-period) is tested on the wage as before. Run after 0344. */
CREATE OR ALTER PROCEDURE pay.usp_RunInput_List @PeriodFrom DATE, @PeriodTo DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @PeriodTo < @PeriodFrom OR DATEDIFF(DAY, @PeriodFrom, @PeriodTo) > 30 THROW 50319, N'The period dates are not valid.', 1;
    DECLARE @CpStart DATE = CASE WHEN MONTH(@PeriodTo) BETWEEN 4 AND 9 THEN DATEFROMPARTS(YEAR(@PeriodTo), 4, 1)
                                 WHEN MONTH(@PeriodTo) >= 10 THEN DATEFROMPARTS(YEAR(@PeriodTo), 10, 1)
                                 ELSE DATEFROMPARTS(YEAR(@PeriodTo) - 1, 10, 1) END;

    SELECT e.EmployeeID, e.FactoryID, e.EmpCode,
           CAST(CASE WHEN e.PayTypeID = 1 THEN 1 ELSE 0 END AS BIT) AS IsDailyWage,
           CAST(CASE e.Gender WHEN 'M' THEN 1 WHEN 'F' THEN 2 ELSE 0 END AS INT) AS GenderID,
           CAST(CASE WHEN ISNULL(s.PtApplicable, 1) = 1 THEN f.StateID END AS INT) AS PtStateID,
           CAST(ISNULL(s.IsEpfMember, 0) AS BIT) AS IsEpfMember,
           CAST(ISNULL(s.EpfOnHigherWage, 0) AS BIT) AS EpfOnHigherWage,
           CAST(ISNULL(s.EpsOnHigherWage, 0) AS BIT) AS EpsOnHigherWage,
           CAST(ISNULL(s.IsEsiMember, 0) AS BIT) AS IsEsiMember,
           CAST(ISNULL(s.EsiHigherCeiling, 0) AS BIT) AS EsiHigherCeiling,
           CAST(CASE WHEN ISNULL(s.IsEsiMember, 0) = 1 AND ec.HasCover = 1 THEN 1 ELSE 0 END AS BIT) AS EsiStayEligible,
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
    OUTER APPLY (SELECT TOP (1) CAST(1 AS BIT) AS HasCover
                 FROM pay.PayrollRunLine l2
                 JOIN pay.PayrollRun r2 ON r2.CompanyID = l2.CompanyID AND r2.RunID = l2.RunID
                 WHERE l2.CompanyID = e.CompanyID AND l2.EmployeeID = e.EmployeeID AND r2.StatusID IN (5, 6) AND r2.RunNo = 1
                   AND r2.PeriodFrom < @PeriodFrom AND r2.PeriodTo >= @CpStart AND l2.EsiEmployer > 0) ec
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
