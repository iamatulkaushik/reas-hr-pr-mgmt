/* 0300_pay_salary_tables.sql
   Payroll salary structure. Brave lane 0300-0399. Idempotent. Never edit after merge.
   pay.CompanyPolicy: one row per company. PayCycleStartDay 1 means calendar month, 26 means 26th to 25th (Owner).
   OT in EPF wage and OT in ESI wage are Owner options, default off. EmiCapPct is the most of net pay a loan EMI may take (Owner: 25).
   pay.SalaryComponent: earning heads a company defines. IsBasic and IsDa make the statutory base. IsOtBase marks other cash heads in the overtime
   rate (CA confirms). A head that is neither basic nor DA is a wage exclusion under the Code on Wages rule.
   pay.EmployeeSalary and pay.EmployeeSalaryLine: effective-dated salary. A revision is a new header with a later EffectiveFrom. Old rows are kept.
   Line Amount is always the monthly figure. A daily-wage employee (mst.Employee.PayTypeID 1) earns Amount / DailyWageDivisor per paid day (Owner: 26). */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'pay.CompanyPolicy', N'U') IS NULL
BEGIN
    CREATE TABLE pay.CompanyPolicy
    (
        CompanyID         INT           NOT NULL CONSTRAINT PK_CompanyPolicy PRIMARY KEY CONSTRAINT FK_CompanyPolicy_Company REFERENCES mst.Company (CompanyID),
        PayCycleStartDay  TINYINT       NOT NULL CONSTRAINT DF_CompanyPolicy_Cycle DEFAULT 1 CONSTRAINT CK_CompanyPolicy_Cycle CHECK (PayCycleStartDay IN (1, 26)),
        OtMultiplier      DECIMAL(7,4)  NOT NULL CONSTRAINT DF_CompanyPolicy_OtMult DEFAULT 2 CONSTRAINT CK_CompanyPolicy_OtMult CHECK (OtMultiplier >= 1),
        OtInEpfWage       BIT           NOT NULL CONSTRAINT DF_CompanyPolicy_OtEpf DEFAULT 0,
        OtInEsiWage       BIT           NOT NULL CONSTRAINT DF_CompanyPolicy_OtEsi DEFAULT 0,
        DailyWageDivisor  DECIMAL(7,2)  NOT NULL CONSTRAINT DF_CompanyPolicy_Div DEFAULT 26 CONSTRAINT CK_CompanyPolicy_Div CHECK (DailyWageDivisor > 0),
        EmiCapPct         DECIMAL(7,4)  NOT NULL CONSTRAINT DF_CompanyPolicy_Emi DEFAULT 25 CONSTRAINT CK_CompanyPolicy_Emi CHECK (EmiCapPct BETWEEN 0 AND 100),
        UpdatedUtc        DATETIME2(0)  NOT NULL CONSTRAINT DF_CompanyPolicy_Updated DEFAULT SYSUTCDATETIME(),
        UpdatedByUserID   INT           NULL
    );
END
GO
IF OBJECT_ID(N'pay.SalaryComponent', N'U') IS NULL
BEGIN
    CREATE TABLE pay.SalaryComponent
    (
        ComponentID  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalaryComponent PRIMARY KEY,
        CompanyID    INT           NOT NULL CONSTRAINT FK_SalaryComponent_Company REFERENCES mst.Company (CompanyID),
        Code         NVARCHAR(20)  NOT NULL,
        Name         NVARCHAR(100) NOT NULL,
        IsBasic      BIT           NOT NULL CONSTRAINT DF_SalaryComponent_Basic DEFAULT 0,
        IsDa         BIT           NOT NULL CONSTRAINT DF_SalaryComponent_Da DEFAULT 0,
        IsOtBase     BIT           NOT NULL CONSTRAINT DF_SalaryComponent_OtBase DEFAULT 0,
        SortOrder    SMALLINT      NOT NULL CONSTRAINT DF_SalaryComponent_Sort DEFAULT 100,
        IsActive     BIT           NOT NULL CONSTRAINT DF_SalaryComponent_Active DEFAULT 1,
        CreatedUtc   DATETIME2(0)  NOT NULL CONSTRAINT DF_SalaryComponent_Created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_SalaryComponent_Code UNIQUE (CompanyID, Code),
        CONSTRAINT UQ_SalaryComponent_Company UNIQUE (CompanyID, ComponentID),
        CONSTRAINT CK_SalaryComponent_Kind CHECK (NOT (IsBasic = 1 AND IsDa = 1))
    );
END
GO
IF OBJECT_ID(N'pay.EmployeeSalary', N'U') IS NULL
BEGIN
    CREATE TABLE pay.EmployeeSalary
    (
        SalaryID        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeSalary PRIMARY KEY,
        CompanyID       INT            NOT NULL CONSTRAINT FK_EmployeeSalary_Company REFERENCES mst.Company (CompanyID),
        EmployeeID      INT            NOT NULL,
        EffectiveFrom   DATE           NOT NULL,
        Reason          NVARCHAR(200)  NULL,
        CreatedUtc      DATETIME2(0)   NOT NULL CONSTRAINT DF_EmployeeSalary_Created DEFAULT SYSUTCDATETIME(),
        CreatedByUserID INT            NULL,
        CONSTRAINT UQ_EmployeeSalary_Date UNIQUE (CompanyID, EmployeeID, EffectiveFrom),
        CONSTRAINT UQ_EmployeeSalary_Company UNIQUE (CompanyID, SalaryID),
        CONSTRAINT FK_EmployeeSalary_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID)
    );
END
GO
IF OBJECT_ID(N'pay.EmployeeSalaryLine', N'U') IS NULL
BEGIN
    CREATE TABLE pay.EmployeeSalaryLine
    (
        CompanyID    INT           NOT NULL,
        SalaryID     INT           NOT NULL,
        ComponentID  INT           NOT NULL,
        Amount       DECIMAL(14,2) NOT NULL CONSTRAINT CK_EmployeeSalaryLine_Amount CHECK (Amount >= 0),
        CONSTRAINT PK_EmployeeSalaryLine PRIMARY KEY (SalaryID, ComponentID),
        CONSTRAINT FK_EmployeeSalaryLine_Salary FOREIGN KEY (CompanyID, SalaryID) REFERENCES pay.EmployeeSalary (CompanyID, SalaryID),
        CONSTRAINT FK_EmployeeSalaryLine_Component FOREIGN KEY (CompanyID, ComponentID) REFERENCES pay.SalaryComponent (CompanyID, ComponentID)
    );
END
GO
