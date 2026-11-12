/* 0600_ast_asset_tables.sql
   Phase 9 step 0600: assets (Platinum plan, Owner only, PlanFeature.AssetsAndExpenses). Brave lane 0600-0699. Idempotent. Never edit after merge. Schema ast is 0033, tenant policy is 0601, procedures are 0602.
   ast.AssetType: a kind of item (laptop, mobile, uniform ...) with its useful life in months; the life is what depreciation uses at step 0601. Rows are never deleted: a retired type is set inactive.
   ast.Asset: one tagged item. StatusID: 1 In stock, 2 Issued, 3 Lost, 4 Damaged, 5 Written off. A returned item goes back to In stock (or to Damaged), so there is no separate Returned status.
   ast.AssetIssue: one row per hand-over. ReturnDate NULL means the employee still holds the item. At most one open row per asset. Rows are never deleted.
   Errors for assets: 53500 to 53509 (see 0602). Expenses use 53510 and up. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'ast.AssetType', N'U') IS NULL
BEGIN
    CREATE TABLE ast.AssetType
    (
        AssetTypeID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetType PRIMARY KEY,
        CompanyID        INT           NOT NULL CONSTRAINT FK_AssetType_Company REFERENCES mst.Company (CompanyID),
        TypeName         NVARCHAR(60)  NOT NULL,
        UsefulLifeMonths INT           NOT NULL CONSTRAINT CK_AssetType_Life CHECK (UsefulLifeMonths BETWEEN 1 AND 600),
        IsActive         BIT           NOT NULL CONSTRAINT DF_AssetType_IsActive DEFAULT 1,
        CreatedUtc       DATETIME2(0)  NOT NULL CONSTRAINT DF_AssetType_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CreatedByUserID  INT           NULL,
        CONSTRAINT UQ_AssetType_Name UNIQUE (CompanyID, TypeName),
        CONSTRAINT UQ_AssetType_Company UNIQUE (CompanyID, AssetTypeID)
    );
END
GO
IF OBJECT_ID(N'ast.Asset', N'U') IS NULL
BEGIN
    CREATE TABLE ast.Asset
    (
        AssetID         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Asset PRIMARY KEY,
        CompanyID       INT            NOT NULL CONSTRAINT FK_Asset_Company REFERENCES mst.Company (CompanyID),
        AssetTypeID     INT            NOT NULL,
        TagNo           NVARCHAR(30)   NOT NULL,
        Description     NVARCHAR(200)  NOT NULL,
        SerialNo        NVARCHAR(60)   NULL,
        PurchaseDate    DATE           NOT NULL,
        PurchaseValue   DECIMAL(12,2)  NOT NULL CONSTRAINT CK_Asset_Value CHECK (PurchaseValue > 0),
        StatusID        TINYINT        NOT NULL CONSTRAINT DF_Asset_Status DEFAULT 1 CONSTRAINT CK_Asset_Status CHECK (StatusID BETWEEN 1 AND 5),
        CreatedUtc      DATETIME2(0)   NOT NULL CONSTRAINT DF_Asset_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CreatedByUserID INT            NULL,
        CONSTRAINT FK_Asset_Type FOREIGN KEY (CompanyID, AssetTypeID) REFERENCES ast.AssetType (CompanyID, AssetTypeID),
        CONSTRAINT UQ_Asset_Tag UNIQUE (CompanyID, TagNo),
        CONSTRAINT UQ_Asset_Company UNIQUE (CompanyID, AssetID)
    );
END
GO
IF OBJECT_ID(N'ast.AssetIssue', N'U') IS NULL
BEGIN
    CREATE TABLE ast.AssetIssue
    (
        IssueID            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetIssue PRIMARY KEY,
        CompanyID          INT           NOT NULL CONSTRAINT FK_AssetIssue_Company REFERENCES mst.Company (CompanyID),
        AssetID            INT           NOT NULL,
        EmployeeID         INT           NOT NULL,
        IssueDate          DATE          NOT NULL,
        IssueCondition     NVARCHAR(100) NULL,
        ExpectedReturnDate DATE          NULL,
        ReturnDate         DATE          NULL,
        ReturnCondition    NVARCHAR(100) NULL,
        IssuedByUserID     INT           NULL,
        ReturnedByUserID   INT           NULL,
        CreatedUtc         DATETIME2(0)  NOT NULL CONSTRAINT DF_AssetIssue_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_AssetIssue_Asset FOREIGN KEY (CompanyID, AssetID) REFERENCES ast.Asset (CompanyID, AssetID),
        CONSTRAINT FK_AssetIssue_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID),
        CONSTRAINT CK_AssetIssue_Dates CHECK ((ExpectedReturnDate IS NULL OR ExpectedReturnDate >= IssueDate) AND (ReturnDate IS NULL OR ReturnDate >= IssueDate))
    );
    CREATE UNIQUE INDEX UX_AssetIssue_Open ON ast.AssetIssue (CompanyID, AssetID) WHERE ReturnDate IS NULL;
    CREATE INDEX IX_AssetIssue_Employee ON ast.AssetIssue (CompanyID, EmployeeID, ReturnDate);
END
GO
