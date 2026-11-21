/* 0210_att_monthly_tables.sql
   Monthly attendance (ADR 0026, work order docs/phase4b-plan-sigma.md). Brave lane 0200-0299. Idempotent. Never edit after merge. Policy is in 0211.
   att.AttendanceMonthly: one row per employee per company month. PeriodStart is always the first of the month. The counts are whole days (0 to 31) as on the
   Associate's sheet; half days are counted as days here (the sheet total must equal the employee's days in the month) and paid as half in payroll.
   Meanings are those of MonthSummary: days paid = present + weekly off + holiday + paid leave + half the half days; loss of pay = absent + half the half days.
   att.AttendanceSetting: the company's attendance mode, 1 Monthly (the default; no row means Monthly), 2 Daily (Platinum only, checked in usp_AttendanceSetting_Set).
   Needs mst.Employee (0105) and its (CompanyID, EmployeeID) key. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'att.AttendanceMonthly', N'U') IS NULL
BEGIN
    CREATE TABLE att.AttendanceMonthly
    (
        CompanyID       INT          NOT NULL CONSTRAINT FK_AttendanceMonthly_Company REFERENCES mst.Company (CompanyID),
        EmployeeID      INT          NOT NULL,
        PeriodStart     DATE         NOT NULL CONSTRAINT CK_AttendanceMonthly_Period CHECK (DAY(PeriodStart) = 1),
        PresentDays     TINYINT      NOT NULL CONSTRAINT CK_AttendanceMonthly_Present CHECK (PresentDays BETWEEN 0 AND 31),
        HalfDays        TINYINT      NOT NULL CONSTRAINT DF_AttendanceMonthly_Half DEFAULT 0 CONSTRAINT CK_AttendanceMonthly_Half CHECK (HalfDays BETWEEN 0 AND 31),
        WeeklyOffDays   TINYINT      NOT NULL CONSTRAINT DF_AttendanceMonthly_Wo DEFAULT 0 CONSTRAINT CK_AttendanceMonthly_Wo CHECK (WeeklyOffDays BETWEEN 0 AND 31),
        HolidayDays     TINYINT      NOT NULL CONSTRAINT DF_AttendanceMonthly_Hol DEFAULT 0 CONSTRAINT CK_AttendanceMonthly_Hol CHECK (HolidayDays BETWEEN 0 AND 31),
        PaidLeaveDays   TINYINT      NOT NULL CONSTRAINT DF_AttendanceMonthly_Pl DEFAULT 0 CONSTRAINT CK_AttendanceMonthly_Pl CHECK (PaidLeaveDays BETWEEN 0 AND 31),
        AbsentDays      TINYINT      NOT NULL CONSTRAINT DF_AttendanceMonthly_Abs DEFAULT 0 CONSTRAINT CK_AttendanceMonthly_Abs CHECK (AbsentDays BETWEEN 0 AND 31),
        OvertimeHours   DECIMAL(5,2) NOT NULL CONSTRAINT DF_AttendanceMonthly_Ot DEFAULT 0 CONSTRAINT CK_AttendanceMonthly_Ot CHECK (OvertimeHours BETWEEN 0 AND 744),
        Remarks         NVARCHAR(100) NULL,
        UpdatedUtc      DATETIME2(0) NOT NULL CONSTRAINT DF_AttendanceMonthly_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        UpdatedByUserID INT          NULL,
        CONSTRAINT PK_AttendanceMonthly PRIMARY KEY (CompanyID, EmployeeID, PeriodStart),
        CONSTRAINT FK_AttendanceMonthly_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID)
    );
    CREATE INDEX IX_AttendanceMonthly_Period ON att.AttendanceMonthly (CompanyID, PeriodStart);
END
GO
IF OBJECT_ID(N'att.AttendanceSetting', N'U') IS NULL
BEGIN
    CREATE TABLE att.AttendanceSetting
    (
        CompanyID  INT          NOT NULL CONSTRAINT PK_AttendanceSetting PRIMARY KEY
                                CONSTRAINT FK_AttendanceSetting_Company REFERENCES mst.Company (CompanyID),
        Mode       TINYINT      NOT NULL CONSTRAINT CK_AttendanceSetting_Mode CHECK (Mode IN (1, 2)),
        UpdatedUtc DATETIME2(0) NOT NULL CONSTRAINT DF_AttendanceSetting_UpdatedUtc DEFAULT SYSUTCDATETIME()
    );
END
GO
