/* 0019_licence_tables.sql
   ADR 0018, step 2. Idempotent. Never edit after merge: add a new number.
   cfg.Installation: exactly one row, the installation id a licence is bound to. Created here, so every database has one from its first migration.
   cfg.Licence: the signed licence files, one current row per Associate, older rows kept. The database stores and serves them; it cannot check an
   ECDSA signature, so the app verifies the signature every time it reads one. A forged row is therefore ignored by the app. Importing needs the Developer.
   sec.[User].MaxCompaniesOverride: the company cap a licence sets for an Associate. NULL means the plan quota (cfg.PlanQuota) applies, as before.
   usp_Associate_CreateCompany now uses the override when there is one. The ceiling stays 500.
   Errors: 50036 only the Developer imports or lists licences, 50037 no such Associate, 50038 licence already imported.
   Dates are UTC days: the licence end date is the last day of use. Expiry, module and user limits are enforced by the app; the company cap here. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'cfg.Installation', N'U') IS NULL
BEGIN
    CREATE TABLE cfg.Installation
    (
        Singleton      BIT              NOT NULL CONSTRAINT PK_Installation PRIMARY KEY CONSTRAINT CK_Installation_Singleton CHECK (Singleton = 1),
        InstallationID UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Installation_ID DEFAULT NEWID(),
        CreatedUtc     DATETIME2(0)     NOT NULL CONSTRAINT DF_Installation_CreatedUtc DEFAULT SYSUTCDATETIME()
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM cfg.Installation) INSERT cfg.Installation (Singleton) VALUES (1);
GO
IF OBJECT_ID(N'cfg.Licence', N'U') IS NULL
BEGIN
    CREATE TABLE cfg.Licence
    (
        LicenceRowID     INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Licence PRIMARY KEY,
        LicenceGuid      UNIQUEIDENTIFIER   NOT NULL CONSTRAINT UQ_Licence_Guid UNIQUE,
        AssociateUserID  INT                NOT NULL CONSTRAINT FK_Licence_Associate REFERENCES sec.[User] (UserID),
        LicenceText      NVARCHAR(MAX)      NOT NULL,
        ValidFrom        DATE               NOT NULL,
        ValidTo          DATE               NOT NULL,
        PlanCapTierID    TINYINT            NOT NULL CONSTRAINT CK_Licence_Plan CHECK (PlanCapTierID BETWEEN 1 AND 4),
        MaxCompanies     SMALLINT           NOT NULL CONSTRAINT CK_Licence_MaxCompanies CHECK (MaxCompanies BETWEEN 1 AND 500),
        MaxActiveUsers   INT                NOT NULL CONSTRAINT CK_Licence_MaxActiveUsers CHECK (MaxActiveUsers >= 1),
        IsCurrent        BIT                NOT NULL CONSTRAINT DF_Licence_IsCurrent DEFAULT 1,
        ImportedByUserID INT                NOT NULL CONSTRAINT FK_Licence_ImportedBy REFERENCES sec.[User] (UserID),
        ImportedUtc      DATETIME2(0)       NOT NULL CONSTRAINT DF_Licence_ImportedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Licence_Dates CHECK (ValidTo >= ValidFrom)
    );
    CREATE UNIQUE INDEX UX_Licence_OneCurrent ON cfg.Licence (AssociateUserID) WHERE IsCurrent = 1;
END
GO
IF COL_LENGTH(N'sec.[User]', N'MaxCompaniesOverride') IS NULL
    ALTER TABLE sec.[User] ADD MaxCompaniesOverride SMALLINT NULL CONSTRAINT CK_User_MaxCompaniesOverride CHECK (MaxCompaniesOverride BETWEEN 1 AND 500);
GO
CREATE OR ALTER PROCEDURE cfg.usp_Installation_Get
AS
BEGIN
    SET NOCOUNT ON;
    SELECT InstallationID, CreatedUtc FROM cfg.Installation;
END
GO
CREATE OR ALTER PROCEDURE cfg.usp_Server_Today
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CAST(SYSUTCDATETIME() AS DATE) AS Today;
END
GO
CREATE OR ALTER PROCEDURE cfg.usp_Licence_Import
    @LicenceGuid UNIQUEIDENTIFIER, @AssociateUserName NVARCHAR(100), @LicenceText NVARCHAR(MAX),
    @ValidFrom DATE, @ValidTo DATE, @PlanCapTierID TINYINT, @MaxCompanies SMALLINT, @MaxActiveUsers INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @ByUserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    IF @ByUserID IS NULL OR NOT EXISTS (SELECT 1 FROM sec.[User] WHERE UserID = @ByUserID AND RoleID = 1 AND IsActive = 1)
        THROW 50036, N'Only the Developer can import a licence.', 1;
    DECLARE @AssociateID INT = (SELECT UserID FROM sec.[User] WHERE UserName = @AssociateUserName AND RoleID = 3 AND IsActive = 1);
    IF @AssociateID IS NULL
        THROW 50037, N'No active Associate has that user name.', 1;
    IF EXISTS (SELECT 1 FROM cfg.Licence WHERE LicenceGuid = @LicenceGuid)
        THROW 50038, N'This licence has already been imported.', 1;
    BEGIN TRAN;
        UPDATE cfg.Licence SET IsCurrent = 0 WHERE AssociateUserID = @AssociateID AND IsCurrent = 1;
        INSERT cfg.Licence (LicenceGuid, AssociateUserID, LicenceText, ValidFrom, ValidTo, PlanCapTierID, MaxCompanies, MaxActiveUsers, ImportedByUserID)
        VALUES (@LicenceGuid, @AssociateID, @LicenceText, @ValidFrom, @ValidTo, @PlanCapTierID, @MaxCompanies, @MaxActiveUsers, @ByUserID);
        UPDATE sec.[User] SET MaxCompaniesOverride = @MaxCompanies WHERE UserID = @AssociateID;
    COMMIT;
END
GO
CREATE OR ALTER PROCEDURE cfg.usp_Licence_ListCurrent
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ByUserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    IF @ByUserID IS NULL OR NOT EXISTS (SELECT 1 FROM sec.[User] WHERE UserID = @ByUserID AND RoleID = 1 AND IsActive = 1)
        THROW 50036, N'Only the Developer can list licences.', 1;
    SELECT l.LicenceGuid, u.UserName AS AssociateUserName, l.LicenceText, l.ValidFrom, l.ValidTo,
           l.PlanCapTierID, l.MaxCompanies, l.MaxActiveUsers, l.ImportedUtc
    FROM cfg.Licence l
    JOIN sec.[User] u ON u.UserID = l.AssociateUserID
    WHERE l.IsCurrent = 1
    ORDER BY u.UserName;
END
GO
/* The licences that govern the signed-in session. An Associate gets their own. An Owner or Operator gets the current licences of the
   Associates who work on their company (the app applies the most generous one). The Developer gets none: the Developer is never limited. */
CREATE OR ALTER PROCEDURE cfg.usp_Licence_ForSession
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @UserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    DECLARE @RoleID TINYINT = (SELECT RoleID FROM sec.[User] WHERE UserID = @UserID);
    SELECT l.LicenceGuid, u.UserName AS AssociateUserName, l.LicenceText, l.ValidFrom, l.ValidTo,
           l.PlanCapTierID, l.MaxCompanies, l.MaxActiveUsers
    FROM cfg.Licence l
    JOIN sec.[User] u ON u.UserID = l.AssociateUserID
    WHERE l.IsCurrent = 1
      AND ((@RoleID = 3 AND l.AssociateUserID = @UserID)
        OR (@RoleID IN (2, 4) AND EXISTS (SELECT 1 FROM sec.UserCompany uc WHERE uc.UserID = l.AssociateUserID AND uc.CompanyID = @CompanyID)));
END
GO
CREATE OR ALTER PROCEDURE sec.usp_Associate_CreateCompany @CompanyName NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @UserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    IF @UserID IS NULL OR NOT EXISTS (SELECT 1 FROM sec.[User] WHERE UserID = @UserID AND RoleID = 3 AND IsActive = 1)
        THROW 50030, N'Only an Associate can create a company here.', 1;
    DECLARE @Name NVARCHAR(200) = LTRIM(RTRIM(@CompanyName));
    IF @Name IS NULL OR LEN(@Name) = 0
        THROW 50031, N'A company needs a name.', 1;
    DECLARE @CompanyID INT;
    BEGIN TRAN;
        DECLARE @Plan TINYINT;
        DECLARE @Override SMALLINT;
        SELECT @Plan = COALESCE(AssociatePlanTierID, 1), @Override = MaxCompaniesOverride
        FROM sec.[User] WITH (UPDLOCK, HOLDLOCK) WHERE UserID = @UserID;
        DECLARE @Max INT = COALESCE(@Override, (SELECT MaxCompanies FROM cfg.PlanQuota WHERE PlanTierID = @Plan));
        DECLARE @Held INT = (SELECT COUNT(*) FROM sec.UserCompany uc JOIN mst.Company c ON c.CompanyID = uc.CompanyID
                             WHERE uc.UserID = @UserID AND c.IsActive = 1);
        IF @Max IS NULL OR @Held >= @Max
        BEGIN
            DECLARE @Msg NVARCHAR(200) = CONCAT(N'Your licence or plan allows ', COALESCE(@Max, 0), N' companies and you work on ', @Held,
                                                N'. Ask the Developer to raise your plan or issue a new licence.');
            THROW 50034, @Msg, 1;
        END
        INSERT mst.Company (CompanyName, PlanTierID) VALUES (@Name, 1);
        SET @CompanyID = CAST(SCOPE_IDENTITY() AS INT);
        INSERT sec.UserCompany (UserID, CompanyID) VALUES (@UserID, @CompanyID);
    COMMIT;
    SELECT @CompanyID;
END
GO
