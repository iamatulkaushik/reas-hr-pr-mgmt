/* 0317_pay_leave_tables.sql
   Minimal leave ledger (Owner, 2026-10-27). Brave lane 0300-0399. Idempotent. Never edit after merge.
   pay.LeaveType: the company's leave types. MaxEncashDays and MaxCarryForwardDays are NULL for no limit. The limits come from the law and are set by the Owner
   or the CA: no values are seeded here.
   pay.LeaveLedger: append only. One row per movement, Days signed. Balance is the sum, never stored. A mistake is corrected with an Adjustment row.
   EntryTypeID: 1 Opening, 2 Accrual, 3 Taken, 4 CarryForward, 5 Encashed, 6 Lapsed, 7 Adjustment.
   Opening, Accrual and CarryForward add. Taken, Encashed and Lapsed take away. Adjustment is either. LeaveYear is the year the financial year starts in. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'pay.LeaveType', N'U') IS NULL
BEGIN
    CREATE TABLE pay.LeaveType
    (
        LeaveTypeID         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LeaveType PRIMARY KEY,
        CompanyID           INT           NOT NULL CONSTRAINT FK_LeaveType_Company REFERENCES mst.Company (CompanyID),
        Code                NVARCHAR(10)  NOT NULL,
        Name                NVARCHAR(100) NOT NULL,
        IsEncashable        BIT           NOT NULL CONSTRAINT DF_LeaveType_Encashable DEFAULT 0,
        MaxEncashDays       DECIMAL(6,2)  NULL CONSTRAINT CK_LeaveType_Encash CHECK (MaxEncashDays IS NULL OR MaxEncashDays > 0),
        MaxCarryForwardDays DECIMAL(6,2)  NULL CONSTRAINT CK_LeaveType_Carry CHECK (MaxCarryForwardDays IS NULL OR MaxCarryForwardDays > 0),
        IsActive            BIT           NOT NULL CONSTRAINT DF_LeaveType_Active DEFAULT 1,
        CONSTRAINT UQ_LeaveType_Code UNIQUE (CompanyID, Code),
        CONSTRAINT UQ_LeaveType_Company UNIQUE (CompanyID, LeaveTypeID)
    );
END
GO
IF OBJECT_ID(N'pay.LeaveLedger', N'U') IS NULL
BEGIN
    CREATE TABLE pay.LeaveLedger
    (
        LedgerID        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LeaveLedger PRIMARY KEY,
        CompanyID       INT           NOT NULL,
        EmployeeID      INT           NOT NULL,
        LeaveTypeID     INT           NOT NULL,
        LeaveYear       SMALLINT      NOT NULL CONSTRAINT CK_LeaveLedger_Year CHECK (LeaveYear BETWEEN 2000 AND 9998),
        EntryDate       DATE          NOT NULL,
        EntryTypeID     TINYINT       NOT NULL CONSTRAINT CK_LeaveLedger_Type CHECK (EntryTypeID BETWEEN 1 AND 7),
        Days            DECIMAL(6,2)  NOT NULL CONSTRAINT CK_LeaveLedger_Days CHECK (Days <> 0),
        Note            NVARCHAR(200) NULL,
        CreatedUtc      DATETIME2(0)  NOT NULL CONSTRAINT DF_LeaveLedger_Created DEFAULT SYSUTCDATETIME(),
        CreatedByUserID INT           NULL,
        CONSTRAINT FK_LeaveLedger_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID),
        CONSTRAINT FK_LeaveLedger_Type FOREIGN KEY (CompanyID, LeaveTypeID) REFERENCES pay.LeaveType (CompanyID, LeaveTypeID),
        CONSTRAINT CK_LeaveLedger_Sign CHECK ((EntryTypeID IN (1, 2, 4) AND Days > 0) OR (EntryTypeID IN (3, 5, 6) AND Days < 0) OR EntryTypeID = 7)
    );
    CREATE INDEX IX_LeaveLedger_Employee ON pay.LeaveLedger (CompanyID, EmployeeID, LeaveTypeID) INCLUDE (Days);
END
GO
