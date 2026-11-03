/* 0333_pay_leave_accrual_procs.sql
   Leave accrual procedures. CREATE OR ALTER keeps it idempotent. The company always comes from SESSION_CONTEXT. No company in session means no read and no write.
   Who may save a rule or post an accrual (Owner and Associate only) is checked by LeaveAccrualService, which also writes the audit entry.
   usp_LeaveAccrual_Present: one row per employee who works in the month, with the days present (present 1, half day 0.5; weekly off, holiday and paid leave do not count)
   and the leave balance of the type (the sum of the ledger). usp_LeaveAccrual_Post writes the marker and the Accrual ledger rows together, from a JSON array.
   The leave year of an entry is the financial year it falls in (April start), the same as the ledger rule in 0317.
   Error numbers: 50002 no company in session, 53200 leave type not found, 53201 values not valid, 53202 the month is already posted for this leave type,
   53203 an employee in the entries is not in the company. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_LeaveAccrualSetting_List
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT t.LeaveTypeID, t.Code, t.Name, s.EarnedDays, s.PerWorkedDays, s.RoundingID, s.MaxBalance
    FROM pay.LeaveType t
    LEFT JOIN pay.LeaveAccrualSetting s ON s.CompanyID = t.CompanyID AND s.LeaveTypeID = t.LeaveTypeID
    WHERE t.CompanyID = @CompanyID AND t.IsActive = 1
    ORDER BY t.Code;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_LeaveAccrualSetting_Save
    @LeaveTypeID INT, @EarnedDays DECIMAL(7,2), @PerWorkedDays DECIMAL(7,2), @RoundingID TINYINT, @MaxBalance DECIMAL(7,2) = NULL, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.LeaveType WHERE CompanyID = @CompanyID AND LeaveTypeID = @LeaveTypeID) THROW 53200, N'Leave type not found.', 1;
    IF @EarnedDays < 0 OR @PerWorkedDays <= 0 OR @RoundingID NOT BETWEEN 1 AND 3 OR (@MaxBalance IS NOT NULL AND @MaxBalance < 0)
        THROW 53201, N'The accrual values are not valid.', 1;
    BEGIN TRANSACTION;
        UPDATE pay.LeaveAccrualSetting WITH (UPDLOCK, HOLDLOCK)
        SET EarnedDays = @EarnedDays, PerWorkedDays = @PerWorkedDays, RoundingID = @RoundingID, MaxBalance = @MaxBalance,
            UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
        WHERE CompanyID = @CompanyID AND LeaveTypeID = @LeaveTypeID;
        IF @@ROWCOUNT = 0
            INSERT pay.LeaveAccrualSetting (CompanyID, LeaveTypeID, EarnedDays, PerWorkedDays, RoundingID, MaxBalance, UpdatedByUserID)
            VALUES (@CompanyID, @LeaveTypeID, @EarnedDays, @PerWorkedDays, @RoundingID, @MaxBalance, @UserID);
    COMMIT TRANSACTION;
END
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
    OUTER APPLY (SELECT SUM(CASE x.StatusID WHEN 1 THEN 1.0 WHEN 3 THEN 0.5 ELSE 0.0 END) AS PresentDays
                 FROM att.AttendanceDaily x
                 WHERE x.CompanyID = e.CompanyID AND x.EmployeeID = e.EmployeeID AND x.WorkDate >= @AccrualMonth AND x.WorkDate <= @MonthEnd) a
    OUTER APPLY (SELECT SUM(l.Days) AS Balance
                 FROM pay.LeaveLedger l
                 WHERE l.CompanyID = e.CompanyID AND l.EmployeeID = e.EmployeeID AND l.LeaveTypeID = @LeaveTypeID) b
    WHERE e.CompanyID = @CompanyID AND e.JoinDate <= @MonthEnd AND (e.ExitDate IS NULL OR e.ExitDate >= @AccrualMonth)
      AND (e.IsActive = 1 OR e.ExitDate IS NOT NULL)
    ORDER BY e.EmployeeID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_LeaveAccrual_Post
    @LeaveTypeID INT, @AccrualMonth DATE, @UserID INT = NULL, @EntriesJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.LeaveType WHERE CompanyID = @CompanyID AND LeaveTypeID = @LeaveTypeID) THROW 53200, N'Leave type not found.', 1;
    IF DAY(@AccrualMonth) <> 1 OR ISJSON(@EntriesJson) <> 1 THROW 53201, N'The accrual values are not valid.', 1;
    DECLARE @Entries TABLE (EmployeeID INT NOT NULL PRIMARY KEY, Days DECIMAL(6,2) NOT NULL);
    BEGIN TRY
        INSERT @Entries SELECT EmployeeID, Days FROM OPENJSON(@EntriesJson) WITH (EmployeeID INT '$.employeeId', Days DECIMAL(6,2) '$.days');
    END TRY
    BEGIN CATCH
        THROW 53201, N'The accrual values are not valid.', 1;
    END CATCH
    IF EXISTS (SELECT 1 FROM @Entries WHERE Days <= 0) THROW 53201, N'The accrual values are not valid.', 1;
    IF EXISTS (SELECT 1 FROM @Entries n WHERE NOT EXISTS (SELECT 1 FROM mst.Employee e WHERE e.CompanyID = @CompanyID AND e.EmployeeID = n.EmployeeID))
        THROW 53203, N'An employee in the accrual is not in the company.', 1;
    DECLARE @Count INT = (SELECT COUNT(*) FROM @Entries);
    DECLARE @Total DECIMAL(10,2) = ISNULL((SELECT SUM(Days) FROM @Entries), 0);
    DECLARE @Year SMALLINT = CAST(CASE WHEN MONTH(@AccrualMonth) >= 4 THEN YEAR(@AccrualMonth) ELSE YEAR(@AccrualMonth) - 1 END AS SMALLINT);
    DECLARE @Note NVARCHAR(200) = N'Accrual ' + FORMAT(@AccrualMonth, N'MMM yyyy', N'en-US');
    BEGIN TRANSACTION;
        IF EXISTS (SELECT 1 FROM pay.LeaveAccrualRun WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND LeaveTypeID = @LeaveTypeID AND AccrualMonth = @AccrualMonth)
            THROW 53202, N'Leave for this month is already posted for this leave type.', 1;
        INSERT pay.LeaveAccrualRun (CompanyID, LeaveTypeID, AccrualMonth, EmployeeCount, TotalDays, PostedByUserID)
        VALUES (@CompanyID, @LeaveTypeID, @AccrualMonth, @Count, @Total, @UserID);
        INSERT pay.LeaveLedger (CompanyID, EmployeeID, LeaveTypeID, LeaveYear, EntryDate, EntryTypeID, Days, Note, CreatedByUserID)
        SELECT @CompanyID, EmployeeID, @LeaveTypeID, @Year, EOMONTH(@AccrualMonth), 2, Days, @Note, @UserID FROM @Entries;
    COMMIT TRANSACTION;
    SELECT @Count AS EmployeeCount;
END
GO
