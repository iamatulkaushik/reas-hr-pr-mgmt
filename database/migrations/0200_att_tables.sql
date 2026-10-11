/* 0200_att_tables.sql
   Attendance tables: shift, daily attendance, month lock. Brave lane 0200-0299 (ADR 0008). Idempotent. Never edit after merge.
   EmployeeID has no foreign key yet: mst.Employee arrives in lane 0100. A later 02xx script adds the key.
   att.MonthLock holds the current state of a month: IsClosed 1 means locked. Reopen sets IsClosed 0 and records who, when and why.
   att.MonthLockEvent is the append-only history of every close and reopen. It is kept on every plan, independent of the audit trail. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'att.Shift', N'U') IS NULL
BEGIN
    CREATE TABLE att.Shift
    (
        ShiftID                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Shift PRIMARY KEY,
        CompanyID                INT           NOT NULL CONSTRAINT FK_Shift_Company REFERENCES mst.Company (CompanyID),
        ShiftName                NVARCHAR(100) NOT NULL,
        StartTime                TIME(0)       NOT NULL,
        EndTime                  TIME(0)       NOT NULL,
        UnpaidBreakMinutes       SMALLINT      NOT NULL CONSTRAINT CK_Shift_Break CHECK (UnpaidBreakMinutes >= 0),
        BreakAppliesAfterMinutes SMALLINT      NOT NULL CONSTRAINT CK_Shift_BreakAfter CHECK (BreakAppliesAfterMinutes >= 0),
        FullDayMinutes           SMALLINT      NOT NULL CONSTRAINT CK_Shift_Full CHECK (FullDayMinutes BETWEEN 1 AND 1440),
        HalfDayMinutes           SMALLINT      NOT NULL,
        OvertimeStepMinutes      TINYINT       NOT NULL CONSTRAINT CK_Shift_OtStep CHECK (OvertimeStepMinutes IN (1, 15, 30, 60)),
        IsActive                 BIT           NOT NULL CONSTRAINT DF_Shift_IsActive DEFAULT 1,
        CreatedUtc               DATETIME2(0)  NOT NULL CONSTRAINT DF_Shift_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_Shift_Name UNIQUE (CompanyID, ShiftName),
        CONSTRAINT CK_Shift_Half CHECK (HalfDayMinutes >= 1 AND HalfDayMinutes < FullDayMinutes)
    );
END
GO
IF OBJECT_ID(N'att.AttendanceDaily', N'U') IS NULL
BEGIN
    CREATE TABLE att.AttendanceDaily
    (
        AttendanceID    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AttendanceDaily PRIMARY KEY,
        CompanyID       INT          NOT NULL CONSTRAINT FK_AttendanceDaily_Company REFERENCES mst.Company (CompanyID),
        EmployeeID      INT          NOT NULL,
        WorkDate        DATE         NOT NULL,
        StatusID        TINYINT      NOT NULL CONSTRAINT CK_AttendanceDaily_Status CHECK (StatusID BETWEEN 1 AND 6),
        ShiftID         INT          NULL CONSTRAINT FK_AttendanceDaily_Shift REFERENCES att.Shift (ShiftID),
        WorkedMinutes   SMALLINT     NOT NULL CONSTRAINT DF_AttendanceDaily_Worked DEFAULT 0 CONSTRAINT CK_AttendanceDaily_Worked CHECK (WorkedMinutes BETWEEN 0 AND 1440),
        OvertimeMinutes SMALLINT     NOT NULL CONSTRAINT DF_AttendanceDaily_Ot DEFAULT 0 CONSTRAINT CK_AttendanceDaily_Ot CHECK (OvertimeMinutes BETWEEN 0 AND 1440),
        UpdatedUtc      DATETIME2(0) NOT NULL CONSTRAINT DF_AttendanceDaily_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        UpdatedByUserID INT          NULL,
        CONSTRAINT UQ_AttendanceDaily_Day UNIQUE (CompanyID, EmployeeID, WorkDate)
    );
END
GO
IF OBJECT_ID(N'att.MonthLock', N'U') IS NULL
BEGIN
    CREATE TABLE att.MonthLock
    (
        CompanyID      INT          NOT NULL CONSTRAINT FK_MonthLock_Company REFERENCES mst.Company (CompanyID),
        MonthKey       INT          NOT NULL CONSTRAINT CK_MonthLock_Key CHECK (MonthKey BETWEEN 190001 AND 999912 AND MonthKey % 100 BETWEEN 1 AND 12),
        IsClosed       BIT          NOT NULL,
        ClosedUtc      DATETIME2(0) NULL,
        ClosedByUserID INT          NULL,
        ReopenedUtc    DATETIME2(0) NULL,
        ReopenedByUserID INT        NULL,
        ReopenReason   NVARCHAR(500) NULL,
        CONSTRAINT PK_MonthLock PRIMARY KEY (CompanyID, MonthKey)
    );
END
GO
IF OBJECT_ID(N'att.MonthLockEvent', N'U') IS NULL
BEGIN
    CREATE TABLE att.MonthLockEvent
    (
        EventID    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MonthLockEvent PRIMARY KEY,
        CompanyID  INT           NOT NULL CONSTRAINT FK_MonthLockEvent_Company REFERENCES mst.Company (CompanyID),
        MonthKey   INT           NOT NULL,
        EventType  TINYINT       NOT NULL CONSTRAINT CK_MonthLockEvent_Type CHECK (EventType IN (1, 2)), /* 1 close, 2 reopen */
        UserID     INT           NULL,
        Reason     NVARCHAR(500) NULL,
        EventUtc   DATETIME2(0)  NOT NULL CONSTRAINT DF_MonthLockEvent_EventUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_MonthLockEvent_Reason CHECK (EventType = 1 OR (Reason IS NOT NULL AND LEN(LTRIM(RTRIM(Reason))) >= 10))
    );
    CREATE INDEX IX_MonthLockEvent_Month ON att.MonthLockEvent (CompanyID, MonthKey, EventID);
END
GO
