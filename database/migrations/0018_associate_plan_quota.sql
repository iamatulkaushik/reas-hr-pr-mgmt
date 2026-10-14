/* 0018_associate_plan_quota.sql
   ADR 0014 amended. An Associate has a plan (set by the Developer) and the plan caps how many companies the Associate may hold.
   Copper 5, Silver 15, Gold 35, Platinum 90. There is no unlimited plan: cfg.PlanQuota.MaxCompanies is NOT NULL with a hard ceiling,
   so a missing or unlimited value cannot be stored. An Associate without a plan counts as Copper.
   Only the Developer's setup tool changes an Associate's plan (sec.usp_Associate_SetPlan is denied to the app).
   Error 50034: quota reached. Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF COL_LENGTH(N'sec.[User]', N'AssociatePlanTierID') IS NULL
    ALTER TABLE sec.[User] ADD AssociatePlanTierID TINYINT NULL CONSTRAINT CK_User_AssociatePlan CHECK (AssociatePlanTierID BETWEEN 1 AND 4);
GO
UPDATE sec.[User] SET AssociatePlanTierID = 1 WHERE RoleID = 3 AND AssociatePlanTierID IS NULL;
GO
IF OBJECT_ID(N'cfg.PlanQuota', N'U') IS NULL
BEGIN
    CREATE TABLE cfg.PlanQuota
    (
        PlanTierID   TINYINT NOT NULL CONSTRAINT PK_PlanQuota PRIMARY KEY CONSTRAINT CK_PlanQuota_Plan CHECK (PlanTierID BETWEEN 1 AND 4),
        MaxCompanies SMALLINT NOT NULL CONSTRAINT CK_PlanQuota_Max CHECK (MaxCompanies BETWEEN 1 AND 500)
    );
END
GO
INSERT cfg.PlanQuota (PlanTierID, MaxCompanies)
SELECT v.PlanTierID, v.MaxCompanies
FROM (VALUES (1, 5), (2, 15), (3, 35), (4, 90)) v (PlanTierID, MaxCompanies)
WHERE NOT EXISTS (SELECT 1 FROM cfg.PlanQuota q WHERE q.PlanTierID = v.PlanTierID);
GO
CREATE OR ALTER PROCEDURE sec.usp_User_Create
    @UserName NVARCHAR(100), @CompanyID INT, @RoleID TINYINT,
    @PasswordHash VARBINARY(32), @PasswordSalt VARBINARY(16), @Iterations INT, @MustChangePassword BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;
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
CREATE OR ALTER PROCEDURE sec.usp_Associate_SetPlan @UserID INT, @PlanTierID TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE sec.[User] SET AssociatePlanTierID = @PlanTierID WHERE UserID = @UserID AND RoleID = 3;
    IF @@ROWCOUNT = 0
        THROW 50035, N'No Associate with that id.', 1;
END
GO
DENY EXECUTE ON OBJECT::sec.usp_Associate_SetPlan TO hrms_app;
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
        SELECT @Plan = COALESCE(AssociatePlanTierID, 1) FROM sec.[User] WITH (UPDLOCK, HOLDLOCK) WHERE UserID = @UserID;
        DECLARE @Max INT = (SELECT MaxCompanies FROM cfg.PlanQuota WHERE PlanTierID = @Plan);
        DECLARE @Held INT = (SELECT COUNT(*) FROM sec.UserCompany uc JOIN mst.Company c ON c.CompanyID = uc.CompanyID
                             WHERE uc.UserID = @UserID AND c.IsActive = 1);
        IF @Max IS NULL OR @Held >= @Max
        BEGIN
            DECLARE @Msg NVARCHAR(200) = CONCAT(N'Your plan allows ', COALESCE(@Max, 0), N' companies and you work on ', @Held,
                                                N'. Ask the Developer to raise your plan.');
            THROW 50034, @Msg, 1;
        END
        INSERT mst.Company (CompanyName, PlanTierID) VALUES (@Name, 1);
        SET @CompanyID = CAST(SCOPE_IDENTITY() AS INT);
        INSERT sec.UserCompany (UserID, CompanyID) VALUES (@UserID, @CompanyID);
    COMMIT;
    SELECT @CompanyID;
END
GO
