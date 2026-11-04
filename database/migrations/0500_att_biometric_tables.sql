/* 0500_att_biometric_tables.sql
   Phase 8, biometric and RFID punches (ADR 0012, ADR 0013). Brave lane 0500-0599. Idempotent. Never edit after merge. Tenant policy is in 0501, procedures in 0502.
   No new schema: the tables sit in att, beside the attendance tables they feed.
   att.Device: a registered device under a company and a location (mst.Factory). The agent pulls from it; the device does not log in. Brand is free text ('ESSL' first).
   LastPunchTime is the newest punch stored from this device, in the device's own clock; the next pull starts a few minutes before it and duplicates are ignored.
   att.RfidCard: one active card per employee and one active employee per card. A new card for an employee, or a card handed over, retires the old row; rows are never deleted.
   att.Punch: one row per punch as the device reported it, with its device and location. DeviceUserID is the id on the device (the employee's BiometricID); CardNo is the RFID card number.
   EmployeeID is worked out when the punch arrives, from the active card first and then from BiometricID, and stays NULL while nobody matches; usp_Punch_Resolve fills it in later.
   PunchTime is the device's clock as the device sent it, the factory's local time. A punch is stored once: the same device, time, device user and card is ignored. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'att.Device', N'U') IS NULL
BEGIN
    CREATE TABLE att.Device
    (
        DeviceID        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Device PRIMARY KEY,
        CompanyID       INT           NOT NULL CONSTRAINT FK_Device_Company REFERENCES mst.Company (CompanyID),
        FactoryID       INT           NOT NULL CONSTRAINT FK_Device_Factory REFERENCES mst.Factory (FactoryID),
        DeviceCode      NVARCHAR(30)  NOT NULL,
        DeviceName      NVARCHAR(100) NOT NULL,
        Brand           NVARCHAR(30)  NOT NULL,
        Host            NVARCHAR(100) NOT NULL,
        Port            INT           NOT NULL CONSTRAINT CK_Device_Port CHECK (Port BETWEEN 1 AND 65535),
        IsActive        BIT           NOT NULL CONSTRAINT DF_Device_IsActive DEFAULT 1,
        LastPunchTime   DATETIME2(0)  NULL,
        LastPulledUtc   DATETIME2(0)  NULL,
        CreatedUtc      DATETIME2(0)  NOT NULL CONSTRAINT DF_Device_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CreatedByUserID INT           NULL,
        CONSTRAINT UQ_Device_Code UNIQUE (CompanyID, DeviceCode),
        CONSTRAINT UQ_Device_Company UNIQUE (CompanyID, DeviceID)
    );
END
GO
IF OBJECT_ID(N'att.RfidCard', N'U') IS NULL
BEGIN
    CREATE TABLE att.RfidCard
    (
        CardID              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RfidCard PRIMARY KEY,
        CompanyID           INT           NOT NULL CONSTRAINT FK_RfidCard_Company REFERENCES mst.Company (CompanyID),
        EmployeeID          INT           NOT NULL,
        CardNo              NVARCHAR(30)  NOT NULL,
        IsActive            BIT           NOT NULL CONSTRAINT DF_RfidCard_IsActive DEFAULT 1,
        MappedUtc           DATETIME2(0)  NOT NULL CONSTRAINT DF_RfidCard_MappedUtc DEFAULT SYSUTCDATETIME(),
        MappedByUserID      INT           NULL,
        RetiredUtc          DATETIME2(0)  NULL,
        RetiredByUserID     INT           NULL,
        CONSTRAINT FK_RfidCard_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID)
    );
    CREATE UNIQUE INDEX UX_RfidCard_ActiveCard ON att.RfidCard (CompanyID, CardNo) WHERE IsActive = 1;
    CREATE UNIQUE INDEX UX_RfidCard_ActiveEmployee ON att.RfidCard (CompanyID, EmployeeID) WHERE IsActive = 1;
END
GO
IF OBJECT_ID(N'att.Punch', N'U') IS NULL
BEGIN
    CREATE TABLE att.Punch
    (
        PunchID         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Punch PRIMARY KEY,
        CompanyID       INT           NOT NULL CONSTRAINT FK_Punch_Company REFERENCES mst.Company (CompanyID),
        DeviceID        INT           NOT NULL,
        FactoryID       INT           NOT NULL,
        DeviceUserID    INT           NULL CONSTRAINT CK_Punch_UserID CHECK (DeviceUserID IS NULL OR DeviceUserID > 0),
        CardNo          NVARCHAR(30)  NULL,
        PunchTime       DATETIME2(0)  NOT NULL,
        VerifyMode      TINYINT       NULL,
        DeviceStatus    TINYINT       NULL,
        EmployeeID      INT           NULL,
        ReceivedUtc     DATETIME2(0)  NOT NULL CONSTRAINT DF_Punch_ReceivedUtc DEFAULT SYSUTCDATETIME(),
        DeviceUserKey   AS ISNULL(DeviceUserID, -1) PERSISTED,
        CardKey         AS ISNULL(CardNo, N'') PERSISTED,
        CONSTRAINT CK_Punch_Who CHECK (DeviceUserID IS NOT NULL OR CardNo IS NOT NULL),
        CONSTRAINT FK_Punch_Device FOREIGN KEY (CompanyID, DeviceID) REFERENCES att.Device (CompanyID, DeviceID),
        CONSTRAINT FK_Punch_Factory FOREIGN KEY (FactoryID) REFERENCES mst.Factory (FactoryID),
        CONSTRAINT UQ_Punch_Once UNIQUE (CompanyID, DeviceID, PunchTime, DeviceUserKey, CardKey)
    );
    CREATE INDEX IX_Punch_Employee ON att.Punch (CompanyID, EmployeeID, PunchTime);
    CREATE INDEX IX_Punch_Time ON att.Punch (CompanyID, PunchTime);
END
GO
