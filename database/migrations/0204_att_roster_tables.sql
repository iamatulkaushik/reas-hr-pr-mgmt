/* 0204_att_roster_tables.sql
   Attendance roster data: employee shift assignment, company holidays, weekly off rule. Brave lane 0200-0299. Idempotent.
   Needs 0100 (mst.Employee) and 0200 (att.Shift).
   Overtime caps are statutory and effective-dated: they go to the stat lane (0400), not here. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'att.EmployeeShift', N'U') IS NULL
BEGIN
    CREATE TABLE att.EmployeeShift
    (
        EmployeeShiftID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeShift PRIMARY KEY,
        CompanyID     INT  NOT NULL,
        EmployeeID    INT  NOT NULL,
        ShiftID       INT  NOT NULL CONSTRAINT FK_EmployeeShift_Shift REFERENCES att.Shift (ShiftID),
        EffectiveFrom DATE NOT NULL,
        EffectiveTo   DATE NULL,
        CONSTRAINT FK_EmployeeShift_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID),
        CONSTRAINT UQ_EmployeeShift_From UNIQUE (CompanyID, EmployeeID, EffectiveFrom),
        CONSTRAINT CK_EmployeeShift_Range CHECK (EffectiveTo IS NULL OR EffectiveTo >= EffectiveFrom)
    );
END
GO
IF OBJECT_ID(N'att.Holiday', N'U') IS NULL
BEGIN
    CREATE TABLE att.Holiday
    (
        HolidayID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Holiday PRIMARY KEY,
        CompanyID   INT           NOT NULL CONSTRAINT FK_Holiday_Company REFERENCES mst.Company (CompanyID),
        HolidayDate DATE          NOT NULL,
        HolidayName NVARCHAR(100) NOT NULL,
        CONSTRAINT UQ_Holiday_Date UNIQUE (CompanyID, HolidayDate)
    );
END
GO
/* DaysMask: bit 0 Monday ... bit 6 Sunday. The row with the latest EffectiveFrom on or before a date applies. */
IF OBJECT_ID(N'att.WeeklyOffRule', N'U') IS NULL
BEGIN
    CREATE TABLE att.WeeklyOffRule
    (
        CompanyID     INT     NOT NULL CONSTRAINT FK_WeeklyOffRule_Company REFERENCES mst.Company (CompanyID),
        EffectiveFrom DATE    NOT NULL,
        DaysMask      TINYINT NOT NULL CONSTRAINT CK_WeeklyOffRule_Mask CHECK (DaysMask BETWEEN 0 AND 127),
        CONSTRAINT PK_WeeklyOffRule PRIMARY KEY (CompanyID, EffectiveFrom)
    );
END
GO
