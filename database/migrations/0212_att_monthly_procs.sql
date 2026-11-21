/* 0212_att_monthly_procs.sql
   Procedures for monthly attendance (ADR 0026). CREATE OR ALTER keeps it idempotent. Needs 0105, 0204 (att.Holiday is not used here), 0210.
   Errors: 50001 month closed, 50002 no company in session, 53601 Daily mode needs Platinum, 53602 the month already has the other kind of rows,
   53603 employee not employed in the month, 53604 counts do not add up (only when a complete save is asked for), 53605 a value is out of range.
   The employee's window in a month runs from the later of the first of the month and the joining date to the earlier of the last of the month and the exit date. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE att.usp_AttendanceMonthly_Save
    @EmployeeID INT, @PeriodStart DATE,
    @PresentDays INT, @HalfDays INT, @WeeklyOffDays INT, @HolidayDays INT, @PaidLeaveDays INT, @AbsentDays INT,
    @OvertimeHours DECIMAL(5,2), @Remarks NVARCHAR(100) = NULL, @UserID INT = NULL, @RequireComplete BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @PeriodStart IS NULL OR DAY(@PeriodStart) <> 1 THROW 53605, N'The month must start on the first day.', 1;
    IF @PresentDays NOT BETWEEN 0 AND 31 OR @HalfDays NOT BETWEEN 0 AND 31 OR @WeeklyOffDays NOT BETWEEN 0 AND 31
       OR @HolidayDays NOT BETWEEN 0 AND 31 OR @PaidLeaveDays NOT BETWEEN 0 AND 31 OR @AbsentDays NOT BETWEEN 0 AND 31
       OR @OvertimeHours IS NULL OR @OvertimeHours NOT BETWEEN 0 AND 744
        THROW 53605, N'A day count is between 0 and 31 and overtime is up to 744 hours.', 1;
    IF EXISTS (SELECT 1 FROM att.MonthLock WHERE CompanyID = @CompanyID AND IsClosed = 1 AND MonthKey = YEAR(@PeriodStart) * 100 + MONTH(@PeriodStart))
        THROW 50001, N'The month is closed.', 1;
    IF EXISTS (SELECT 1 FROM att.AttendanceDaily WHERE CompanyID = @CompanyID AND WorkDate >= @PeriodStart AND WorkDate <= EOMONTH(@PeriodStart))
        THROW 53602, N'This month already has daily attendance. A month is either daily or monthly.', 1;
    DECLARE @WindowDays INT;
    SELECT @WindowDays = CASE WHEN w.WStart <= w.WEnd THEN DATEDIFF(DAY, w.WStart, w.WEnd) + 1 ELSE 0 END
    FROM mst.Employee e
    CROSS APPLY (SELECT CASE WHEN e.JoinDate > @PeriodStart THEN e.JoinDate ELSE @PeriodStart END AS WStart,
                        CASE WHEN e.ExitDate IS NOT NULL AND e.ExitDate < EOMONTH(@PeriodStart) THEN e.ExitDate ELSE EOMONTH(@PeriodStart) END AS WEnd) w
    WHERE e.CompanyID = @CompanyID AND e.EmployeeID = @EmployeeID;
    IF @WindowDays IS NULL OR @WindowDays = 0 THROW 53603, N'The employee is not employed in this month.', 1;
    DECLARE @Total INT = @PresentDays + @HalfDays + @WeeklyOffDays + @HolidayDays + @PaidLeaveDays + @AbsentDays;
    IF @Total > @WindowDays THROW 53605, N'The days add up to more than the days of employment in the month.', 1;
    IF @RequireComplete = 1 AND @Total <> @WindowDays THROW 53604, N'The days do not add up to the days of employment in the month.', 1;
    UPDATE att.AttendanceMonthly
    SET PresentDays = @PresentDays, HalfDays = @HalfDays, WeeklyOffDays = @WeeklyOffDays, HolidayDays = @HolidayDays,
        PaidLeaveDays = @PaidLeaveDays, AbsentDays = @AbsentDays, OvertimeHours = @OvertimeHours, Remarks = @Remarks,
        UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND PeriodStart = @PeriodStart;
    IF @@ROWCOUNT = 0
        INSERT att.AttendanceMonthly (CompanyID, EmployeeID, PeriodStart, PresentDays, HalfDays, WeeklyOffDays, HolidayDays, PaidLeaveDays, AbsentDays, OvertimeHours, Remarks, UpdatedByUserID)
        VALUES (@CompanyID, @EmployeeID, @PeriodStart, @PresentDays, @HalfDays, @WeeklyOffDays, @HolidayDays, @PaidLeaveDays, @AbsentDays, @OvertimeHours, @Remarks, @UserID);
END
GO
/* One row per employee employed in the month (active employees only), with the saved row or empty counts, the window days and its first and last date, and whether the row is complete.
   HasRow tells a saved row from an empty one; a row is complete when HasRow = 1 and the counts add up to WindowDays. */
CREATE OR ALTER PROCEDURE att.usp_AttendanceMonthly_List @PeriodStart DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @PeriodStart IS NULL OR DAY(@PeriodStart) <> 1 THROW 53605, N'The month must start on the first day.', 1;
    SELECT e.EmployeeID, e.EmpCode, e.FullName,
           w.WindowDays, x.WStart AS WindowFrom, x.WEnd AS WindowTo,
           CAST(CASE WHEN a.EmployeeID IS NULL THEN 0 ELSE 1 END AS BIT) AS HasRow,
           CAST(ISNULL(a.PresentDays, 0) AS INT) AS PresentDays, CAST(ISNULL(a.HalfDays, 0) AS INT) AS HalfDays,
           CAST(ISNULL(a.WeeklyOffDays, 0) AS INT) AS WeeklyOffDays, CAST(ISNULL(a.HolidayDays, 0) AS INT) AS HolidayDays,
           CAST(ISNULL(a.PaidLeaveDays, 0) AS INT) AS PaidLeaveDays, CAST(ISNULL(a.AbsentDays, 0) AS INT) AS AbsentDays,
           CAST(ISNULL(a.OvertimeHours, 0) AS DECIMAL(5,2)) AS OvertimeHours, a.Remarks,
           CAST(CASE WHEN a.EmployeeID IS NOT NULL
                      AND a.PresentDays + a.HalfDays + a.WeeklyOffDays + a.HolidayDays + a.PaidLeaveDays + a.AbsentDays = w.WindowDays
                     THEN 1 ELSE 0 END AS BIT) AS IsComplete
    FROM mst.Employee e
    CROSS APPLY (SELECT CASE WHEN e.JoinDate > @PeriodStart THEN e.JoinDate ELSE @PeriodStart END AS WStart,
                        CASE WHEN e.ExitDate IS NOT NULL AND e.ExitDate < EOMONTH(@PeriodStart) THEN e.ExitDate ELSE EOMONTH(@PeriodStart) END AS WEnd) x
    CROSS APPLY (SELECT CASE WHEN x.WStart <= x.WEnd THEN DATEDIFF(DAY, x.WStart, x.WEnd) + 1 ELSE 0 END AS WindowDays) w
    LEFT JOIN att.AttendanceMonthly a ON a.CompanyID = e.CompanyID AND a.EmployeeID = e.EmployeeID AND a.PeriodStart = @PeriodStart
    WHERE e.CompanyID = @CompanyID AND e.IsActive = 1 AND w.WindowDays > 0
    ORDER BY e.EmpCode;
END
GO
CREATE OR ALTER PROCEDURE att.usp_AttendanceMonthly_Delete @EmployeeID INT, @PeriodStart DATE, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @PeriodStart IS NULL OR DAY(@PeriodStart) <> 1 THROW 53605, N'The month must start on the first day.', 1;
    IF EXISTS (SELECT 1 FROM att.MonthLock WHERE CompanyID = @CompanyID AND IsClosed = 1 AND MonthKey = YEAR(@PeriodStart) * 100 + MONTH(@PeriodStart))
        THROW 50001, N'The month is closed.', 1;
    DELETE FROM att.AttendanceMonthly WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND PeriodStart = @PeriodStart;
END
GO
/* The attendance mode of the open company: 1 Monthly (also when no row exists) or 2 Daily. */
CREATE OR ALTER PROCEDURE att.usp_AttendanceSetting_Get
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT CAST(ISNULL((SELECT Mode FROM att.AttendanceSetting WHERE CompanyID = @CompanyID), 1) AS TINYINT) AS Mode;
END
GO
/* Daily (2) is refused unless the company is on the Platinum plan (PlanTierID 4). The setting changes the screens only, never the data. */
CREATE OR ALTER PROCEDURE att.usp_AttendanceSetting_Set @Mode TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @Mode NOT IN (1, 2) THROW 53605, N'Attendance mode must be Monthly or Daily.', 1;
    IF @Mode = 2 AND NOT EXISTS (SELECT 1 FROM mst.Company WHERE CompanyID = @CompanyID AND PlanTierID = 4)
        THROW 53601, N'Daily attendance is part of the Platinum plan.', 1;
    UPDATE att.AttendanceSetting SET Mode = @Mode, UpdatedUtc = SYSUTCDATETIME() WHERE CompanyID = @CompanyID;
    IF @@ROWCOUNT = 0 INSERT att.AttendanceSetting (CompanyID, Mode) VALUES (@CompanyID, @Mode);
END
GO
