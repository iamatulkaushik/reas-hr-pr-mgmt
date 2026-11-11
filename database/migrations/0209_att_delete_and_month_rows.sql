/* 0209_att_delete_and_month_rows.sql
   Two procedures for the attendance screens. CREATE OR ALTER keeps it idempotent.
   usp_Attendance_ListMonthRows: one employee's marked days with shift and worked minutes (read only, tenant rows by row-level security).
   usp_Attendance_Delete: removes one marked day. Refused when the month is closed (same check as usp_Attendance_Save). */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE att.usp_Attendance_ListMonthRows @EmployeeID INT, @Year INT, @Month INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @First DATE = DATEFROMPARTS(@Year, @Month, 1);
    SELECT a.WorkDate, a.StatusID, a.ShiftID, a.WorkedMinutes, a.OvertimeMinutes
    FROM att.AttendanceDaily a
    WHERE a.EmployeeID = @EmployeeID AND a.WorkDate >= @First AND a.WorkDate < DATEADD(MONTH, 1, @First)
    ORDER BY a.WorkDate;
END
GO
CREATE OR ALTER PROCEDURE att.usp_Attendance_Delete @EmployeeID INT, @WorkDate DATE, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF EXISTS (SELECT 1 FROM att.MonthLock WHERE CompanyID = @CompanyID AND IsClosed = 1 AND MonthKey = YEAR(@WorkDate) * 100 + MONTH(@WorkDate))
        THROW 50001, N'The month is closed.', 1;
    DELETE FROM att.AttendanceDaily
    WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND WorkDate = @WorkDate;
END
GO
