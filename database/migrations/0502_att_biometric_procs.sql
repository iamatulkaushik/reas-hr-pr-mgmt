/* 0502_att_biometric_procs.sql
   Biometric procedures. CREATE OR ALTER keeps it idempotent. Except usp_Punch_Ingest, the company comes from SESSION_CONTEXT, never from a parameter.
   usp_Punch_Ingest takes the company as a parameter on purpose: it is called only by Sigma's agent wrapper, which has already authenticated the agent key and set the company
   from the agent registration (ADR 0012, ADR 0013). It is not granted to the app login or to the agent login directly.
   Error numbers: 50002 no company in session, 53300 location not found, 53301 device code already used, 53302 device not found or not active, 53303 values not valid,
   53304 employee not found, 53305 card already on another active employee, 53306 the employee has no active card. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE att.usp_Device_Save
    @DeviceID INT = NULL, @FactoryID INT, @DeviceCode NVARCHAR(30), @DeviceName NVARCHAR(100), @Brand NVARCHAR(30), @Host NVARCHAR(100),
    @Port INT, @IsActive BIT = 1, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Factory WHERE FactoryID = @FactoryID AND CompanyID = @CompanyID) THROW 53300, N'Location not found.', 1;
    IF LEN(LTRIM(RTRIM(@DeviceCode))) = 0 OR LEN(LTRIM(RTRIM(@DeviceName))) = 0 OR LEN(LTRIM(RTRIM(@Brand))) = 0 OR LEN(LTRIM(RTRIM(@Host))) = 0 OR @Port NOT BETWEEN 1 AND 65535
        THROW 53303, N'The device values are not valid.', 1;
    BEGIN TRANSACTION;
        IF EXISTS (SELECT 1 FROM att.Device WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND DeviceCode = @DeviceCode AND (@DeviceID IS NULL OR DeviceID <> @DeviceID))
            THROW 53301, N'The device code is already used.', 1;
        IF @DeviceID IS NULL
        BEGIN
            INSERT att.Device (CompanyID, FactoryID, DeviceCode, DeviceName, Brand, Host, Port, IsActive, CreatedByUserID)
            VALUES (@CompanyID, @FactoryID, LTRIM(RTRIM(@DeviceCode)), LTRIM(RTRIM(@DeviceName)), LTRIM(RTRIM(@Brand)), LTRIM(RTRIM(@Host)), @Port, @IsActive, @UserID);
            SET @DeviceID = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE att.Device
            SET FactoryID = @FactoryID, DeviceCode = LTRIM(RTRIM(@DeviceCode)), DeviceName = LTRIM(RTRIM(@DeviceName)), Brand = LTRIM(RTRIM(@Brand)),
                Host = LTRIM(RTRIM(@Host)), Port = @Port, IsActive = @IsActive
            WHERE CompanyID = @CompanyID AND DeviceID = @DeviceID;
            IF @@ROWCOUNT = 0 THROW 53302, N'Device not found.', 1;
        END
    COMMIT TRANSACTION;
    SELECT @DeviceID AS DeviceID;
END
GO
CREATE OR ALTER PROCEDURE att.usp_Device_List
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT d.DeviceID, d.FactoryID, f.FactoryName, d.DeviceCode, d.DeviceName, d.Brand, d.Host, d.Port, d.IsActive, d.LastPunchTime, d.LastPulledUtc
    FROM att.Device d
    JOIN mst.Factory f ON f.FactoryID = d.FactoryID
    WHERE d.CompanyID = @CompanyID AND (@IncludeInactive = 1 OR d.IsActive = 1)
    ORDER BY f.FactoryName, d.DeviceCode;
END
GO
CREATE OR ALTER PROCEDURE att.usp_RfidCard_Map
    @EmployeeID INT, @CardNo NVARCHAR(30), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SET @CardNo = LTRIM(RTRIM(@CardNo));
    IF LEN(@CardNo) = 0 THROW 53303, N'The card number is not valid.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID) THROW 53304, N'Employee not found.', 1;
    BEGIN TRANSACTION;
        IF EXISTS (SELECT 1 FROM att.RfidCard WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND CardNo = @CardNo AND IsActive = 1 AND EmployeeID <> @EmployeeID)
            THROW 53305, N'The card is already mapped to another employee.', 1;
        IF EXISTS (SELECT 1 FROM att.RfidCard WHERE CompanyID = @CompanyID AND CardNo = @CardNo AND IsActive = 1 AND EmployeeID = @EmployeeID)
        BEGIN
            COMMIT TRANSACTION;
            RETURN;
        END
        UPDATE att.RfidCard SET IsActive = 0, RetiredUtc = SYSUTCDATETIME(), RetiredByUserID = @UserID
        WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND IsActive = 1;
        INSERT att.RfidCard (CompanyID, EmployeeID, CardNo, MappedByUserID) VALUES (@CompanyID, @EmployeeID, @CardNo, @UserID);
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE att.usp_RfidCard_Unmap
    @EmployeeID INT, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    UPDATE att.RfidCard SET IsActive = 0, RetiredUtc = SYSUTCDATETIME(), RetiredByUserID = @UserID
    WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND IsActive = 1;
    IF @@ROWCOUNT = 0 THROW 53306, N'The employee has no active card.', 1;
END
GO
CREATE OR ALTER PROCEDURE att.usp_RfidCard_List
    @EmployeeID INT = NULL, @IncludeRetired BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT c.CardID, c.EmployeeID, e.EmpCode, e.FullName, c.CardNo, c.IsActive, c.MappedUtc, c.RetiredUtc
    FROM att.RfidCard c
    JOIN mst.Employee e ON e.CompanyID = c.CompanyID AND e.EmployeeID = c.EmployeeID
    WHERE c.CompanyID = @CompanyID AND (@EmployeeID IS NULL OR c.EmployeeID = @EmployeeID) AND (@IncludeRetired = 1 OR c.IsActive = 1)
    ORDER BY e.EmpCode, c.CardID DESC;
END
GO
CREATE OR ALTER PROCEDURE att.usp_Punch_Ingest
    @CompanyID INT, @DeviceID INT, @PunchesJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @FactoryID INT = (SELECT FactoryID FROM att.Device WHERE CompanyID = @CompanyID AND DeviceID = @DeviceID AND IsActive = 1);
    IF @FactoryID IS NULL THROW 53302, N'Device not found or not active.', 1;
    IF ISJSON(@PunchesJson) <> 1 THROW 53303, N'The punches are not valid.', 1;
    DECLARE @In TABLE (DeviceUserID INT NULL, CardNo NVARCHAR(30) NULL, PunchTime DATETIME2(0) NOT NULL, VerifyMode TINYINT NULL, DeviceStatus TINYINT NULL);
    BEGIN TRY
        INSERT @In
        SELECT DeviceUserID, NULLIF(LTRIM(RTRIM(CardNo)), N''), PunchTime, VerifyMode, DeviceStatus
        FROM OPENJSON(@PunchesJson) WITH (DeviceUserID INT '$.deviceUserId', CardNo NVARCHAR(30) '$.cardNo', PunchTime DATETIME2(0) '$.punchTime',
                                          VerifyMode TINYINT '$.verifyMode', DeviceStatus TINYINT '$.deviceStatus');
    END TRY
    BEGIN CATCH
        THROW 53303, N'The punches are not valid.', 1;
    END CATCH
    IF EXISTS (SELECT 1 FROM @In WHERE (DeviceUserID IS NULL AND CardNo IS NULL) OR DeviceUserID <= 0) THROW 53303, N'The punches are not valid.', 1;
    DECLARE @Received INT = (SELECT COUNT(*) FROM @In);
    DECLARE @Inserted INT = 0, @Unmatched INT = 0;
    BEGIN TRANSACTION;
        INSERT att.Punch (CompanyID, DeviceID, FactoryID, DeviceUserID, CardNo, PunchTime, VerifyMode, DeviceStatus, EmployeeID)
        SELECT DISTINCT @CompanyID, @DeviceID, @FactoryID, i.DeviceUserID, i.CardNo, i.PunchTime, i.VerifyMode, i.DeviceStatus,
               COALESCE((SELECT c.EmployeeID FROM att.RfidCard c WHERE c.CompanyID = @CompanyID AND c.CardNo = i.CardNo AND c.IsActive = 1),
                        (SELECT e.EmployeeID FROM mst.Employee e WHERE e.CompanyID = @CompanyID AND e.BiometricID = i.DeviceUserID))
        FROM @In i
        WHERE NOT EXISTS (SELECT 1 FROM att.Punch p WHERE p.CompanyID = @CompanyID AND p.DeviceID = @DeviceID AND p.PunchTime = i.PunchTime
                          AND p.DeviceUserKey = ISNULL(i.DeviceUserID, -1) AND p.CardKey = ISNULL(i.CardNo, N''));
        SET @Inserted = @@ROWCOUNT;
        UPDATE att.Device
        SET LastPulledUtc = SYSUTCDATETIME(), LastPunchTime = (SELECT MAX(t) FROM (VALUES (LastPunchTime), ((SELECT MAX(PunchTime) FROM @In))) AS v(t))
        WHERE CompanyID = @CompanyID AND DeviceID = @DeviceID;
    COMMIT TRANSACTION;
    SELECT @Unmatched = COUNT(*) FROM att.Punch WHERE CompanyID = @CompanyID AND DeviceID = @DeviceID AND EmployeeID IS NULL
        AND PunchTime >= (SELECT MIN(PunchTime) FROM @In);
    SELECT @Received AS Received, @Inserted AS Inserted, @Unmatched AS Unmatched;
END
GO
CREATE OR ALTER PROCEDURE att.usp_Punch_List
    @FromDate DATE, @ToDate DATE, @EmployeeID INT = NULL, @DeviceID INT = NULL, @UnmatchedOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT TOP (5000) p.PunchID, p.DeviceID, d.DeviceCode, p.FactoryID, p.DeviceUserID, p.CardNo, p.PunchTime, p.VerifyMode, p.DeviceStatus, p.EmployeeID, e.EmpCode, e.FullName
    FROM att.Punch p
    JOIN att.Device d ON d.CompanyID = p.CompanyID AND d.DeviceID = p.DeviceID
    LEFT JOIN mst.Employee e ON e.CompanyID = p.CompanyID AND e.EmployeeID = p.EmployeeID
    WHERE p.CompanyID = @CompanyID AND p.PunchTime >= @FromDate AND p.PunchTime < DATEADD(DAY, 1, CAST(@ToDate AS DATETIME2(0)))
      AND (@EmployeeID IS NULL OR p.EmployeeID = @EmployeeID) AND (@DeviceID IS NULL OR p.DeviceID = @DeviceID)
      AND (@UnmatchedOnly = 0 OR p.EmployeeID IS NULL)
    ORDER BY p.PunchTime, p.PunchID;
END
GO
CREATE OR ALTER PROCEDURE att.usp_Punch_Resolve
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Before INT = (SELECT COUNT(*) FROM att.Punch WHERE CompanyID = @CompanyID AND EmployeeID IS NULL);
    UPDATE p
    SET EmployeeID = COALESCE((SELECT c.EmployeeID FROM att.RfidCard c WHERE c.CompanyID = p.CompanyID AND c.CardNo = p.CardNo AND c.IsActive = 1),
                              (SELECT e.EmployeeID FROM mst.Employee e WHERE e.CompanyID = p.CompanyID AND e.BiometricID = p.DeviceUserID))
    FROM att.Punch p
    WHERE p.CompanyID = @CompanyID AND p.EmployeeID IS NULL;
    DECLARE @After INT = (SELECT COUNT(*) FROM att.Punch WHERE CompanyID = @CompanyID AND EmployeeID IS NULL);
    SELECT @Before - @After AS Resolved, @After AS StillUnmatched;
END
GO
