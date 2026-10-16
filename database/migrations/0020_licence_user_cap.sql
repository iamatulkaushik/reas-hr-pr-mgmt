/* 0020_licence_user_cap.sql
   ADR 0018, step 2b. The licence's user cap, enforced in the database. Idempotent. Never edit after merge: add a new number.
   What counts: active Owner and Operator logins in the companies an Associate works on. Developers and Associates are never counted.
   A company worked on by several Associates is governed by the most generous licence: creating a user is refused only when EVERY linked
   Associate that has a current licence is already at the cap. No current licence means no database cap (trial and unlicensed use are the app's job).
   Error 50039: the licence allows no more active users.
   cfg.usp_Licence_RequireUserRoom is the one place that rule lives. sec.usp_User_Create and sec.usp_Associate_CreateOwner call it inside their
   transaction, and it locks the licence rows so two logins created at once cannot both take the last place. Reactivating a user is not covered:
   no procedure does that yet; when one is added it must call the same helper. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE cfg.usp_Licence_RequireUserRoom @CompanyID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Licensed INT, @WithRoom INT;
    SELECT @Licensed = COUNT(*),
           @WithRoom = COALESCE(SUM(CASE WHEN x.Used < l.MaxActiveUsers THEN 1 ELSE 0 END), 0)
    FROM cfg.Licence l WITH (UPDLOCK, HOLDLOCK)
    CROSS APPLY (SELECT COUNT(*) AS Used
                 FROM sec.[User] su
                 WHERE su.RoleID IN (2, 4) AND su.IsActive = 1
                   AND su.CompanyID IN (SELECT uc.CompanyID FROM sec.UserCompany uc WHERE uc.UserID = l.AssociateUserID)) x
    WHERE l.IsCurrent = 1
      AND EXISTS (SELECT 1 FROM sec.UserCompany lc WHERE lc.UserID = l.AssociateUserID AND lc.CompanyID = @CompanyID);
    IF @Licensed > 0 AND @WithRoom = 0
        THROW 50039, N'The licence allows no more active users. Ask the Developer for a licence with more users.', 1;
END
GO
CREATE OR ALTER PROCEDURE sec.usp_User_Create
    @UserName NVARCHAR(100), @CompanyID INT, @RoleID TINYINT,
    @PasswordHash VARBINARY(32), @PasswordSalt VARBINARY(16), @Iterations INT, @MustChangePassword BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;
        IF @RoleID IN (2, 4) AND @CompanyID IS NOT NULL
            EXEC cfg.usp_Licence_RequireUserRoom @CompanyID;
        INSERT sec.[User] (UserName, CompanyID, RoleID, PasswordHash, PasswordSalt, Iterations, MustChangePassword, AssociatePlanTierID)
        VALUES (@UserName, @CompanyID, @RoleID, @PasswordHash, @PasswordSalt, @Iterations, @MustChangePassword,
                CASE WHEN @RoleID = 3 THEN 1 END);
        DECLARE @UserID INT = CAST(SCOPE_IDENTITY() AS INT);
        IF @RoleID = 3 AND @CompanyID IS NOT NULL
            INSERT sec.UserCompany (UserID, CompanyID) VALUES (@UserID, @CompanyID);
    COMMIT;
    SELECT @UserID;
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
    DECLARE @NewID INT;
    BEGIN TRAN;
        EXEC cfg.usp_Licence_RequireUserRoom @CompanyID;
        INSERT sec.[User] (UserName, CompanyID, RoleID, PasswordHash, PasswordSalt, Iterations, MustChangePassword)
        VALUES (LTRIM(RTRIM(@UserName)), @CompanyID, 2, @PasswordHash, @PasswordSalt, @Iterations, 1);
        SET @NewID = CAST(SCOPE_IDENTITY() AS INT);
    COMMIT;
    SELECT @NewID;
END
GO
