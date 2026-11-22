/* 0720_rpt_report_screens.sql
   Rows for the Reports menu screens (ADR 0028): the run list, the Basic Report and the Muster Roll. Read-only, in schema rpt (created in 0014/0016).
   The company always comes from SESSION_CONTEXT. No company in the session means error 50002 and no data.
   Salary Sheet, Wage Register and Abstract use the procedures of 0700 and 0701. */
SET XACT_ABORT ON;
GO
/* Payroll runs of the company, newest first, for the run drop-down. RunStatusID below 4 (Approved) prints a DRAFT mark. */
CREATE OR ALTER PROCEDURE rpt.usp_RunList_Get
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT r.RunID, r.StatusID AS RunStatusID, r.PeriodFrom, r.PeriodTo, ISNULL(cp.LegalName, c.CompanyName) AS CompanyName,
           r.EmployeeCount, r.TotalNet
    FROM pay.PayrollRun r
    JOIN mst.Company c ON c.CompanyID = r.CompanyID
    LEFT JOIN mst.CompanyProfile cp ON cp.CompanyID = r.CompanyID
    WHERE r.CompanyID = @CompanyID
    ORDER BY r.PeriodFrom DESC, r.RunID DESC;
END
GO
/* Basic Report: one row per employee, grouped by factory in the layout. By default only active employees who have not left. */
CREATE OR ALTER PROCEDURE rpt.usp_EmployeeBasic_Get @IncludeExited BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT ISNULL(cp.LegalName, c.CompanyName) AS CompanyName, f.FactoryID, f.FactoryName,
           e.EmployeeID, e.EmpCode, e.FullName, e.FatherOrSpouseName, e.Gender, e.DateOfBirth, e.JoinDate, e.ExitDate,
           d.DesignationName, dp.DepartmentName, e.Mobile, e.IsActive
    FROM mst.Employee e
    JOIN mst.Company c ON c.CompanyID = e.CompanyID
    LEFT JOIN mst.CompanyProfile cp ON cp.CompanyID = e.CompanyID
    JOIN mst.Factory f ON f.CompanyID = e.CompanyID AND f.FactoryID = e.FactoryID
    LEFT JOIN mst.Designation d ON d.CompanyID = e.CompanyID AND d.DesignationID = e.DesignationID
    LEFT JOIN mst.Department dp ON dp.CompanyID = e.CompanyID AND dp.DepartmentID = e.DepartmentID
    WHERE e.CompanyID = @CompanyID AND (@IncludeExited = 1 OR (e.IsActive = 1 AND e.ExitDate IS NULL))
    ORDER BY f.FactoryName, e.EmpCode;
END
GO
/* Muster Roll for one month: every employee employed in the month, with the monthly attendance counters (NULL when none is entered yet).
   The day cells of the layout are blank for hand marking. */
CREATE OR ALTER PROCEDURE rpt.usp_Muster_Get @PeriodStart DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SET @PeriodStart = DATEFROMPARTS(YEAR(@PeriodStart), MONTH(@PeriodStart), 1);
    DECLARE @PeriodEnd DATE = EOMONTH(@PeriodStart);
    SELECT ISNULL(cp.LegalName, c.CompanyName) AS CompanyName, @PeriodStart AS PeriodStart, f.FactoryID, f.FactoryName,
           e.EmployeeID, e.EmpCode, e.FullName, d.DesignationName,
           a.PresentDays, a.HalfDays, a.WeeklyOffDays, a.HolidayDays, a.PaidLeaveDays, a.AbsentDays, a.OvertimeHours, a.Remarks
    FROM mst.Employee e
    JOIN mst.Company c ON c.CompanyID = e.CompanyID
    LEFT JOIN mst.CompanyProfile cp ON cp.CompanyID = e.CompanyID
    JOIN mst.Factory f ON f.CompanyID = e.CompanyID AND f.FactoryID = e.FactoryID
    LEFT JOIN mst.Designation d ON d.CompanyID = e.CompanyID AND d.DesignationID = e.DesignationID
    LEFT JOIN att.AttendanceMonthly a ON a.CompanyID = e.CompanyID AND a.EmployeeID = e.EmployeeID AND a.PeriodStart = @PeriodStart
    WHERE e.CompanyID = @CompanyID AND e.JoinDate <= @PeriodEnd AND (e.ExitDate IS NULL OR e.ExitDate >= @PeriodStart)
    ORDER BY f.FactoryName, e.EmpCode;
END
GO
