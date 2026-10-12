/* 0101_mst_company_profile.sql
   Employer details that reports, returns and challans print. One row per company. Idempotent. Never edit after merge.
   Registration numbers are not secret, so they are plain text. The tenant policy is in 0104.
   EmployeeCodePrefix feeds the employee code (EMP-00042); the sequence lives in mst.EmployeeCodeSeq (0103). */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'mst.CompanyProfile', N'U') IS NULL
BEGIN
    CREATE TABLE mst.CompanyProfile
    (
        CompanyID            INT           NOT NULL CONSTRAINT PK_CompanyProfile PRIMARY KEY
                                           CONSTRAINT FK_CompanyProfile_Company REFERENCES mst.Company (CompanyID),
        LegalName            NVARCHAR(200) NOT NULL,
        AddressLine1         NVARCHAR(150) NULL,
        AddressLine2         NVARCHAR(150) NULL,
        City                 NVARCHAR(100) NULL,
        StateID              TINYINT       NULL CONSTRAINT FK_CompanyProfile_State REFERENCES mst.[State] (StateID),
        Pincode              CHAR(6)       NULL CONSTRAINT CK_CompanyProfile_Pincode CHECK (Pincode LIKE '[1-9][0-9][0-9][0-9][0-9][0-9]'),
        PAN                  CHAR(10)      NULL,
        [TAN]                  CHAR(10)      NULL,
        GSTIN                CHAR(15)      NULL,
        EpfEstablishmentCode NVARCHAR(30)  NULL,
        EsiEmployerCode      CHAR(17)      NULL,
        PtRegistrationNo     NVARCHAR(30)  NULL,
        EmployeeCodePrefix   NVARCHAR(10)  NOT NULL CONSTRAINT DF_CompanyProfile_Prefix DEFAULT N'EMP'
                                           CONSTRAINT CK_CompanyProfile_Prefix CHECK (LEN(LTRIM(RTRIM(EmployeeCodePrefix))) >= 1),
        UpdatedUtc           DATETIME2(0)  NOT NULL CONSTRAINT DF_CompanyProfile_UpdatedUtc DEFAULT SYSUTCDATETIME()
    );
END
GO
