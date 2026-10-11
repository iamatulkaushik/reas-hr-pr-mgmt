/* 0100_mst_masters_employee.sql
   Masters and employees. Brave lane 0100-0199 (ADR 0008). Idempotent. Never edit after merge.
   Sensitive ids (Aadhaar, PAN, UAN, ESI IP, bank account) are stored as ciphertext only (ADR 0004).
   AadhaarIndex and PanIndex are keyed hashes (blind index) so duplicates are caught without decrypting.
   Both come from IFieldCrypto (change request 0010). Until it is accepted, the application cannot write these columns. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'mst.Factory', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Factory
    (
        FactoryID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Factory PRIMARY KEY,
        CompanyID   INT           NOT NULL CONSTRAINT FK_Factory_Company REFERENCES mst.Company (CompanyID),
        FactoryName NVARCHAR(200) NOT NULL,
        LicenseNo   NVARCHAR(50)  NULL,
        Address     NVARCHAR(500) NULL,
        IsActive    BIT           NOT NULL CONSTRAINT DF_Factory_IsActive DEFAULT 1,
        CONSTRAINT UQ_Factory_Name UNIQUE (CompanyID, FactoryName)
    );
END
GO
IF OBJECT_ID(N'mst.Department', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Department
    (
        DepartmentID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Department PRIMARY KEY,
        CompanyID      INT           NOT NULL CONSTRAINT FK_Department_Company REFERENCES mst.Company (CompanyID),
        DepartmentName NVARCHAR(100) NOT NULL,
        IsActive       BIT           NOT NULL CONSTRAINT DF_Department_IsActive DEFAULT 1,
        CONSTRAINT UQ_Department_Name UNIQUE (CompanyID, DepartmentName)
    );
END
GO
IF OBJECT_ID(N'mst.Designation', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Designation
    (
        DesignationID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Designation PRIMARY KEY,
        CompanyID       INT           NOT NULL CONSTRAINT FK_Designation_Company REFERENCES mst.Company (CompanyID),
        DesignationName NVARCHAR(100) NOT NULL,
        IsActive        BIT           NOT NULL CONSTRAINT DF_Designation_IsActive DEFAULT 1,
        CONSTRAINT UQ_Designation_Name UNIQUE (CompanyID, DesignationName)
    );
END
GO
IF OBJECT_ID(N'mst.Bank', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Bank
    (
        BankID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bank PRIMARY KEY,
        CompanyID INT           NOT NULL CONSTRAINT FK_Bank_Company REFERENCES mst.Company (CompanyID),
        BankName NVARCHAR(100)  NOT NULL,
        IsActive BIT            NOT NULL CONSTRAINT DF_Bank_IsActive DEFAULT 1,
        CONSTRAINT UQ_Bank_Name UNIQUE (CompanyID, BankName)
    );
END
GO
IF OBJECT_ID(N'mst.Employee', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Employee
    (
        EmployeeID       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employee PRIMARY KEY,
        CompanyID        INT           NOT NULL CONSTRAINT FK_Employee_Company REFERENCES mst.Company (CompanyID),
        EmpCode          NVARCHAR(20)  NOT NULL,
        FullName         NVARCHAR(150) NOT NULL,
        DateOfBirth      DATE          NOT NULL,
        JoinDate         DATE          NOT NULL,
        LeaveDate        DATE          NULL,
        PayTypeID        TINYINT       NOT NULL CONSTRAINT CK_Employee_PayType CHECK (PayTypeID IN (1, 2)), /* 1 daily wage, 2 monthly salary */
        Mobile           NVARCHAR(10)  NULL,
        FactoryID        INT           NULL CONSTRAINT FK_Employee_Factory REFERENCES mst.Factory (FactoryID),
        DepartmentID     INT           NULL CONSTRAINT FK_Employee_Department REFERENCES mst.Department (DepartmentID),
        DesignationID    INT           NULL CONSTRAINT FK_Employee_Designation REFERENCES mst.Designation (DesignationID),
        BankID           INT           NULL CONSTRAINT FK_Employee_Bank REFERENCES mst.Bank (BankID),
        Ifsc             NVARCHAR(11)  NULL,
        AadhaarEnc       VARBINARY(256) NULL,
        PanEnc           VARBINARY(256) NULL,
        UanEnc           VARBINARY(256) NULL,
        EsiIpEnc         VARBINARY(256) NULL,
        BankAccountEnc   VARBINARY(256) NULL,
        AadhaarIndex     VARBINARY(32) NULL,
        PanIndex         VARBINARY(32) NULL,
        IsActive         BIT           NOT NULL CONSTRAINT DF_Employee_IsActive DEFAULT 1,
        CreatedUtc       DATETIME2(0)  NOT NULL CONSTRAINT DF_Employee_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_Employee_Code UNIQUE (CompanyID, EmpCode),
        CONSTRAINT UQ_Employee_CompanyEmployee UNIQUE (CompanyID, EmployeeID),
        CONSTRAINT CK_Employee_Age CHECK (DATEADD(YEAR, 14, DateOfBirth) <= JoinDate),
        CONSTRAINT CK_Employee_Leave CHECK (LeaveDate IS NULL OR LeaveDate >= JoinDate)
    );
    CREATE UNIQUE INDEX UX_Employee_Aadhaar ON mst.Employee (CompanyID, AadhaarIndex) WHERE AadhaarIndex IS NOT NULL;
    CREATE UNIQUE INDEX UX_Employee_Pan ON mst.Employee (CompanyID, PanIndex) WHERE PanIndex IS NOT NULL;
END
GO
IF OBJECT_ID(N'mst.Nominee', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Nominee
    (
        NomineeID    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Nominee PRIMARY KEY,
        CompanyID    INT           NOT NULL,
        EmployeeID   INT           NOT NULL,
        NomineeName  NVARCHAR(150) NOT NULL,
        Relation     NVARCHAR(50)  NOT NULL,
        DateOfBirth  DATE          NULL,
        SharePercent DECIMAL(5,2)  NOT NULL CONSTRAINT CK_Nominee_Share CHECK (SharePercent > 0 AND SharePercent <= 100),
        CONSTRAINT FK_Nominee_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID)
    );
END
GO
