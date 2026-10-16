/* 0021_licence_sets_associate_plan.sql
   ADR 0018 amended: licensing belongs to the Associate, one current licence each, and the licence's plan (Copper, Silver, Gold, Platinum)
   IS that Associate's plan. Importing a licence now also sets sec.[User].AssociatePlanTierID to the licence's plan, so the plan the
   Developer sees on the Associate, the company quota and the licence cannot disagree. A new licence replaces the old one
   (cfg.Licence keeps older rows, one current per Associate: UX_Licence_OneCurrent from 0019).
   sec.usp_Associate_SetPlan (setup tool, 0018) stays for an Associate who has no licence yet. Importing a licence overrides it.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
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
        UPDATE sec.[User]
        SET MaxCompaniesOverride = @MaxCompanies, AssociatePlanTierID = @PlanCapTierID
        WHERE UserID = @AssociateID;
    COMMIT;
END
GO
