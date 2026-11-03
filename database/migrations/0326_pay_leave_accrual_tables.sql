/* 0326_pay_leave_accrual_tables.sql
   Leave accrual setting and the monthly accrual marker (change request 0020, accepted by Sigma 2026-11-01; Owner decision 2026-11-01: leave is earned as days per days worked,
   set by the company). Brave lane 0300-0399. Idempotent. Never edit after merge. Tenant policy is in 0332.
   pay.LeaveAccrualSetting: one row per company and leave type. EarnedDays of leave for every PerWorkedDays days worked, for example 1 for every 20.
   RoundingID follows the RateRounding enum in Revolution.Core: 1 Nearest, 2 Up, 3 Down. MaxBalance NULL means no cap. A leave type with no row earns nothing by accrual.
   pay.LeaveAccrualRun: one row per company, leave type and month once the accrual is posted. The key refuses a second posting for the same month.
   The earned days themselves are Accrual rows (EntryTypeID 2) in the append-only pay.LeaveLedger. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'pay.LeaveAccrualSetting', N'U') IS NULL
BEGIN
    CREATE TABLE pay.LeaveAccrualSetting
    (
        CompanyID         INT           NOT NULL,
        LeaveTypeID       INT           NOT NULL,
        EarnedDays        DECIMAL(7,2)  NOT NULL CONSTRAINT CK_LeaveAccrualSetting_Earned CHECK (EarnedDays >= 0),
        PerWorkedDays     DECIMAL(7,2)  NOT NULL CONSTRAINT CK_LeaveAccrualSetting_Per CHECK (PerWorkedDays > 0),
        RoundingID        TINYINT       NOT NULL CONSTRAINT CK_LeaveAccrualSetting_Rounding CHECK (RoundingID BETWEEN 1 AND 3),
        MaxBalance        DECIMAL(7,2)  NULL CONSTRAINT CK_LeaveAccrualSetting_Max CHECK (MaxBalance IS NULL OR MaxBalance >= 0),
        UpdatedUtc        DATETIME2(0)  NOT NULL CONSTRAINT DF_LeaveAccrualSetting_Updated DEFAULT SYSUTCDATETIME(),
        UpdatedByUserID   INT           NULL,
        CONSTRAINT PK_LeaveAccrualSetting PRIMARY KEY (CompanyID, LeaveTypeID),
        CONSTRAINT FK_LeaveAccrualSetting_Type FOREIGN KEY (CompanyID, LeaveTypeID) REFERENCES pay.LeaveType (CompanyID, LeaveTypeID)
    );
END
GO
IF OBJECT_ID(N'pay.LeaveAccrualRun', N'U') IS NULL
BEGIN
    CREATE TABLE pay.LeaveAccrualRun
    (
        CompanyID       INT           NOT NULL,
        LeaveTypeID     INT           NOT NULL,
        AccrualMonth    DATE          NOT NULL CONSTRAINT CK_LeaveAccrualRun_Month CHECK (DAY(AccrualMonth) = 1),
        EmployeeCount   INT           NOT NULL,
        TotalDays       DECIMAL(10,2) NOT NULL,
        PostedUtc       DATETIME2(0)  NOT NULL CONSTRAINT DF_LeaveAccrualRun_Posted DEFAULT SYSUTCDATETIME(),
        PostedByUserID  INT           NULL,
        CONSTRAINT PK_LeaveAccrualRun PRIMARY KEY (CompanyID, LeaveTypeID, AccrualMonth),
        CONSTRAINT FK_LeaveAccrualRun_Type FOREIGN KEY (CompanyID, LeaveTypeID) REFERENCES pay.LeaveType (CompanyID, LeaveTypeID)
    );
END
GO
