/* 0319_pay_leave_procs.sql
   Leave ledger procedures. CREATE OR ALTER keeps it idempotent.
   The company always comes from SESSION_CONTEXT, never from a parameter. No company in session means no read and no write.
   usp_LeaveLedger_Post takes Days as a positive number for every entry type except Adjustment, and applies the sign itself: Taken, Encashed and Lapsed take
   away. Adjustment keeps the sign it is given. A posting that would leave the balance below zero is refused, except an Adjustment.
   Error numbers: 50002 no company in session, 50350 leave type not found, 50351 employee not found, 50352 balance would go below zero,
   50353 days must be positive for this entry type. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_LeaveType_List
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT LeaveTypeID, Code, Name, IsEncashable, MaxEncashDays, MaxCarryForwardDays, IsActive
    FROM pay.LeaveType WHERE CompanyID = @CompanyID ORDER BY Name;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_LeaveType_Save
    @LeaveTypeID INT = NULL, @Code NVARCHAR(10), @Name NVARCHAR(100), @IsEncashable BIT, @MaxEncashDays DECIMAL(6,2) = NULL,
    @MaxCarryForwardDays DECIMAL(6,2) = NULL, @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @LeaveTypeID IS NULL
    BEGIN
        INSERT pay.LeaveType (CompanyID, Code, Name, IsEncashable, MaxEncashDays, MaxCarryForwardDays, IsActive)
        VALUES (@CompanyID, @Code, @Name, @IsEncashable, @MaxEncashDays, @MaxCarryForwardDays, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS LeaveTypeID;
    END
    ELSE
    BEGIN
        UPDATE pay.LeaveType
        SET Code = @Code, Name = @Name, IsEncashable = @IsEncashable, MaxEncashDays = @MaxEncashDays,
            MaxCarryForwardDays = @MaxCarryForwardDays, IsActive = @IsActive
        WHERE LeaveTypeID = @LeaveTypeID AND CompanyID = @CompanyID;
        IF @@ROWCOUNT = 0 THROW 50350, N'Leave type not found.', 1;
        SELECT @LeaveTypeID AS LeaveTypeID;
    END
END
GO
CREATE OR ALTER PROCEDURE pay.usp_LeaveLedger_Post
    @EmployeeID INT, @LeaveTypeID INT, @LeaveYear SMALLINT, @EntryDate DATE, @EntryTypeID TINYINT, @Days DECIMAL(6,2),
    @Note NVARCHAR(200) = NULL, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.LeaveType WHERE CompanyID = @CompanyID AND LeaveTypeID = @LeaveTypeID) THROW 50350, N'Leave type not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID) THROW 50351, N'Employee not found.', 1;
    IF @EntryTypeID <> 7 AND @Days <= 0 THROW 50353, N'Days must be more than zero for this entry type.', 1;
    DECLARE @Signed DECIMAL(6,2) = CASE WHEN @EntryTypeID IN (3, 5, 6) THEN -@Days ELSE @Days END;
    BEGIN TRANSACTION;
        DECLARE @Balance DECIMAL(8,2) = ISNULL((SELECT SUM(Days) FROM pay.LeaveLedger WITH (UPDLOCK, HOLDLOCK)
                                                WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND LeaveTypeID = @LeaveTypeID), 0);
        IF @EntryTypeID <> 7 AND @Balance + @Signed < 0
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50352, N'The balance would go below zero.', 1;
        END
        INSERT pay.LeaveLedger (CompanyID, EmployeeID, LeaveTypeID, LeaveYear, EntryDate, EntryTypeID, Days, Note, CreatedByUserID)
        VALUES (@CompanyID, @EmployeeID, @LeaveTypeID, @LeaveYear, @EntryDate, @EntryTypeID, @Signed, @Note, @UserID);
        DECLARE @LedgerID BIGINT = SCOPE_IDENTITY();
    COMMIT TRANSACTION;
    SELECT @LedgerID AS LedgerID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_LeaveLedger_List @EmployeeID INT, @LeaveTypeID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT LedgerID, LeaveTypeID, LeaveYear, EntryDate, EntryTypeID, Days, Note
    FROM pay.LeaveLedger
    WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND (@LeaveTypeID IS NULL OR LeaveTypeID = @LeaveTypeID)
    ORDER BY EntryDate, LedgerID;
END
GO
/* One row per leave type with a movement. Accrued is Opening plus Accrual, taken and the rest are shown as positive days. Balance is the sum of every entry. */
CREATE OR ALTER PROCEDURE pay.usp_LeaveBalance_List @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT t.LeaveTypeID, t.Code, t.Name, t.IsEncashable, t.MaxEncashDays,
           ISNULL(SUM(CASE WHEN l.EntryTypeID IN (1, 2) THEN l.Days END), 0)  AS AccruedDays,
           ISNULL(SUM(CASE WHEN l.EntryTypeID = 4 THEN l.Days END), 0)        AS CarriedForwardDays,
           ISNULL(-SUM(CASE WHEN l.EntryTypeID = 3 THEN l.Days END), 0)       AS TakenDays,
           ISNULL(-SUM(CASE WHEN l.EntryTypeID = 5 THEN l.Days END), 0)       AS EncashedDays,
           ISNULL(-SUM(CASE WHEN l.EntryTypeID = 6 THEN l.Days END), 0)       AS LapsedDays,
           ISNULL(SUM(CASE WHEN l.EntryTypeID = 7 THEN l.Days END), 0)        AS AdjustedDays,
           ISNULL(SUM(l.Days), 0)                                             AS BalanceDays
    FROM pay.LeaveType t
    LEFT JOIN pay.LeaveLedger l ON l.CompanyID = t.CompanyID AND l.LeaveTypeID = t.LeaveTypeID AND l.EmployeeID = @EmployeeID
    WHERE t.CompanyID = @CompanyID
    GROUP BY t.LeaveTypeID, t.Code, t.Name, t.IsEncashable, t.MaxEncashDays
    ORDER BY t.Name;
END
GO
