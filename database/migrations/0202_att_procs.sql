/* 0202_att_procs.sql
   Attendance stored procedures. CREATE OR ALTER keeps it idempotent.
   The company always comes from SESSION_CONTEXT, never from a parameter. No company in the session means no write.
   Error numbers: 50001 month is closed, 50002 no company in session, 50003 month already closed, 50004 month is not closed, 50005 reopen reason too short.
   att.MonthLockEvent is insert-only: no procedure updates or deletes it, and the app login has EXECUTE only. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE att.usp_Shift_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ShiftID, ShiftName, StartTime, EndTime, UnpaidBreakMinutes, BreakAppliesAfterMinutes,
           FullDayMinutes, HalfDayMinutes, OvertimeStepMinutes, IsActive
    FROM att.Shift
    ORDER BY ShiftName;
END
GO
CREATE OR ALTER PROCEDURE att.usp_Shift_Save
    @ShiftID INT = NULL, @ShiftName NVARCHAR(100), @StartTime TIME(0), @EndTime TIME(0),
    @UnpaidBreakMinutes SMALLINT, @BreakAppliesAfterMinutes SMALLINT, @FullDayMinutes SMALLINT,
    @HalfDayMinutes SMALLINT, @OvertimeStepMinutes TINYINT, @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @ShiftID IS NULL
    BEGIN
        INSERT att.Shift (CompanyID, ShiftName, StartTime, EndTime, UnpaidBreakMinutes, BreakAppliesAfterMinutes,
                          FullDayMinutes, HalfDayMinutes, OvertimeStepMinutes, IsActive)
        VALUES (@CompanyID, @ShiftName, @StartTime, @EndTime, @UnpaidBreakMinutes, @BreakAppliesAfterMinutes,
                @FullDayMinutes, @HalfDayMinutes, @OvertimeStepMinutes, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS ShiftID;
    END
    ELSE
    BEGIN
        UPDATE att.Shift
        SET ShiftName = @ShiftName, StartTime = @StartTime, EndTime = @EndTime, UnpaidBreakMinutes = @UnpaidBreakMinutes,
            BreakAppliesAfterMinutes = @BreakAppliesAfterMinutes, FullDayMinutes = @FullDayMinutes,
            HalfDayMinutes = @HalfDayMinutes, OvertimeStepMinutes = @OvertimeStepMinutes, IsActive = @IsActive
        WHERE ShiftID = @ShiftID AND CompanyID = @CompanyID;
        SELECT @ShiftID AS ShiftID;
    END
END
GO
CREATE OR ALTER PROCEDURE att.usp_Month_ListLocked
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MonthKey FROM att.MonthLock WHERE IsClosed = 1 ORDER BY MonthKey;
END
GO
CREATE OR ALTER PROCEDURE att.usp_Month_Close @MonthKey INT, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    BEGIN TRAN;
    IF EXISTS (SELECT 1 FROM att.MonthLock WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND MonthKey = @MonthKey AND IsClosed = 1)
        THROW 50003, N'The month is already closed.', 1;
    UPDATE att.MonthLock SET IsClosed = 1, ClosedUtc = SYSUTCDATETIME(), ClosedByUserID = @UserID
    WHERE CompanyID = @CompanyID AND MonthKey = @MonthKey;
    IF @@ROWCOUNT = 0
        INSERT att.MonthLock (CompanyID, MonthKey, IsClosed, ClosedUtc, ClosedByUserID)
        VALUES (@CompanyID, @MonthKey, 1, SYSUTCDATETIME(), @UserID);
    INSERT att.MonthLockEvent (CompanyID, MonthKey, EventType, UserID) VALUES (@CompanyID, @MonthKey, 1, @UserID);
    COMMIT;
END
GO
/* Reopen: unlock and history row in one transaction. XACT_ABORT rolls both back on any error, so the month stays locked. */
CREATE OR ALTER PROCEDURE att.usp_Month_Reopen @MonthKey INT, @Reason NVARCHAR(500), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @Reason IS NULL OR LEN(LTRIM(RTRIM(@Reason))) < 10 THROW 50005, N'A reopen needs a reason of at least 10 characters.', 1;
    BEGIN TRAN;
    UPDATE att.MonthLock
    SET IsClosed = 0, ReopenedUtc = SYSUTCDATETIME(), ReopenedByUserID = @UserID, ReopenReason = LTRIM(RTRIM(@Reason))
    WHERE CompanyID = @CompanyID AND MonthKey = @MonthKey AND IsClosed = 1;
    IF @@ROWCOUNT = 0 THROW 50004, N'The month is not closed.', 1;
    INSERT att.MonthLockEvent (CompanyID, MonthKey, EventType, UserID, Reason)
    VALUES (@CompanyID, @MonthKey, 2, @UserID, LTRIM(RTRIM(@Reason)));
    COMMIT;
END
GO
CREATE OR ALTER PROCEDURE att.usp_Attendance_ListMonth @EmployeeID INT, @Year INT, @Month INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @First DATE = DATEFROMPARTS(@Year, @Month, 1);
    SELECT WorkDate, StatusID, OvertimeMinutes
    FROM att.AttendanceDaily
    WHERE EmployeeID = @EmployeeID AND WorkDate >= @First AND WorkDate < DATEADD(MONTH, 1, @First)
    ORDER BY WorkDate;
END
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
    UPDATE att.AttendanceDaily
    SET StatusID = @StatusID, ShiftID = @ShiftID, WorkedMinutes = @WorkedMinutes, OvertimeMinutes = @OvertimeMinutes,
        UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND WorkDate = @WorkDate;
    IF @@ROWCOUNT = 0
        INSERT att.AttendanceDaily (CompanyID, EmployeeID, WorkDate, StatusID, ShiftID, WorkedMinutes, OvertimeMinutes, UpdatedByUserID)
        VALUES (@CompanyID, @EmployeeID, @WorkDate, @StatusID, @ShiftID, @WorkedMinutes, @OvertimeMinutes, @UserID);
END
GO
