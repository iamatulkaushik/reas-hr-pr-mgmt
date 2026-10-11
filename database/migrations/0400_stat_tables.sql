/* 0400_stat_tables.sql
   Statutory rates and rules as effective-dated rows (ADR 0004). Shared by all companies, so no CompanyID and no tenant policy.
   A rule applies from EffectiveFrom until a later row takes over: the reader takes the latest row with EffectiveFrom <= the date asked.
   No overlaps are possible, because there is no end date to get wrong.
   Every row carries Verified (0 or 1), VerifiedOn and SourceRef. A row is loaded as Verified = 0 and flipped only after the CA signs it off
   (PLAN phase 5 gate; setting RateVerificationRequired in cfg.Setting). NO RATES ARE LOADED BY THIS SCRIPT. Idempotent. Never edit after merge.
   Rows are added by new migration scripts, so cfg.SchemaVersion records who and what, with a checksum.
   Codes: GenderID 0 all, 1 male, 2 female. FrequencyID 1 monthly, 2 half-yearly, 3 yearly. RegimeID 1 old, 2 new.
          AgeBandID 1 below 60, 2 senior 60 to 79, 3 super senior 80 and above. FinancialYear is the year it starts in (2026 = 1 April 2026 to 31 March 2027).
          RoundingID 1 nearest rupee, 2 up to next rupee, 3 down. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'stat.EpfRate', N'U') IS NULL
BEGIN
    CREATE TABLE stat.EpfRate
    (
        EffectiveFrom      DATE          NOT NULL CONSTRAINT PK_EpfRate PRIMARY KEY,
        EmployeePct        DECIMAL(7,4)  NOT NULL,
        EmployerEpfPct     DECIMAL(7,4)  NOT NULL,
        EmployerEpsPct     DECIMAL(7,4)  NOT NULL,
        EdliPct            DECIMAL(7,4)  NOT NULL,
        AdminPct           DECIMAL(7,4)  NOT NULL,
        AdminMinimum       DECIMAL(12,2) NOT NULL CONSTRAINT DF_EpfRate_AdminMin DEFAULT 0,
        WageCeiling        DECIMAL(12,2) NOT NULL,
        EdliWageCeiling    DECIMAL(12,2) NOT NULL,
        Verified           BIT           NOT NULL CONSTRAINT DF_EpfRate_Verified DEFAULT 0,
        VerifiedOn         DATE          NULL,
        SourceRef          NVARCHAR(300) NULL
    );
END
GO
IF OBJECT_ID(N'stat.EsiRate', N'U') IS NULL
BEGIN
    CREATE TABLE stat.EsiRate
    (
        EffectiveFrom       DATE          NOT NULL CONSTRAINT PK_EsiRate PRIMARY KEY,
        EmployeePct         DECIMAL(7,4)  NOT NULL,
        EmployerPct         DECIMAL(7,4)  NOT NULL,
        WageCeiling         DECIMAL(12,2) NOT NULL,
        DailyWageExemption  DECIMAL(12,2) NOT NULL CONSTRAINT DF_EsiRate_DailyExempt DEFAULT 0,
        RoundingID          TINYINT       NOT NULL CONSTRAINT DF_EsiRate_Rounding DEFAULT 2 CONSTRAINT CK_EsiRate_Rounding CHECK (RoundingID BETWEEN 1 AND 3),
        Verified            BIT           NOT NULL CONSTRAINT DF_EsiRate_Verified DEFAULT 0,
        VerifiedOn          DATE          NULL,
        SourceRef           NVARCHAR(300) NULL
    );
END
GO
IF OBJECT_ID(N'stat.PtRule', N'U') IS NULL
BEGIN
    CREATE TABLE stat.PtRule
    (
        StateID       TINYINT       NOT NULL CONSTRAINT FK_PtRule_State REFERENCES mst.[State] (StateID),
        EffectiveFrom DATE          NOT NULL,
        FrequencyID   TINYINT       NOT NULL CONSTRAINT CK_PtRule_Frequency CHECK (FrequencyID BETWEEN 1 AND 3),
        SpecialMonth  TINYINT       NULL CONSTRAINT CK_PtRule_Month CHECK (SpecialMonth IS NULL OR SpecialMonth BETWEEN 1 AND 12),
        Verified      BIT           NOT NULL CONSTRAINT DF_PtRule_Verified DEFAULT 0,
        VerifiedOn    DATE          NULL,
        SourceRef     NVARCHAR(300) NULL,
        CONSTRAINT PK_PtRule PRIMARY KEY (StateID, EffectiveFrom)
    );
END
GO
IF OBJECT_ID(N'stat.PtSlab', N'U') IS NULL
BEGIN
    CREATE TABLE stat.PtSlab
    (
        StateID         TINYINT       NOT NULL,
        EffectiveFrom   DATE          NOT NULL,
        GenderID        TINYINT       NOT NULL CONSTRAINT DF_PtSlab_Gender DEFAULT 0 CONSTRAINT CK_PtSlab_Gender CHECK (GenderID BETWEEN 0 AND 2),
        SlabNo          TINYINT       NOT NULL,
        FromAmount      DECIMAL(12,2) NOT NULL,
        ToAmount        DECIMAL(12,2) NULL,
        TaxAmount       DECIMAL(12,2) NOT NULL,
        SpecialMonthTax DECIMAL(12,2) NULL,
        CONSTRAINT PK_PtSlab PRIMARY KEY (StateID, EffectiveFrom, GenderID, SlabNo),
        CONSTRAINT FK_PtSlab_Rule FOREIGN KEY (StateID, EffectiveFrom) REFERENCES stat.PtRule (StateID, EffectiveFrom),
        CONSTRAINT CK_PtSlab_Range CHECK (ToAmount IS NULL OR ToAmount > FromAmount)
    );
END
GO
IF OBJECT_ID(N'stat.TdsParam', N'U') IS NULL
BEGIN
    CREATE TABLE stat.TdsParam
    (
        FinancialYear      SMALLINT      NOT NULL,
        RegimeID           TINYINT       NOT NULL CONSTRAINT CK_TdsParam_Regime CHECK (RegimeID IN (1, 2)),
        StandardDeduction  DECIMAL(12,2) NOT NULL,
        RebateIncomeLimit  DECIMAL(14,2) NOT NULL,
        RebateMaximum      DECIMAL(12,2) NOT NULL,
        CessPct            DECIMAL(7,4)  NOT NULL,
        Verified           BIT           NOT NULL CONSTRAINT DF_TdsParam_Verified DEFAULT 0,
        VerifiedOn         DATE          NULL,
        SourceRef          NVARCHAR(300) NULL,
        CONSTRAINT PK_TdsParam PRIMARY KEY (FinancialYear, RegimeID)
    );
END
GO
IF OBJECT_ID(N'stat.TdsSlab', N'U') IS NULL
BEGIN
    CREATE TABLE stat.TdsSlab
    (
        FinancialYear SMALLINT      NOT NULL,
        RegimeID      TINYINT       NOT NULL,
        AgeBandID     TINYINT       NOT NULL CONSTRAINT CK_TdsSlab_Age CHECK (AgeBandID BETWEEN 1 AND 3),
        SlabNo        TINYINT       NOT NULL,
        FromAmount    DECIMAL(14,2) NOT NULL,
        ToAmount      DECIMAL(14,2) NULL,
        RatePct       DECIMAL(7,4)  NOT NULL,
        CONSTRAINT PK_TdsSlab PRIMARY KEY (FinancialYear, RegimeID, AgeBandID, SlabNo),
        CONSTRAINT FK_TdsSlab_Param FOREIGN KEY (FinancialYear, RegimeID) REFERENCES stat.TdsParam (FinancialYear, RegimeID),
        CONSTRAINT CK_TdsSlab_Range CHECK (ToAmount IS NULL OR ToAmount > FromAmount)
    );
END
GO
IF OBJECT_ID(N'stat.TdsSurcharge', N'U') IS NULL
BEGIN
    CREATE TABLE stat.TdsSurcharge
    (
        FinancialYear SMALLINT      NOT NULL,
        RegimeID      TINYINT       NOT NULL,
        SlabNo        TINYINT       NOT NULL,
        FromIncome    DECIMAL(14,2) NOT NULL,
        ToIncome      DECIMAL(14,2) NULL,
        RatePct       DECIMAL(7,4)  NOT NULL,
        CONSTRAINT PK_TdsSurcharge PRIMARY KEY (FinancialYear, RegimeID, SlabNo),
        CONSTRAINT FK_TdsSurcharge_Param FOREIGN KEY (FinancialYear, RegimeID) REFERENCES stat.TdsParam (FinancialYear, RegimeID),
        CONSTRAINT CK_TdsSurcharge_Range CHECK (ToIncome IS NULL OR ToIncome > FromIncome)
    );
END
GO
IF OBJECT_ID(N'stat.OvertimeLimit', N'U') IS NULL
BEGIN
    CREATE TABLE stat.OvertimeLimit
    (
        StateID             TINYINT       NOT NULL CONSTRAINT FK_OvertimeLimit_State REFERENCES mst.[State] (StateID),
        EffectiveFrom       DATE          NOT NULL,
        DailyMaxMinutes     INT           NULL,
        WeeklyMaxMinutes    INT           NULL,
        QuarterlyMaxMinutes INT           NULL,
        Verified            BIT           NOT NULL CONSTRAINT DF_OvertimeLimit_Verified DEFAULT 0,
        VerifiedOn          DATE          NULL,
        SourceRef           NVARCHAR(300) NULL,
        CONSTRAINT PK_OvertimeLimit PRIMARY KEY (StateID, EffectiveFrom)
    );
END
GO
IF OBJECT_ID(N'stat.GratuityRule', N'U') IS NULL
BEGIN
    CREATE TABLE stat.GratuityRule
    (
        EffectiveFrom      DATE          NOT NULL CONSTRAINT PK_GratuityRule PRIMARY KEY,
        MinYearsRegular    TINYINT       NOT NULL,
        MinYearsFixedTerm  TINYINT       NOT NULL,
        DaysPerYear        DECIMAL(5,2)  NOT NULL,
        MonthDivisor       DECIMAL(5,2)  NOT NULL,
        RoundUpAfterMonths TINYINT       NOT NULL,
        CapAmount          DECIMAL(14,2) NOT NULL,
        Verified           BIT           NOT NULL CONSTRAINT DF_GratuityRule_Verified DEFAULT 0,
        VerifiedOn         DATE          NULL,
        SourceRef          NVARCHAR(300) NULL
    );
END
GO
IF OBJECT_ID(N'stat.WageRule', N'U') IS NULL
BEGIN
    CREATE TABLE stat.WageRule
    (
        EffectiveFrom DATE          NOT NULL CONSTRAINT PK_WageRule PRIMARY KEY,
        UseCodeWage   BIT           NOT NULL,
        WageFloorPct  DECIMAL(5,2)  NOT NULL,
        Notes         NVARCHAR(300) NULL,
        Verified      BIT           NOT NULL CONSTRAINT DF_WageRule_Verified DEFAULT 0,
        VerifiedOn    DATE          NULL,
        SourceRef     NVARCHAR(300) NULL
    );
END
GO
IF OBJECT_ID(N'stat.RuleChangeLog', N'U') IS NULL
BEGIN
    CREATE TABLE stat.RuleChangeLog
    (
        ChangeID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RuleChangeLog PRIMARY KEY,
        Area          NVARCHAR(30)  NOT NULL,
        Description   NVARCHAR(500) NOT NULL,
        EffectiveFrom DATE          NULL,
        SourceRef     NVARCHAR(300) NULL,
        EnteredBy     NVARCHAR(128) NOT NULL CONSTRAINT DF_RuleChangeLog_EnteredBy DEFAULT SUSER_SNAME(),
        EnteredUtc    DATETIME2(0)  NOT NULL CONSTRAINT DF_RuleChangeLog_EnteredUtc DEFAULT SYSUTCDATETIME()
    );
END
GO
