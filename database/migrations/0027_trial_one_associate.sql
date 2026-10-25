/* 0027_trial_one_associate.sql
   ADR 0018, decided 2026-10-24: the trial allows one Associate. An Associate with no current licence row is on the trial (a revoked licence is
   still a current row, so a revoked Associate is not on the trial). cfg.usp_Licence_RequireTrialRoom refuses to create another Associate while an
   active Associate has no current licence: the Developer imports a licence for the first Associate before creating the second.
   Existing Associates are not touched; the rule only stops new ones. Deactivating an Associate frees the place.
   sec.usp_User_Create calls the helper for a new Associate (RoleID 3) inside its transaction, under an exclusive application lock so two
   Associates created at once cannot both take the one place. Everything else in sec.usp_User_Create is as migration 0020.
   Errors: 50055 the trial allows one Associate, 50056 the lock could not be taken (try again).
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE cfg.usp_Licence_RequireTrialRoom
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Lock INT;
    EXEC @Lock = sp_getapplock @Resource = N'Revolution.TrialAssociate', @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
    IF @Lock < 0
        THROW 50056, N'Another Associate is being created. Try again.', 1;
    IF EXISTS (SELECT 1
               FROM sec.[User] su
               WHERE su.RoleID = 3 AND su.IsActive = 1
                 AND NOT EXISTS (SELECT 1 FROM cfg.Licence l WHERE l.AssociateUserID = su.UserID AND l.IsCurrent = 1))
        THROW 50055, N'The trial allows one Associate. Import a licence for the existing Associate before creating another.', 1;
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
        IF @RoleID = 3
            EXEC cfg.usp_Licence_RequireTrialRoom;
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
