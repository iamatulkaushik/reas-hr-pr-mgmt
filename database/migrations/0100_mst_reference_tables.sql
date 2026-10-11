/* 0100_mst_reference_tables.sql
   Shared reference data: states, districts, banks, document types. No CompanyID: these are the same for every tenant. Idempotent. Never edit after merge.
   Written by Sigma in Brave's lane 0100-0199 (ADR 0010) because Phase 3 was blocked on it.
   mst.[State].StateID is the GST state code (01 Jammu and Kashmir ... 38 Ladakh; 25 and 28 are retired). Professional tax and
   labour-law limits are per state, so every later table that needs a state points here. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'mst.State', N'U') IS NULL
BEGIN
    CREATE TABLE mst.[State]
    (
        StateID   TINYINT       NOT NULL CONSTRAINT PK_State PRIMARY KEY,
        StateName NVARCHAR(100) NOT NULL CONSTRAINT UQ_State_Name UNIQUE,
        IsActive  BIT           NOT NULL CONSTRAINT DF_State_IsActive DEFAULT 1
    );
END
GO
IF OBJECT_ID(N'mst.District', N'U') IS NULL
BEGIN
    CREATE TABLE mst.District
    (
        DistrictID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_District PRIMARY KEY,
        StateID      TINYINT       NOT NULL CONSTRAINT FK_District_State REFERENCES mst.[State] (StateID),
        DistrictName NVARCHAR(100) NOT NULL,
        IsActive     BIT           NOT NULL CONSTRAINT DF_District_IsActive DEFAULT 1,
        CONSTRAINT UQ_District_Name UNIQUE (StateID, DistrictName)
    );
END
GO
IF OBJECT_ID(N'mst.Bank', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Bank
    (
        BankID     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bank PRIMARY KEY,
        BankName   NVARCHAR(150) NOT NULL CONSTRAINT UQ_Bank_Name UNIQUE,
        IfscPrefix CHAR(4)       NOT NULL CONSTRAINT UQ_Bank_IfscPrefix UNIQUE,
        IsActive   BIT           NOT NULL CONSTRAINT DF_Bank_IsActive DEFAULT 1
    );
END
GO
IF OBJECT_ID(N'mst.DocumentType', N'U') IS NULL
BEGIN
    CREATE TABLE mst.DocumentType
    (
        DocumentTypeID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentType PRIMARY KEY,
        DocumentName   NVARCHAR(80) NOT NULL CONSTRAINT UQ_DocumentType_Name UNIQUE,
        IsMandatory    BIT          NOT NULL CONSTRAINT DF_DocumentType_IsMandatory DEFAULT 0,
        HasExpiry      BIT          NOT NULL CONSTRAINT DF_DocumentType_HasExpiry DEFAULT 0
    );
END
GO
INSERT mst.[State] (StateID, StateName)
SELECT v.StateID, v.StateName
FROM (VALUES
    (1, N'Jammu and Kashmir'), (2, N'Himachal Pradesh'), (3, N'Punjab'), (4, N'Chandigarh'), (5, N'Uttarakhand'),
    (6, N'Haryana'), (7, N'Delhi'), (8, N'Rajasthan'), (9, N'Uttar Pradesh'), (10, N'Bihar'), (11, N'Sikkim'),
    (12, N'Arunachal Pradesh'), (13, N'Nagaland'), (14, N'Manipur'), (15, N'Mizoram'), (16, N'Tripura'),
    (17, N'Meghalaya'), (18, N'Assam'), (19, N'West Bengal'), (20, N'Jharkhand'), (21, N'Odisha'),
    (22, N'Chhattisgarh'), (23, N'Madhya Pradesh'), (24, N'Gujarat'), (26, N'Dadra and Nagar Haveli and Daman and Diu'),
    (27, N'Maharashtra'), (29, N'Karnataka'), (30, N'Goa'), (31, N'Lakshadweep'), (32, N'Kerala'),
    (33, N'Tamil Nadu'), (34, N'Puducherry'), (35, N'Andaman and Nicobar Islands'), (36, N'Telangana'),
    (37, N'Andhra Pradesh'), (38, N'Ladakh')
) v (StateID, StateName)
WHERE NOT EXISTS (SELECT 1 FROM mst.[State] s WHERE s.StateID = v.StateID);
GO
INSERT mst.Bank (BankName, IfscPrefix)
SELECT v.BankName, v.IfscPrefix
FROM (VALUES
    (N'State Bank of India', 'SBIN'), (N'HDFC Bank', 'HDFC'), (N'ICICI Bank', 'ICIC'), (N'Axis Bank', 'UTIB'),
    (N'Punjab National Bank', 'PUNB'), (N'Bank of Baroda', 'BARB'), (N'Canara Bank', 'CNRB'),
    (N'Union Bank of India', 'UBIN'), (N'Bank of India', 'BKID'), (N'Indian Overseas Bank', 'IOBA'),
    (N'Indian Bank', 'IDIB'), (N'Bank of Maharashtra', 'MAHB'), (N'UCO Bank', 'UCBA'),
    (N'Central Bank of India', 'CBIN'), (N'Punjab and Sind Bank', 'PSIB'), (N'Kotak Mahindra Bank', 'KKBK'),
    (N'Yes Bank', 'YESB'), (N'IndusInd Bank', 'INDB'), (N'Federal Bank', 'FDRL'), (N'IDFC FIRST Bank', 'IDFB'),
    (N'South Indian Bank', 'SIBL'), (N'Karnataka Bank', 'KARB'), (N'RBL Bank', 'RATN'), (N'AU Small Finance Bank', 'AUBL')
) v (BankName, IfscPrefix)
WHERE NOT EXISTS (SELECT 1 FROM mst.Bank b WHERE b.IfscPrefix = v.IfscPrefix);
GO
INSERT mst.DocumentType (DocumentName, IsMandatory, HasExpiry)
SELECT v.DocumentName, v.IsMandatory, v.HasExpiry
FROM (VALUES
    (N'Aadhaar', 1, 0), (N'PAN', 0, 0), (N'Bank passbook or cancelled cheque', 1, 0), (N'Age proof', 1, 0),
    (N'Educational certificate', 0, 0), (N'Medical fitness certificate', 0, 1), (N'Police verification', 0, 1),
    (N'Appointment letter', 1, 0), (N'EPF declaration (Form 11)', 0, 0), (N'Photo', 1, 0)
) v (DocumentName, IsMandatory, HasExpiry)
WHERE NOT EXISTS (SELECT 1 FROM mst.DocumentType d WHERE d.DocumentName = v.DocumentName);
GO
