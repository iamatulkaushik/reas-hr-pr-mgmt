/* 0214_att_save_one_source_per_month.sql
   Patch of att.usp_Attendance_Save (defined in 0202; this is the last definition, ADR 0024): a month that already has monthly rows refuses daily entry (error 53602,
   ADR 0026 point 4). The body is the 0202 body plus that check. Needs 0210. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE att.usp_Attendance_Save
    @EmployeeID INT, @WorkDate DATE, @StatusID TINYINT, @ShiftID INT = NULL,
    @WorkedMinutes SMALLINT, @OvertimeMinutes SMALLINT, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    /* The service checks the lock too. This check closes the gap if the month is closed between the two calls. */
    IF EXISTS (SELECT 1 FROM att.MonthLock WHERE CompanyID = @CompanyID AND IsClosed = 1 AND MonthKey = YEAR(@WorkDate) * 100 + MONTH(@WorkDate))
        THROW 50001, N'The month is closed.', 1;
    IF EXISTS (SELECT 1 FROM att.AttendanceMonthly WHERE CompanyID = @CompanyID AND PeriodStart = DATEFROMPARTS(YEAR(@WorkDate), MONTH(@WorkDate), 1))
        THROW 53602, N'This month already has monthly attendance. A month is either daily or monthly.', 1;
    UPDATE att.AttendanceDaily
    SET StatusID = @StatusID, ShiftID = @ShiftID, WorkedMinutes = @WorkedMinutes, OvertimeMinutes = @OvertimeMinutes,
        UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND WorkDate = @WorkDate;
    IF @@ROWCOUNT = 0
        INSERT att.AttendanceDaily (CompanyID, EmployeeID, WorkDate, StatusID, ShiftID, WorkedMinutes, OvertimeMinutes, UpdatedByUserID)
        VALUES (@CompanyID, @EmployeeID, @WorkDate, @StatusID, @ShiftID, @WorkedMinutes, @OvertimeMinutes, @UserID);
END
GO
