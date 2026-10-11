/* 0206_att_roster_procs.sql
   Roster procedures. CREATE OR ALTER keeps it idempotent. Company comes from SESSION_CONTEXT only.
   Error numbers: 50002 no company in session, 50020 shift assignment starts before or on the current one, 50021 employee or shift not found. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE att.usp_Holiday_List @Year INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT HolidayID, HolidayDate, HolidayName FROM att.Holiday
    WHERE HolidayDate >= DATEFROMPARTS(@Year, 1, 1) AND HolidayDate < DATEFROMPARTS(@Year + 1, 1, 1)
    ORDER BY HolidayDate;
END
GO
CREATE OR ALTER PROCEDURE att.usp_Holiday_Save @HolidayDate DATE, @HolidayName NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    UPDATE att.Holiday SET HolidayName = @HolidayName WHERE CompanyID = @CompanyID AND HolidayDate = @HolidayDate;
    IF @@ROWCOUNT = 0 INSERT att.Holiday (CompanyID, HolidayDate, HolidayName) VALUES (@CompanyID, @HolidayDate, @HolidayName);
END
GO
CREATE OR ALTER PROCEDURE att.usp_Holiday_Delete @HolidayDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DELETE att.Holiday WHERE CompanyID = @CompanyID AND HolidayDate = @HolidayDate;
END
GO
CREATE OR ALTER PROCEDURE att.usp_WeeklyOff_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT EffectiveFrom, DaysMask FROM att.WeeklyOffRule ORDER BY EffectiveFrom;
END
GO
CREATE OR ALTER PROCEDURE att.usp_WeeklyOff_Save @EffectiveFrom DATE, @DaysMask TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    UPDATE att.WeeklyOffRule SET DaysMask = @DaysMask WHERE CompanyID = @CompanyID AND EffectiveFrom = @EffectiveFrom;
    IF @@ROWCOUNT = 0 INSERT att.WeeklyOffRule (CompanyID, EffectiveFrom, DaysMask) VALUES (@CompanyID, @EffectiveFrom, @DaysMask);
END
GO
CREATE OR ALTER PROCEDURE att.usp_EmployeeShift_List @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT es.ShiftID, s.ShiftName, es.EffectiveFrom, es.EffectiveTo
    FROM att.EmployeeShift es JOIN att.Shift s ON s.ShiftID = es.ShiftID
    WHERE es.EmployeeID = @EmployeeID
    ORDER BY es.EffectiveFrom;
END
GO
/* Assigns a shift from a date. The open assignment ends the day before. A new start date must be after the latest start date. */
CREATE OR ALTER PROCEDURE att.usp_EmployeeShift_Assign @EmployeeID INT, @ShiftID INT, @EffectiveFrom DATE
AS
BEGIN
    SET NOCOUNT ON;
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
