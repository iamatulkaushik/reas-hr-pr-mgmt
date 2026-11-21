/* 0213_att_fn_month_attendance.sql
   The one seam for payroll and leave accrual (ADR 0026): att.fn_MonthAttendance(@CompanyID, @PeriodStart) returns, per employee, the month's counts from the monthly
   rows when the company month has monthly rows, and otherwise from att.AttendanceDaily. One source per company month; usp_AttendanceMonthly_Save and the patched
   usp_Attendance_Save (0214) keep the two apart. @PeriodStart is the first of the month.
   OvertimeMinutes: daily rows summed; monthly rows are hours times 60, rounded to a whole minute.
   UnmarkedDays: daily, the days of the month without a row (as MonthSummaryBuilder); monthly, the days of employment in the month that the counts do not cover. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER FUNCTION att.fn_MonthAttendance (@CompanyID INT, @PeriodStart DATE)
RETURNS TABLE
AS
RETURN
(
    SELECT a.EmployeeID,
           CAST(a.PresentDays AS INT) AS PresentDays, CAST(a.HalfDays AS INT) AS HalfDays, CAST(a.WeeklyOffDays AS INT) AS WeeklyOffDays,
           CAST(a.HolidayDays AS INT) AS HolidayDays, CAST(a.PaidLeaveDays AS INT) AS PaidLeaveDays, CAST(a.AbsentDays AS INT) AS AbsentDays,
           CAST(ROUND(a.OvertimeHours * 60, 0) AS INT) AS OvertimeMinutes,
           CAST(CASE WHEN w.WindowDays - (a.PresentDays + a.HalfDays + a.WeeklyOffDays + a.HolidayDays + a.PaidLeaveDays + a.AbsentDays) > 0
                     THEN w.WindowDays - (a.PresentDays + a.HalfDays + a.WeeklyOffDays + a.HolidayDays + a.PaidLeaveDays + a.AbsentDays) ELSE 0 END AS INT) AS UnmarkedDays
    FROM att.AttendanceMonthly a
    JOIN mst.Employee e ON e.CompanyID = a.CompanyID AND e.EmployeeID = a.EmployeeID
    CROSS APPLY (SELECT CASE WHEN e.JoinDate > @PeriodStart THEN e.JoinDate ELSE @PeriodStart END AS WStart,
                        CASE WHEN e.ExitDate IS NOT NULL AND e.ExitDate < EOMONTH(@PeriodStart) THEN e.ExitDate ELSE EOMONTH(@PeriodStart) END AS WEnd) x
    CROSS APPLY (SELECT CASE WHEN x.WStart <= x.WEnd THEN DATEDIFF(DAY, x.WStart, x.WEnd) + 1 ELSE 0 END AS WindowDays) w
    WHERE a.CompanyID = @CompanyID AND a.PeriodStart = @PeriodStart
    UNION ALL
    SELECT d.EmployeeID,
           CAST(SUM(CASE WHEN d.StatusID = 1 THEN 1 ELSE 0 END) AS INT),
           CAST(SUM(CASE WHEN d.StatusID = 3 THEN 1 ELSE 0 END) AS INT),
           CAST(SUM(CASE WHEN d.StatusID = 4 THEN 1 ELSE 0 END) AS INT),
           CAST(SUM(CASE WHEN d.StatusID = 5 THEN 1 ELSE 0 END) AS INT),
           CAST(SUM(CASE WHEN d.StatusID = 6 THEN 1 ELSE 0 END) AS INT),
           CAST(SUM(CASE WHEN d.StatusID = 2 THEN 1 ELSE 0 END) AS INT),
           CAST(SUM(CAST(d.OvertimeMinutes AS INT)) AS INT),
           CAST(DAY(EOMONTH(@PeriodStart)) - COUNT(*) AS INT)
    FROM att.AttendanceDaily d
    WHERE d.CompanyID = @CompanyID AND d.WorkDate >= @PeriodStart AND d.WorkDate <= EOMONTH(@PeriodStart)
      AND NOT EXISTS (SELECT 1 FROM att.AttendanceMonthly m WHERE m.CompanyID = @CompanyID AND m.PeriodStart = @PeriodStart)
    GROUP BY d.EmployeeID
);
GO
