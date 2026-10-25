/* 0312_pay_tds_tables.sql
   Phase 6 TDS on salary, declarations side. Brave lane 0300-0399. Idempotent. Never edit after merge: add a new number.
   Form 124 (was 12BB) is the employee's declaration. The Associate enters it; there is no employee self-service (Owner).
   pay.TdsDeclaration: one per employee and financial year (FinancialYear is the start year, 2026 is 2026-27). RegimeID 1 old, 2 new, matching
   stat.TdsParam. StatusID 1 Draft, 2 Submitted, 3 Locked. Only a Locked declaration refuses changes.
   pay.TdsDeclarationItem: one deduction claim, several may share a section (for example PF and LIC under one section). SectionCode and SubLimitKey
   match stat.TdsDeductionLimit; the caps are applied by the calculator, not here. VerifiedAmount is set when proof is checked.
   pay.TdsHraRent: rent paid for a range of months (YYYYMM). Form 124 asks for the landlord's name and relationship, and for the landlord PAN when the
   yearly rent is above the limit. That limit is not in the database (CR 0018), so the service checks it. The PAN is encrypted in the application (ADR 0011):
   LandlordPanEnc and LandlordPanLast4 are both set or both empty.
   pay.TdsPrevEmployer: income and tax from the previous employer in the same year (Form 12B, 122 from 2026-27). Perquisites are optional (Owner).
   pay.TdsMonthLine: what the run worked out for one employee: kept so the figures can be explained and so Form 130 and Form 138 can be built. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'pay.TdsDeclaration', N'U') IS NULL
BEGIN
    CREATE TABLE pay.TdsDeclaration
    (
        DeclarationID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TdsDeclaration PRIMARY KEY,
        CompanyID       INT          NOT NULL CONSTRAINT FK_TdsDeclaration_Company REFERENCES mst.Company (CompanyID),
        EmployeeID      INT          NOT NULL,
        FinancialYear   SMALLINT     NOT NULL CONSTRAINT CK_TdsDeclaration_Year CHECK (FinancialYear BETWEEN 2000 AND 2200),
        RegimeID        TINYINT      NOT NULL CONSTRAINT CK_TdsDeclaration_Regime CHECK (RegimeID IN (1, 2)),
        StatusID        TINYINT      NOT NULL CONSTRAINT DF_TdsDeclaration_Status DEFAULT 1 CONSTRAINT CK_TdsDeclaration_Status CHECK (StatusID IN (1, 2, 3)),
        CreatedUtc      DATETIME2(0) NOT NULL CONSTRAINT DF_TdsDeclaration_Created DEFAULT SYSUTCDATETIME(),
        CreatedByUserID INT          NULL,
        UpdatedUtc      DATETIME2(0) NULL,
        UpdatedByUserID INT          NULL,
        CONSTRAINT UQ_TdsDeclaration_Company UNIQUE (CompanyID, DeclarationID),
        CONSTRAINT UQ_TdsDeclaration_Year UNIQUE (CompanyID, EmployeeID, FinancialYear),
        CONSTRAINT FK_TdsDeclaration_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID)
    );
END
GO
IF OBJECT_ID(N'pay.TdsDeclarationItem', N'U') IS NULL
BEGIN
    CREATE TABLE pay.TdsDeclarationItem
    (
        CompanyID      INT           NOT NULL,
        DeclarationID  INT           NOT NULL,
        ItemNo         SMALLINT      NOT NULL CONSTRAINT CK_TdsDeclarationItem_No CHECK (ItemNo >= 1),
        SectionCode    NVARCHAR(20)  NOT NULL CONSTRAINT CK_TdsDeclarationItem_Section CHECK (LEN(LTRIM(RTRIM(SectionCode))) > 0),
        SubLimitKey    NVARCHAR(40)  NOT NULL CONSTRAINT DF_TdsDeclarationItem_SubKey DEFAULT N'',
        Description    NVARCHAR(100) NULL,
        DeclaredAmount DECIMAL(14,2) NOT NULL CONSTRAINT CK_TdsDeclarationItem_Declared CHECK (DeclaredAmount >= 0),
        ProofRef       NVARCHAR(200) NULL,
        VerifiedAmount DECIMAL(14,2) NULL CONSTRAINT CK_TdsDeclarationItem_Verified CHECK (VerifiedAmount IS NULL OR VerifiedAmount >= 0),
        CONSTRAINT PK_TdsDeclarationItem PRIMARY KEY (DeclarationID, ItemNo),
        CONSTRAINT FK_TdsDeclarationItem_Declaration FOREIGN KEY (CompanyID, DeclarationID) REFERENCES pay.TdsDeclaration (CompanyID, DeclarationID)
    );
END
GO
IF OBJECT_ID(N'pay.TdsHraRent', N'U') IS NULL
BEGIN
    CREATE TABLE pay.TdsHraRent
    (
        CompanyID            INT            NOT NULL,
        DeclarationID        INT            NOT NULL,
        RentNo               SMALLINT       NOT NULL CONSTRAINT CK_TdsHraRent_No CHECK (RentNo >= 1),
        FromMonthKey         INT            NOT NULL CONSTRAINT CK_TdsHraRent_From CHECK (FromMonthKey BETWEEN 190001 AND 999912 AND FromMonthKey % 100 BETWEEN 1 AND 12),
        ToMonthKey           INT            NOT NULL CONSTRAINT CK_TdsHraRent_To CHECK (ToMonthKey BETWEEN 190001 AND 999912 AND ToMonthKey % 100 BETWEEN 1 AND 12),
        MonthlyRent          DECIMAL(14,2)  NOT NULL CONSTRAINT CK_TdsHraRent_Rent CHECK (MonthlyRent > 0),
        City                 NVARCHAR(100)  NOT NULL CONSTRAINT CK_TdsHraRent_City CHECK (LEN(LTRIM(RTRIM(City))) > 0),
        LandlordName         NVARCHAR(150)  NOT NULL CONSTRAINT CK_TdsHraRent_Name CHECK (LEN(LTRIM(RTRIM(LandlordName))) > 0),
        LandlordRelationship NVARCHAR(50)   NOT NULL CONSTRAINT CK_TdsHraRent_Relation CHECK (LEN(LTRIM(RTRIM(LandlordRelationship))) > 0),
        LandlordAddress      NVARCHAR(300)  NULL,
        LandlordPanEnc       VARBINARY(256) NULL,
        LandlordPanLast4     CHAR(4)        NULL,
        IdKeyVersion         TINYINT        NOT NULL CONSTRAINT DF_TdsHraRent_KeyVersion DEFAULT 1,
        CONSTRAINT PK_TdsHraRent PRIMARY KEY (DeclarationID, RentNo),
        CONSTRAINT CK_TdsHraRent_Range CHECK (ToMonthKey >= FromMonthKey),
        CONSTRAINT CK_TdsHraRent_Pan CHECK ((LandlordPanEnc IS NULL AND LandlordPanLast4 IS NULL) OR (LandlordPanEnc IS NOT NULL AND LandlordPanLast4 IS NOT NULL)),
        CONSTRAINT FK_TdsHraRent_Declaration FOREIGN KEY (CompanyID, DeclarationID) REFERENCES pay.TdsDeclaration (CompanyID, DeclarationID)
    );
END
GO
IF OBJECT_ID(N'pay.TdsPrevEmployer', N'U') IS NULL
BEGIN
    CREATE TABLE pay.TdsPrevEmployer
    (
        CompanyID       INT            NOT NULL,
        DeclarationID   INT            NOT NULL CONSTRAINT PK_TdsPrevEmployer PRIMARY KEY,
        EmployerName    NVARCHAR(150)  NOT NULL CONSTRAINT CK_TdsPrevEmployer_Name CHECK (LEN(LTRIM(RTRIM(EmployerName))) > 0),
        EmployerTan     CHAR(10)       NULL,
        SalaryIncome    DECIMAL(14,2)  NOT NULL CONSTRAINT CK_TdsPrevEmployer_Salary CHECK (SalaryIncome >= 0),
        Perquisites     DECIMAL(14,2)  NOT NULL CONSTRAINT DF_TdsPrevEmployer_Perq DEFAULT 0 CONSTRAINT CK_TdsPrevEmployer_Perq CHECK (Perquisites >= 0),
        ProvidentFund   DECIMAL(14,2)  NOT NULL CONSTRAINT DF_TdsPrevEmployer_Pf DEFAULT 0 CONSTRAINT CK_TdsPrevEmployer_Pf CHECK (ProvidentFund >= 0),
        ProfessionalTax DECIMAL(14,2)  NOT NULL CONSTRAINT DF_TdsPrevEmployer_Pt DEFAULT 0 CONSTRAINT CK_TdsPrevEmployer_Pt CHECK (ProfessionalTax >= 0),
        TaxDeducted     DECIMAL(14,2)  NOT NULL CONSTRAINT CK_TdsPrevEmployer_Tax CHECK (TaxDeducted >= 0),
        CONSTRAINT FK_TdsPrevEmployer_Declaration FOREIGN KEY (CompanyID, DeclarationID) REFERENCES pay.TdsDeclaration (CompanyID, DeclarationID)
    );
END
GO
IF OBJECT_ID(N'pay.TdsMonthLine', N'U') IS NULL
BEGIN
    CREATE TABLE pay.TdsMonthLine
    (
        CompanyID            INT           NOT NULL,
        RunID                INT           NOT NULL,
        EmployeeID           INT           NOT NULL,
        FinancialYear        SMALLINT      NOT NULL,
        RegimeID             TINYINT       NOT NULL CONSTRAINT CK_TdsMonthLine_Regime CHECK (RegimeID IN (1, 2)),
        ProjectedIncome      DECIMAL(14,2) NOT NULL CONSTRAINT CK_TdsMonthLine_Income CHECK (ProjectedIncome >= 0),
        TaxableIncome        DECIMAL(14,2) NOT NULL CONSTRAINT CK_TdsMonthLine_Taxable CHECK (TaxableIncome >= 0),
        ProjectedAnnualTax   DECIMAL(14,2) NOT NULL CONSTRAINT CK_TdsMonthLine_Annual CHECK (ProjectedAnnualTax >= 0),
        TaxDeductedBefore    DECIMAL(14,2) NOT NULL CONSTRAINT CK_TdsMonthLine_Before CHECK (TaxDeductedBefore >= 0),
        RemainingMonths      TINYINT       NOT NULL CONSTRAINT CK_TdsMonthLine_Months CHECK (RemainingMonths BETWEEN 1 AND 12),
        TdsThisMonth         DECIMAL(14,2) NOT NULL CONSTRAINT CK_TdsMonthLine_Tds CHECK (TdsThisMonth >= 0),
        CONSTRAINT PK_TdsMonthLine PRIMARY KEY (RunID, EmployeeID),
        CONSTRAINT FK_TdsMonthLine_Line FOREIGN KEY (RunID, EmployeeID) REFERENCES pay.PayrollRunLine (RunID, EmployeeID),
        CONSTRAINT FK_TdsMonthLine_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID)
    );
END
GO
