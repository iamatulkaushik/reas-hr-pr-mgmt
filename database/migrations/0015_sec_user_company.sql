/* 0015_sec_user_company.sql
   ADR 0013. An Associate works on several companies. sec.UserCompany links a user to the companies the user may open.
   - An Associate may have no company yet (the first one is created in the app), so the Associate no longer needs sec.[User].CompanyID.
     Developer: no company. Owner and Operator: one company. Associate: any.
   - Existing Associates are linked to the company they already have.
   - Setup's sec.usp_User_Create now also links a new Associate to the company given.
   - The running app may create a company and its Owner login, but only through the two procedures below. Each one checks in the
     database that SESSION_CONTEXT UserID is an active Associate (and, for an Owner, that the Associate works on that company).
     A new company starts on the Copper plan; only the Developer changes a plan. Setup's usp_Company_Create and the general
     usp_User_Create stay denied to the app (ADR 0004 amended by ADR 0013).
   Error numbers: 50030 not an Associate, 50031 name missing, 50032 company not managed by this Associate or inactive.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_User_Company' AND parent_object_id = OBJECT_ID(N'sec.[User]')
           AND definition NOT LIKE N'%(3)%')
    ALTER TABLE sec.[User] DROP CONSTRAINT CK_User_Company;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_User_Company' AND parent_object_id = OBJECT_ID(N'sec.[User]'))
    ALTER TABLE sec.[User] ADD CONSTRAINT CK_User_Company
        CHECK ((RoleID = 1 AND CompanyID IS NULL) OR RoleID = 3 OR (RoleID IN (2, 4) AND CompanyID IS NOT NULL));
GO
IF OBJECT_ID(N'sec.UserCompany', N'U') IS NULL
BEGIN
    CREATE TABLE sec.UserCompany
    (
        UserID     INT          NOT NULL CONSTRAINT FK_UserCompany_User REFERENCES sec.[User] (UserID),
        CompanyID  INT          NOT NULL CONSTRAINT FK_UserCompany_Company REFERENCES mst.Company (CompanyID),
        CreatedUtc DATETIME2(0) NOT NULL CONSTRAINT DF_UserCompany_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_UserCompany PRIMARY KEY (UserID, CompanyID)
    );
    CREATE INDEX IX_UserCompany_Company ON sec.UserCompany (CompanyID);
END
GO
INSERT sec.UserCompany (UserID, CompanyID)
SELECT u.UserID, u.CompanyID
FROM sec.[User] u
WHERE u.RoleID = 3 AND u.CompanyID IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM sec.UserCompany x WHERE x.UserID = u.UserID AND x.CompanyID = u.CompanyID);
GO
CREATE OR ALTER PROCEDURE sec.usp_User_Create
    @UserName NVARCHAR(100), @CompanyID INT, @RoleID TINYINT,
    @PasswordHash VARBINARY(32), @PasswordSalt VARBINARY(16), @Iterations INT, @MustChangePassword BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;
        INSERT sec.[User] (UserName, CompanyID, RoleID, PasswordHash, PasswordSalt, Iterations, MustChangePassword)
        VALUES (@UserName, @CompanyID, @RoleID, @PasswordHash, @PasswordSalt, @Iterations, @MustChangePassword);
        DECLARE @UserID INT = CAST(SCOPE_IDENTITY() AS INT);
        IF @RoleID = 3 AND @CompanyID IS NOT NULL
            INSERT sec.UserCompany (UserID, CompanyID) VALUES (@UserID, @CompanyID);
    COMMIT;
    SELECT @UserID;
END
GO
CREATE OR ALTER PROCEDURE sec.usp_UserCompany_List @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.CompanyID, c.CompanyName, c.PlanTierID
    FROM sec.UserCompany uc
    JOIN mst.Company c ON c.CompanyID = uc.CompanyID
    WHERE uc.UserID = @UserID AND c.IsActive = 1
    ORDER BY c.CompanyName, c.CompanyID;
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
        INSERT mst.Company (CompanyName, PlanTierID) VALUES (@Name, 1);
        SET @CompanyID = CAST(SCOPE_IDENTITY() AS INT);
        INSERT sec.UserCompany (UserID, CompanyID) VALUES (@UserID, @CompanyID);
    COMMIT;
    SELECT @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE sec.usp_Associate_CreateOwner
    @CompanyID INT, @UserName NVARCHAR(100), @PasswordHash VARBINARY(32), @PasswordSalt VARBINARY(16), @Iterations INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @UserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    IF @UserID IS NULL OR NOT EXISTS (SELECT 1 FROM sec.[User] WHERE UserID = @UserID AND RoleID = 3 AND IsActive = 1)
        THROW 50030, N'Only an Associate can create an Owner login here.', 1;
    IF NOT EXISTS (SELECT 1 FROM sec.UserCompany uc JOIN mst.Company c ON c.CompanyID = uc.CompanyID
                   WHERE uc.UserID = @UserID AND uc.CompanyID = @CompanyID AND c.IsActive = 1)
        THROW 50032, N'You do not work on that company.', 1;
    INSERT sec.[User] (UserName, CompanyID, RoleID, PasswordHash, PasswordSalt, Iterations, MustChangePassword)
    VALUES (LTRIM(RTRIM(@UserName)), @CompanyID, 2, @PasswordHash, @PasswordSalt, @Iterations, 1);
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO
