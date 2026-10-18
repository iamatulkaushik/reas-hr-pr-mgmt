/* 0208_att_screen_procs.sql
   Read procedures for the attendance screens. CREATE OR ALTER keeps it idempotent. Read only: no writes, no transaction.
   Tenant rows come from the row-level security policy on mst.Employee and the att tables, so no company parameter exists.
   An employee is on the sheet when active and the date falls between join date and exit date. */
SET XACT_ABORT ON;
GO
/* One row per employee on the day, marked or not. StatusID is NULL when the day is not marked yet. */
CREATE OR ALTER PROCEDURE att.usp_Attendance_ListDay @WorkDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT e.EmployeeID, e.EmpCode, e.FullName, a.StatusID, a.ShiftID,
           ISNULL(a.WorkedMinutes, 0) AS WorkedMinutes, ISNULL(a.OvertimeMinutes, 0) AS OvertimeMinutes
    FROM mst.Employee e
    LEFT JOIN att.AttendanceDaily a ON a.EmployeeID = e.EmployeeID AND a.WorkDate = @WorkDate
    WHERE e.IsActive = 1 AND e.JoinDate <= @WorkDate AND (e.ExitDate IS NULL OR e.ExitDate >= @WorkDate)
    ORDER BY e.EmpCode;
END
GO
/* Marked days of every employee on the roll during the month. Used by month close. */
CREATE OR ALTER PROCEDURE att.usp_Attendance_ListMonthAll @Year INT, @Month INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @First DATE = DATEFROMPARTS(@Year, @Month, 1);
    DECLARE @Next DATE = DATEADD(MONTH, 1, @First);
    SELECT e.EmployeeID, e.EmpCode, e.FullName, a.WorkDate, a.StatusID, a.OvertimeMinutes
    FROM mst.Employee e
    LEFT JOIN att.AttendanceDaily a ON a.EmployeeID = e.EmployeeID AND a.WorkDate >= @First AND a.WorkDate < @Next
    WHERE e.IsActive = 1 AND e.JoinDate < @Next AND (e.ExitDate IS NULL OR e.ExitDate >= @First)
    ORDER BY e.EmpCode, a.WorkDate;
END
GO
/* Close and reopen history of one month, newest first. */
CREATE OR ALTER PROCEDURE att.usp_Month_ListEvents @MonthKey INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT EventType, UserID, Reason, EventUtc FROM att.MonthLockEvent WHERE MonthKey = @MonthKey ORDER BY EventID DESC;
END
GO
