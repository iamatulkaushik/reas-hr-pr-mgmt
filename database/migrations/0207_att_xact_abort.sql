/* 0207_att_xact_abort.sql
   Re-creates the three attendance procedures that open a transaction so that SET XACT_ABORT ON is inside the procedure.
   The SET at the top of 0202 and 0206 only applies to the setup session, not to the app connection. Without it a THROW inside
   BEGIN TRAN left the transaction open (and its UPDLOCK held) until the connection was reset.
   Bodies are the same as 0202 and 0206 apart from that line. CREATE OR ALTER keeps this idempotent. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE att.usp_Month_Close @MonthKey INT, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
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
    SET XACT_ABORT ON;
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
/* Assigns a shift from a date. The open assignment ends the day before. A new start date must be after the latest start date. */
CREATE OR ALTER PROCEDURE att.usp_EmployeeShift_Assign @EmployeeID INT, @ShiftID INT, @EffectiveFrom DATE
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID)
       OR NOT EXISTS (SELECT 1 FROM att.Shift WHERE ShiftID = @ShiftID AND CompanyID = @CompanyID)
        THROW 50021, N'Employee or shift not found.', 1;
    BEGIN TRAN;
    IF EXISTS (SELECT 1 FROM att.EmployeeShift WITH (UPDLOCK, HOLDLOCK)
               WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND EffectiveFrom >= @EffectiveFrom)
        THROW 50020, N'The new shift must start after the current one.', 1;
    UPDATE att.EmployeeShift SET EffectiveTo = DATEADD(DAY, -1, @EffectiveFrom)
    WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND EffectiveTo IS NULL;
    INSERT att.EmployeeShift (CompanyID, EmployeeID, ShiftID, EffectiveFrom) VALUES (@CompanyID, @EmployeeID, @ShiftID, @EffectiveFrom);
    COMMIT;
END
GO
