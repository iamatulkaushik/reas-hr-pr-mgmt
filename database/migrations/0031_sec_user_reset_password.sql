/* 0031_sec_user_reset_password.sql
   ADR 0022. Idempotent. Never edit after merge: add a new number.
   Reset of another user's password from inside the app. Who may reset whom (one source, sec.fn_ResettableUsers):
     Developer (RoleID 1) resets an Associate (3);
     Associate (3) resets the Owner (2) of the company that is open in the session, and only a company the Associate works on (sec.UserCompany);
     Owner (2) resets an Operator (4) of the company that is open in the session, and only the Owner's own company.
   Nobody resets themselves here (they use Change password), nobody resets a Developer or an inactive user here (the Developer uses RevolutionSetup reset-password).
   The caller is read from SESSION_CONTEXT UserID and CompanyID, which the app sets read-only at sign-in. The new password is a temporary one: MustChangePassword is set, the lock and
   the failed-attempt count are cleared. The hash is made by the app (PasswordHasher); the database never sees a plain password.
   Errors: 50060 no signed-in user, 50061 own password, 50062 that user cannot be reset by this caller. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER FUNCTION sec.fn_ResettableUsers (@CallerID INT, @CompanyID INT)
RETURNS TABLE
AS
RETURN
(
    SELECT t.UserID, t.UserName, t.RoleID, t.MustChangePassword,
           CAST(CASE WHEN t.LockedUntilUtc IS NOT NULL AND t.LockedUntilUtc > SYSUTCDATETIME() THEN 1 ELSE 0 END AS BIT) AS IsLocked
    FROM sec.[User] c
    JOIN sec.[User] t ON t.UserID <> c.UserID AND t.IsActive = 1
    WHERE c.UserID = @CallerID AND c.IsActive = 1
      AND (   (c.RoleID = 1 AND t.RoleID = 3)
           OR (c.RoleID = 3 AND t.RoleID = 2 AND @CompanyID IS NOT NULL AND t.CompanyID = @CompanyID
               AND EXISTS (SELECT 1 FROM sec.UserCompany uc WHERE uc.UserID = c.UserID AND uc.CompanyID = @CompanyID))
           OR (c.RoleID = 2 AND t.RoleID = 4 AND @CompanyID IS NOT NULL AND t.CompanyID = @CompanyID AND c.CompanyID = @CompanyID))
);
GO
CREATE OR ALTER PROCEDURE sec.usp_User_ListResettable
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @UserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @UserID IS NULL THROW 50060, N'Nobody is signed in.', 1;
    SELECT UserID, UserName, RoleID, IsLocked, MustChangePassword
    FROM sec.fn_ResettableUsers(@UserID, @CompanyID)
    ORDER BY UserName;
END
GO
CREATE OR ALTER PROCEDURE sec.usp_User_ResetPassword
    @TargetUserID INT, @PasswordHash VARBINARY(32), @PasswordSalt VARBINARY(16), @Iterations INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @UserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @UserID IS NULL THROW 50060, N'Nobody is signed in.', 1;
    IF @TargetUserID = @UserID THROW 50061, N'Use Change password for your own password.', 1;
    IF NOT EXISTS (SELECT 1 FROM sec.fn_ResettableUsers(@UserID, @CompanyID) WHERE UserID = @TargetUserID)
        THROW 50062, N'You cannot reset the password of that user.', 1;
    UPDATE sec.[User]
    SET PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt, Iterations = @Iterations,
        MustChangePassword = 1, FailedAttempts = 0, LockedUntilUtc = NULL
    WHERE UserID = @TargetUserID;
END
GO
