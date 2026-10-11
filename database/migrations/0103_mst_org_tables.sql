/* 0102_mst_org_tables.sql
   Factory (work location), department, section, grade, designation. Per company. Idempotent. Never edit after merge.
   Department belongs to a factory, section to a department. Each table has UNIQUE (CompanyID, <ID>) so child tables use a
   composite foreign key: a row of company A can never point at a row of company B, whatever the application does.
   Factory.StateID drives professional tax and state labour limits. Designation.CategoryID feeds minimum-wage checks later:
   1 Unskilled, 2 Semi-skilled, 3 Skilled, 4 Highly skilled, 5 Clerical, 6 Supervisory, 7 Managerial. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'mst.Factory', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Factory
    (
        FactoryID       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Factory PRIMARY KEY,
        CompanyID       INT           NOT NULL CONSTRAINT FK_Factory_Company REFERENCES mst.Company (CompanyID),
        FactoryCode     NVARCHAR(10)  NOT NULL,
        FactoryName     NVARCHAR(150) NOT NULL,
        AddressLine1    NVARCHAR(150) NULL,
        AddressLine2    NVARCHAR(150) NULL,
        City            NVARCHAR(100) NULL,
        StateID         TINYINT       NOT NULL CONSTRAINT FK_Factory_State REFERENCES mst.[State] (StateID),
        FactoryLicenceNo NVARCHAR(50) NULL,
        LicenceValidTo  DATE          NULL,
        MaxWorkers      INT           NULL CONSTRAINT CK_Factory_MaxWorkers CHECK (MaxWorkers IS NULL OR MaxWorkers > 0),
        ManagerName     NVARCHAR(120) NULL,
        EpfSubCode      NVARCHAR(20)  NULL,
        EsiSubCode      NVARCHAR(17)  NULL,
        IsActive        BIT           NOT NULL CONSTRAINT DF_Factory_IsActive DEFAULT 1,
        CreatedUtc      DATETIME2(0)  NOT NULL CONSTRAINT DF_Factory_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_Factory_Code UNIQUE (CompanyID, FactoryCode),
        CONSTRAINT UQ_Factory_Name UNIQUE (CompanyID, FactoryName),
        CONSTRAINT UQ_Factory_Company_Id UNIQUE (CompanyID, FactoryID)
    );
END
GO
IF OBJECT_ID(N'mst.Department', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Department
    (
        DepartmentID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Department PRIMARY KEY,
        CompanyID      INT           NOT NULL,
        FactoryID      INT           NOT NULL,
        DepartmentCode NVARCHAR(10)  NOT NULL,
        DepartmentName NVARCHAR(100) NOT NULL,
        IsHazardous    BIT           NOT NULL CONSTRAINT DF_Department_IsHazardous DEFAULT 0,
        IsActive       BIT           NOT NULL CONSTRAINT DF_Department_IsActive DEFAULT 1,
        CONSTRAINT FK_Department_Factory FOREIGN KEY (CompanyID, FactoryID) REFERENCES mst.Factory (CompanyID, FactoryID),
        CONSTRAINT UQ_Department_Code UNIQUE (CompanyID, FactoryID, DepartmentCode),
        CONSTRAINT UQ_Department_Company_Id UNIQUE (CompanyID, DepartmentID),
        CONSTRAINT UQ_Department_Factory_Id UNIQUE (CompanyID, FactoryID, DepartmentID)
    );
END
GO
IF OBJECT_ID(N'mst.Section', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Section
    (
        SectionID    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Section PRIMARY KEY,
        CompanyID    INT           NOT NULL,
        DepartmentID INT           NOT NULL,
        SectionCode  NVARCHAR(10)  NOT NULL,
        SectionName  NVARCHAR(100) NOT NULL,
        IsActive     BIT           NOT NULL CONSTRAINT DF_Section_IsActive DEFAULT 1,
        CONSTRAINT FK_Section_Department FOREIGN KEY (CompanyID, DepartmentID) REFERENCES mst.Department (CompanyID, DepartmentID),
        CONSTRAINT UQ_Section_Code UNIQUE (CompanyID, DepartmentID, SectionCode),
        CONSTRAINT UQ_Section_Company_Id UNIQUE (CompanyID, SectionID)
    );
END
GO
IF OBJECT_ID(N'mst.Grade', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Grade
    (
        GradeID    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Grade PRIMARY KEY,
        CompanyID  INT           NOT NULL CONSTRAINT FK_Grade_Company REFERENCES mst.Company (CompanyID),
        GradeCode  NVARCHAR(10)  NOT NULL,
        GradeName  NVARCHAR(60)  NOT NULL,
        GradeLevel INT           NOT NULL,
        MinGross   DECIMAL(14,2) NULL,
        MaxGross   DECIMAL(14,2) NULL,
        IsActive   BIT           NOT NULL CONSTRAINT DF_Grade_IsActive DEFAULT 1,
        CONSTRAINT CK_Grade_Range CHECK (MinGross IS NULL OR MaxGross IS NULL OR MaxGross >= MinGross),
        CONSTRAINT UQ_Grade_Code UNIQUE (CompanyID, GradeCode),
        CONSTRAINT UQ_Grade_Company_Id UNIQUE (CompanyID, GradeID)
    );
END
GO
IF OBJECT_ID(N'mst.Designation', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Designation
    (
        DesignationID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Designation PRIMARY KEY,
        CompanyID       INT           NOT NULL CONSTRAINT FK_Designation_Company REFERENCES mst.Company (CompanyID),
        DesignationCode NVARCHAR(10)  NOT NULL,
        DesignationName NVARCHAR(100) NOT NULL,
        GradeID         INT           NULL,
        CategoryID      TINYINT       NOT NULL CONSTRAINT CK_Designation_Category CHECK (CategoryID BETWEEN 1 AND 7),
        IsActive        BIT           NOT NULL CONSTRAINT DF_Designation_IsActive DEFAULT 1,
        CONSTRAINT FK_Designation_Grade FOREIGN KEY (CompanyID, GradeID) REFERENCES mst.Grade (CompanyID, GradeID),
        CONSTRAINT UQ_Designation_Code UNIQUE (CompanyID, DesignationCode),
        CONSTRAINT UQ_Designation_Company_Id UNIQUE (CompanyID, DesignationID)
    );
END
GO
