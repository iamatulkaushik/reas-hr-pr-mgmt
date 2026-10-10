/* 0003_sec_procs.sql
   Stored procedures for login, passwords, company plan, role permissions and audit. CREATE OR ALTER keeps it idempotent. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE sec.usp_User_GetByUserName @UserName NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UserID, UserName, CompanyID, RoleID, IsActive, MustChangePassword,
           PasswordHash, PasswordSalt, Iterations, LockedUntilUtc
    FROM sec.[User]
    WHERE UserName = @UserName;
END
GO
CREATE OR ALTER PROCEDURE sec.usp_User_RecordFailedLogin @UserID INT, @MaxAttempts INT, @LockMinutes INT
AS
BEGIN
    SET NOCOUNT ON;
    /* Right-hand sides read the old values: lock when this failure reaches the limit, then restart the count. */
    UPDATE sec.[User]
    SET LockedUntilUtc = CASE WHEN FailedAttempts + 1 >= @MaxAttempts
                              THEN DATEADD(MINUTE, @LockMinutes, SYSUTCDATETIME()) ELSE LockedUntilUtc END,
        FailedAttempts = CASE WHEN FailedAttempts + 1 >= @MaxAttempts THEN 0 ELSE FailedAttempts + 1 END
    WHERE UserID = @UserID;
END
GO
CREATE OR ALTER PROCEDURE sec.usp_User_RecordSuccessfulLogin @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE sec.[User]
    SET FailedAttempts = 0, LockedUntilUtc = NULL, LastLoginUtc = SYSUTCDATETIME()
    WHERE UserID = @UserID;
END
GO
CREATE OR ALTER PROCEDURE sec.usp_User_SetPassword
    @UserID INT, @PasswordHash VARBINARY(32), @PasswordSalt VARBINARY(16), @Iterations INT, @MustChangePassword BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE sec.[User]
    SET PasswordHash = @PasswordHash, PasswordSalt = @PasswordSalt, Iterations = @Iterations,
        MustChangePassword = @MustChangePassword, FailedAttempts = 0, LockedUntilUtc = NULL
    WHERE UserID = @UserID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Company_GetPlan @CompanyID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PlanTierID FROM mst.Company WHERE CompanyID = @CompanyID AND IsActive = 1;
END
GO
CREATE OR ALTER PROCEDURE sec.usp_Permission_ListForRole @RoleID TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ModuleID, ActionID FROM sec.RolePermission WHERE RoleID = @RoleID;
END
GO
CREATE OR ALTER PROCEDURE aud.usp_Audit_Insert
    @UserID INT, @CompanyID INT, @ModuleID TINYINT, @Action NVARCHAR(100),
    @EntityName NVARCHAR(100), @EntityID NVARCHAR(100), @Details NVARCHAR(2000)
AS
BEGIN
    SET NOCOUNT ON;
    /* The session's tenant wins over the parameter, so a caller cannot write into another company's trail. */
    INSERT aud.AuditLog (CompanyID, UserID, ModuleID, Action, EntityName, EntityID, Details)
    VALUES (COALESCE(TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID')), @CompanyID), @UserID, @ModuleID,
            @Action, @EntityName, @EntityID, @Details);
END
GO
CREATE OR ALTER PROCEDURE aud.usp_Audit_List @Top INT = 200
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Top) AuditID, CompanyID, UserID, ModuleID, Action, EntityName, EntityID, Details, CreatedUtc
    FROM aud.AuditLog
    ORDER BY AuditID DESC;
END
GO
