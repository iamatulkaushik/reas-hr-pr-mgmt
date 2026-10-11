/* 0103_mst_employee_tables.sql
   Employee and the tables that hang off it. Per company. Idempotent. Never edit after merge. Tenant policy is in 0104.
   Codes: EmploymentTypeID 1 Permanent 2 Contract 3 Fixed term 4 Casual 5 Apprentice 6 Trainee. WorkerTypeID 1 Worker 2 Staff 3 Manager.
          PayTypeID 1 Daily wage 2 Monthly salary (the module's PayType). StatusID 1 Active 2 Notice 3 Left 4 Retired 5 Deceased 6 Absconded.
          MaritalStatusID 1 Single 2 Married 3 Widowed 4 Divorced. AddressTypeID 1 Current 2 Permanent.
          SchemeID 1 EPF 2 EPS 3 Gratuity 4 ESI 5 Insurance. TaxRegimeID 1 Old 2 New. PaymentModeID 1 Bank 2 Cash 3 Cheque.
          ReasonID 1 Resignation 2 Retirement 3 Death 4 Disablement 5 Termination 6 Absconding 7 Contract end 8 Retrenchment.
          FnFStatusID 1 Open 2 Calculated 3 Approved 4 Paid.
   Protected ids (ADR 0004): PAN, Aadhaar, UAN, ESI IP, bank account and document numbers are AES-256 encrypted IN THE APPLICATION.
   For each, <Id>Enc holds the ciphertext, <Id>Last4 the last four characters for masked display, and for the four employee ids
   <Id>Hash holds a keyed hash (blind index) so duplicates are caught without decrypting. IdKeyVersion says which key encrypted the row.
   The database never sees a clear id and holds no key.
   Shift assignment is not here: att.Shift is created later (0200) and shift history belongs to the attendance lane. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'mst.Employee', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Employee
    (
        EmployeeID           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employee PRIMARY KEY,
        CompanyID            INT           NOT NULL CONSTRAINT FK_Employee_Company REFERENCES mst.Company (CompanyID),
        EmpCode              NVARCHAR(20)  NOT NULL,
        FullName             NVARCHAR(100) NOT NULL,
        FatherOrSpouseName   NVARCHAR(120) NULL,
        Gender               CHAR(1)       NOT NULL CONSTRAINT CK_Employee_Gender CHECK (Gender IN ('M', 'F', 'O')),
        DateOfBirth          DATE          NOT NULL,
        JoinDate             DATE          NOT NULL,
        ExitDate             DATE          NULL,
        EmploymentTypeID     TINYINT       NOT NULL CONSTRAINT CK_Employee_EmploymentType CHECK (EmploymentTypeID BETWEEN 1 AND 6),
        WorkerTypeID         TINYINT       NOT NULL CONSTRAINT DF_Employee_WorkerType DEFAULT 1
                                           CONSTRAINT CK_Employee_WorkerType CHECK (WorkerTypeID BETWEEN 1 AND 3),
        PayTypeID            TINYINT       NOT NULL CONSTRAINT CK_Employee_PayType CHECK (PayTypeID IN (1, 2)),
        FactoryID            INT           NOT NULL,
        DepartmentID         INT           NULL,
        SectionID            INT           NULL,
        DesignationID        INT           NULL,
        GradeID              INT           NULL,
        ReportingToEmployeeID INT          NULL,
        BiometricID          INT           NULL,
        MaritalStatusID      TINYINT       NULL CONSTRAINT CK_Employee_Marital CHECK (MaritalStatusID BETWEEN 1 AND 4),
        Nationality          NVARCHAR(30)  NOT NULL CONSTRAINT DF_Employee_Nationality DEFAULT N'Indian',
        Mobile               CHAR(10)      NULL CONSTRAINT CK_Employee_Mobile CHECK (Mobile LIKE '[6-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
        Email                NVARCHAR(200) NULL,
        StatusID             TINYINT       NOT NULL CONSTRAINT DF_Employee_Status DEFAULT 1
                                           CONSTRAINT CK_Employee_Status CHECK (StatusID BETWEEN 1 AND 6),
        IsActive             BIT           NOT NULL CONSTRAINT DF_Employee_IsActive DEFAULT 1,
        IsAdolescent         AS (CASE WHEN DATEADD(YEAR, 18, DateOfBirth) > JoinDate THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END),
        CreatedUtc           DATETIME2(0)  NOT NULL CONSTRAINT DF_Employee_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CreatedByUserID      INT           NULL,
        UpdatedUtc           DATETIME2(0)  NULL,
        UpdatedByUserID      INT           NULL,
        RowVer               ROWVERSION    NOT NULL,
        CONSTRAINT CK_Employee_MinAge CHECK (JoinDate >= DATEADD(YEAR, 14, DateOfBirth)),
        CONSTRAINT CK_Employee_Exit CHECK (ExitDate IS NULL OR ExitDate >= JoinDate),
        CONSTRAINT UQ_Employee_Code UNIQUE (CompanyID, EmpCode),
        CONSTRAINT UQ_Employee_Company_Id UNIQUE (CompanyID, EmployeeID),
        CONSTRAINT FK_Employee_Factory FOREIGN KEY (CompanyID, FactoryID) REFERENCES mst.Factory (CompanyID, FactoryID),
        CONSTRAINT FK_Employee_Department FOREIGN KEY (CompanyID, FactoryID, DepartmentID) REFERENCES mst.Department (CompanyID, FactoryID, DepartmentID),
        CONSTRAINT FK_Employee_Section FOREIGN KEY (CompanyID, SectionID) REFERENCES mst.Section (CompanyID, SectionID),
        CONSTRAINT FK_Employee_Designation FOREIGN KEY (CompanyID, DesignationID) REFERENCES mst.Designation (CompanyID, DesignationID),
        CONSTRAINT FK_Employee_Grade FOREIGN KEY (CompanyID, GradeID) REFERENCES mst.Grade (CompanyID, GradeID),
        CONSTRAINT FK_Employee_ReportingTo FOREIGN KEY (CompanyID, ReportingToEmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID)
    );
    CREATE INDEX IX_Employee_Factory ON mst.Employee (CompanyID, FactoryID) INCLUDE (EmpCode, FullName, StatusID, IsActive);
    CREATE INDEX IX_Employee_Name ON mst.Employee (CompanyID, FullName);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Employee_Biometric')
    CREATE UNIQUE INDEX UX_Employee_Biometric ON mst.Employee (CompanyID, BiometricID) WHERE BiometricID IS NOT NULL;
GO
IF OBJECT_ID(N'mst.EmployeeCodeSeq', N'U') IS NULL
BEGIN
    CREATE TABLE mst.EmployeeCodeSeq
    (
        CompanyID INT NOT NULL CONSTRAINT PK_EmployeeCodeSeq PRIMARY KEY CONSTRAINT FK_EmployeeCodeSeq_Company REFERENCES mst.Company (CompanyID),
        LastSeq   INT NOT NULL CONSTRAINT DF_EmployeeCodeSeq_LastSeq DEFAULT 0
    );
END
GO
IF OBJECT_ID(N'mst.EmployeeStatutory', N'U') IS NULL
BEGIN
    CREATE TABLE mst.EmployeeStatutory
    (
        CompanyID            INT           NOT NULL,
        EmployeeID           INT           NOT NULL,
        IsEpfMember          BIT           NOT NULL CONSTRAINT DF_EmpStat_Epf DEFAULT 0,
        PfNumber             NVARCHAR(30)  NULL,
        EpfJoinDate          DATE          NULL,
        IsPensionMember      BIT           NOT NULL CONSTRAINT DF_EmpStat_Pension DEFAULT 1,
        HigherWageOption     BIT           NOT NULL CONSTRAINT DF_EmpStat_Higher DEFAULT 0,
        IsEsiMember          BIT           NOT NULL CONSTRAINT DF_EmpStat_Esi DEFAULT 0,
        EsiDispensary        NVARCHAR(100) NULL,
        PtApplicable         BIT           NOT NULL CONSTRAINT DF_EmpStat_Pt DEFAULT 1,
        LwfApplicable        BIT           NOT NULL CONSTRAINT DF_EmpStat_Lwf DEFAULT 1,
        TaxRegimeID          TINYINT       NOT NULL CONSTRAINT DF_EmpStat_Regime DEFAULT 2 CONSTRAINT CK_EmpStat_Regime CHECK (TaxRegimeID IN (1, 2)),
        IsInternationalWorker BIT          NOT NULL CONSTRAINT DF_EmpStat_Intl DEFAULT 0,
        UanEnc               VARBINARY(256) NULL, UanHash     BINARY(32) NULL, UanLast4     CHAR(4) NULL,
        EsiIpEnc             VARBINARY(256) NULL, EsiIpHash   BINARY(32) NULL, EsiIpLast4   CHAR(4) NULL,
        PanEnc               VARBINARY(256) NULL, PanHash     BINARY(32) NULL, PanLast4     CHAR(4) NULL,
        AadhaarEnc           VARBINARY(256) NULL, AadhaarHash BINARY(32) NULL, AadhaarLast4 CHAR(4) NULL,
        IdKeyVersion         TINYINT       NOT NULL CONSTRAINT DF_EmpStat_KeyVersion DEFAULT 1,
        UpdatedUtc           DATETIME2(0)  NOT NULL CONSTRAINT DF_EmpStat_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        UpdatedByUserID      INT           NULL,
        CONSTRAINT PK_EmployeeStatutory PRIMARY KEY (CompanyID, EmployeeID),
        CONSTRAINT FK_EmployeeStatutory_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID),
        CONSTRAINT CK_EmpStat_Uan CHECK ((UanEnc IS NULL AND UanHash IS NULL AND UanLast4 IS NULL) OR (UanEnc IS NOT NULL AND UanHash IS NOT NULL AND UanLast4 IS NOT NULL)),
        CONSTRAINT CK_EmpStat_EsiIp CHECK ((EsiIpEnc IS NULL AND EsiIpHash IS NULL AND EsiIpLast4 IS NULL) OR (EsiIpEnc IS NOT NULL AND EsiIpHash IS NOT NULL AND EsiIpLast4 IS NOT NULL)),
        CONSTRAINT CK_EmpStat_Pan CHECK ((PanEnc IS NULL AND PanHash IS NULL AND PanLast4 IS NULL) OR (PanEnc IS NOT NULL AND PanHash IS NOT NULL AND PanLast4 IS NOT NULL)),
        CONSTRAINT CK_EmpStat_Aadhaar CHECK ((AadhaarEnc IS NULL AND AadhaarHash IS NULL AND AadhaarLast4 IS NULL) OR (AadhaarEnc IS NOT NULL AND AadhaarHash IS NOT NULL AND AadhaarLast4 IS NOT NULL))
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_EmpStat_Uan')
    CREATE UNIQUE INDEX UX_EmpStat_Uan ON mst.EmployeeStatutory (CompanyID, UanHash) WHERE UanHash IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_EmpStat_EsiIp')
    CREATE UNIQUE INDEX UX_EmpStat_EsiIp ON mst.EmployeeStatutory (CompanyID, EsiIpHash) WHERE EsiIpHash IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_EmpStat_Pan')
    CREATE UNIQUE INDEX UX_EmpStat_Pan ON mst.EmployeeStatutory (CompanyID, PanHash) WHERE PanHash IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_EmpStat_Aadhaar')
    CREATE UNIQUE INDEX UX_EmpStat_Aadhaar ON mst.EmployeeStatutory (CompanyID, AadhaarHash) WHERE AadhaarHash IS NOT NULL;
GO
IF OBJECT_ID(N'mst.EmployeeBank', N'U') IS NULL
BEGIN
    CREATE TABLE mst.EmployeeBank
    (
        CompanyID       INT            NOT NULL,
        EmployeeID      INT            NOT NULL,
        PaymentModeID   TINYINT        NOT NULL CONSTRAINT DF_EmpBank_Mode DEFAULT 1 CONSTRAINT CK_EmpBank_Mode CHECK (PaymentModeID BETWEEN 1 AND 3),
        BankID          INT            NULL CONSTRAINT FK_EmployeeBank_Bank REFERENCES mst.Bank (BankID),
        Ifsc            CHAR(11)       NULL,
        AccountEnc      VARBINARY(256) NULL,
        AccountLast4    CHAR(4)        NULL,
        IdKeyVersion    TINYINT        NOT NULL CONSTRAINT DF_EmpBank_KeyVersion DEFAULT 1,
        UpdatedUtc      DATETIME2(0)   NOT NULL CONSTRAINT DF_EmpBank_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        UpdatedByUserID INT            NULL,
        CONSTRAINT PK_EmployeeBank PRIMARY KEY (CompanyID, EmployeeID),
        CONSTRAINT FK_EmployeeBank_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID),
        CONSTRAINT CK_EmpBank_Pair CHECK ((AccountEnc IS NULL AND AccountLast4 IS NULL) OR (AccountEnc IS NOT NULL AND AccountLast4 IS NOT NULL)),
        CONSTRAINT CK_EmpBank_BankMode CHECK (PaymentModeID <> 1 OR (AccountEnc IS NOT NULL AND Ifsc IS NOT NULL))
    );
END
GO
IF OBJECT_ID(N'mst.EmployeeAddress', N'U') IS NULL
BEGIN
    CREATE TABLE mst.EmployeeAddress
    (
        AddressID     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeAddress PRIMARY KEY,
        CompanyID     INT           NOT NULL,
        EmployeeID    INT           NOT NULL,
        AddressTypeID TINYINT       NOT NULL CONSTRAINT CK_EmpAddr_Type CHECK (AddressTypeID IN (1, 2)),
        Line1         NVARCHAR(200) NULL,
        Line2         NVARCHAR(200) NULL,
        City          NVARCHAR(100) NULL,
        StateID       TINYINT       NULL CONSTRAINT FK_EmployeeAddress_State REFERENCES mst.[State] (StateID),
        Pincode       CHAR(6)       NULL CONSTRAINT CK_EmpAddr_Pincode CHECK (Pincode LIKE '[1-9][0-9][0-9][0-9][0-9][0-9]'),
        CONSTRAINT FK_EmployeeAddress_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID),
        CONSTRAINT UQ_EmployeeAddress_Type UNIQUE (CompanyID, EmployeeID, AddressTypeID)
    );
END
GO
IF OBJECT_ID(N'mst.EmployeeFamily', N'U') IS NULL
BEGIN
    CREATE TABLE mst.EmployeeFamily
    (
        FamilyID    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeFamily PRIMARY KEY,
        CompanyID   INT           NOT NULL,
        EmployeeID  INT           NOT NULL,
        MemberName  NVARCHAR(120) NOT NULL,
        Relation    NVARCHAR(30)  NOT NULL,
        DateOfBirth DATE          NULL,
        IsDependent BIT           NOT NULL CONSTRAINT DF_EmpFamily_Dependent DEFAULT 0,
        IsResiding  BIT           NOT NULL CONSTRAINT DF_EmpFamily_Residing DEFAULT 1,
        CONSTRAINT FK_EmployeeFamily_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID),
        CONSTRAINT UQ_EmployeeFamily_Company_Id UNIQUE (CompanyID, FamilyID)
    );
END
GO
IF OBJECT_ID(N'mst.EmployeeNominee', N'U') IS NULL
BEGIN
    CREATE TABLE mst.EmployeeNominee
    (
        NomineeID    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeNominee PRIMARY KEY,
        CompanyID    INT          NOT NULL,
        EmployeeID   INT          NOT NULL,
        SchemeID     TINYINT      NOT NULL CONSTRAINT CK_EmpNominee_Scheme CHECK (SchemeID BETWEEN 1 AND 5),
        FamilyID     INT          NOT NULL,
        SharePercent DECIMAL(5,2) NOT NULL CONSTRAINT CK_EmpNominee_Share CHECK (SharePercent > 0 AND SharePercent <= 100),
        CONSTRAINT FK_EmployeeNominee_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID),
        CONSTRAINT FK_EmployeeNominee_Family FOREIGN KEY (CompanyID, FamilyID) REFERENCES mst.EmployeeFamily (CompanyID, FamilyID),
        CONSTRAINT UQ_EmployeeNominee UNIQUE (CompanyID, EmployeeID, SchemeID, FamilyID)
    );
END
GO
IF OBJECT_ID(N'mst.EmployeeDocument', N'U') IS NULL
BEGIN
    CREATE TABLE mst.EmployeeDocument
    (
        DocumentID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeDocument PRIMARY KEY,
        CompanyID       INT            NOT NULL,
        EmployeeID      INT            NOT NULL,
        DocumentTypeID  INT            NOT NULL CONSTRAINT FK_EmployeeDocument_Type REFERENCES mst.DocumentType (DocumentTypeID),
        DocNoEnc        VARBINARY(256) NULL,
        DocNoLast4      CHAR(4)        NULL,
        IdKeyVersion    TINYINT        NOT NULL CONSTRAINT DF_EmpDoc_KeyVersion DEFAULT 1,
        ValidTo         DATE           NULL,
        FilePath        NVARCHAR(400)  NULL,
        VerifiedByUserID INT           NULL,
        VerifiedOn      DATE           NULL,
        CONSTRAINT CK_EmpDoc_Pair CHECK ((DocNoEnc IS NULL AND DocNoLast4 IS NULL) OR (DocNoEnc IS NOT NULL AND DocNoLast4 IS NOT NULL)),
        CONSTRAINT FK_EmployeeDocument_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID),
        CONSTRAINT UQ_EmployeeDocument_Type UNIQUE (CompanyID, EmployeeID, DocumentTypeID)
    );
END
GO
IF OBJECT_ID(N'mst.EmployeeSeparation', N'U') IS NULL
BEGIN
    CREATE TABLE mst.EmployeeSeparation
    (
        SeparationID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmployeeSeparation PRIMARY KEY,
        CompanyID         INT          NOT NULL,
        EmployeeID        INT          NOT NULL,
        ReasonID          TINYINT      NOT NULL CONSTRAINT CK_EmpSep_Reason CHECK (ReasonID BETWEEN 1 AND 8),
        NoticeDate        DATE         NULL,
        LastWorkingDay    DATE         NOT NULL,
        NoticePeriodDays  SMALLINT     NOT NULL CONSTRAINT DF_EmpSep_Period DEFAULT 30 CONSTRAINT CK_EmpSep_Period CHECK (NoticePeriodDays >= 0),
        NoticeServedDays  SMALLINT     NOT NULL CONSTRAINT DF_EmpSep_Served DEFAULT 0 CONSTRAINT CK_EmpSep_Served CHECK (NoticeServedDays >= 0),
        FnFStatusID       TINYINT      NOT NULL CONSTRAINT DF_EmpSep_Fnf DEFAULT 1 CONSTRAINT CK_EmpSep_Fnf CHECK (FnFStatusID BETWEEN 1 AND 4),
        ExitInterviewDone BIT          NOT NULL CONSTRAINT DF_EmpSep_Interview DEFAULT 0,
        CreatedUtc        DATETIME2(0) NOT NULL CONSTRAINT DF_EmpSep_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CreatedByUserID   INT          NULL,
        CONSTRAINT FK_EmployeeSeparation_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID)
    );
END
GO
