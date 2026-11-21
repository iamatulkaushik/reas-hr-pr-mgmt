/* 0348_pay_leave_present_monthly_attendance.sql
   ADR 0026. Brave lane 0300-0399. Idempotent (CREATE OR ALTER). Never edit after merge. Run after 0333 and 0213.
   usp_LeaveAccrual_Present (defined in 0333, no later definition): the days present of the month now come from att.fn_MonthAttendance, so a company on monthly
   attendance accrues leave from its monthly rows and a company on daily attendance from the daily rows, with the same rule as before (present 1, half day 0.5;
   weekly off, holiday and paid leave do not count). The accrual month is always a whole calendar month (DAY = 1 is checked below). */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_LeaveAccrual_Present
    @LeaveTypeID INT, @AccrualMonth DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.LeaveType WHERE CompanyID = @CompanyID AND LeaveTypeID = @LeaveTypeID) THROW 53200, N'Leave type not found.', 1;
    IF DAY(@AccrualMonth) <> 1 THROW 53201, N'The accrual values are not valid.', 1;
    DECLARE @MonthEnd DATE = EOMONTH(@AccrualMonth);
    SELECT e.EmployeeID,
           CAST(ISNULL(a.PresentDays, 0) AS DECIMAL(7,2)) AS PresentDays,
           CAST(ISNULL(b.Balance, 0) AS DECIMAL(8,2)) AS Balance
    FROM mst.Employee e
    OUTER APPLY (SELECT CAST(m.PresentDays + m.HalfDays * 0.5 AS DECIMAL(7,2)) AS PresentDays
                 FROM att.fn_MonthAttendance(@CompanyID, @AccrualMonth) m
                 WHERE m.EmployeeID = e.EmployeeID) a
    OUTER APPLY (SELECT SUM(l.Days) AS Balance
                 FROM pay.LeaveLedger l
                 WHERE l.CompanyID = e.CompanyID AND l.EmployeeID = e.EmployeeID AND l.LeaveTypeID = @LeaveTypeID) b
    WHERE e.CompanyID = @CompanyID AND e.JoinDate <= @MonthEnd AND (e.ExitDate IS NULL OR e.ExitDate >= @AccrualMonth)
      AND (e.IsActive = 1 OR e.ExitDate IS NOT NULL)
    ORDER BY e.EmployeeID;
END
GO
