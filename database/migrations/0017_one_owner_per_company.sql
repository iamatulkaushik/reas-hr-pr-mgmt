/* 0017_one_owner_per_company.sql
   ADR 0014 amended: one Owner login per company, created by an Associate. An Operator is never turned into an Owner (no procedure
   changes a user's role). The unique index is the hard rule; sec.usp_Associate_CreateOwner also checks first, for a clear message (50033).
   If a company already has more than one Owner this script stops with error 50040: fix those rows by hand, then run it again.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_User_OneOwnerPerCompany' AND object_id = OBJECT_ID(N'sec.[User]'))
BEGIN
    IF EXISTS (SELECT 1 FROM sec.[User] WHERE RoleID = 2 GROUP BY CompanyID HAVING COUNT(*) > 1)
        THROW 50040, N'A company has more than one Owner. Keep one, deactivate or move the others, then run this script again.', 1;
    CREATE UNIQUE INDEX UX_User_OneOwnerPerCompany ON sec.[User] (CompanyID) WHERE RoleID = 2;
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
    IF EXISTS (SELECT 1 FROM sec.[User] WHERE CompanyID = @CompanyID AND RoleID = 2)
        THROW 50033, N'This company already has an Owner login.', 1;
    INSERT sec.[User] (UserName, CompanyID, RoleID, PasswordHash, PasswordSalt, Iterations, MustChangePassword)
    VALUES (LTRIM(RTRIM(@UserName)), @CompanyID, 2, @PasswordHash, @PasswordSalt, @Iterations, 1);
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO
