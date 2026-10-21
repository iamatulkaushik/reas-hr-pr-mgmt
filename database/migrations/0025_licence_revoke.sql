/* 0025_licence_revoke.sql
   ADR 0018 amended: the Developer can revoke an Associate's current licence, and reinstate it after a mistake.
   Revoked does not delete anything: the current row stays, with RevokedUtc, RevokedByUserID and a reason. The app then treats that Associate
   (and the Owners and Operators of the Associate's companies) as read-only: view and print only, no export, no create or edit. There is no
   grace period and no fall-back to the trial. A new licence for the same Associate replaces the revoked one as before (cfg.usp_Licence_Import).
   Only the Developer revokes or reinstates. Revoking is enforced by the app like expiry; the company and user caps stay enforced by the database.
   cfg.usp_Licence_ListCurrent and cfg.usp_Licence_ForSession gain two columns at the end (RevokedUtc, RevokeReason); the existing columns do not move.
   Errors: 50036 only the Developer, 50037 no such Associate, 50048 no current licence, 50049 already revoked, 50050 not revoked, 50051 reason missing.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF COL_LENGTH(N'cfg.Licence', N'RevokedUtc') IS NULL
    ALTER TABLE cfg.Licence ADD RevokedUtc DATETIME2(0) NULL;
GO
IF COL_LENGTH(N'cfg.Licence', N'RevokedByUserID') IS NULL
    ALTER TABLE cfg.Licence ADD RevokedByUserID INT NULL CONSTRAINT FK_Licence_RevokedBy REFERENCES sec.[User] (UserID);
GO
IF COL_LENGTH(N'cfg.Licence', N'RevokeReason') IS NULL
    ALTER TABLE cfg.Licence ADD RevokeReason NVARCHAR(200) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Licence_Revoked')
    ALTER TABLE cfg.Licence ADD CONSTRAINT CK_Licence_Revoked CHECK
        ((RevokedUtc IS NULL AND RevokedByUserID IS NULL AND RevokeReason IS NULL)
      OR (RevokedUtc IS NOT NULL AND RevokedByUserID IS NOT NULL AND RevokeReason IS NOT NULL));
GO
CREATE OR ALTER PROCEDURE cfg.usp_Licence_ListCurrent
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ByUserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    IF @ByUserID IS NULL OR NOT EXISTS (SELECT 1 FROM sec.[User] WHERE UserID = @ByUserID AND RoleID = 1 AND IsActive = 1)
        THROW 50036, N'Only the Developer can list licences.', 1;
    SELECT l.LicenceGuid, u.UserName AS AssociateUserName, l.LicenceText, l.ValidFrom, l.ValidTo,
           l.PlanCapTierID, l.MaxCompanies, l.MaxActiveUsers, l.ImportedUtc, l.RevokedUtc, l.RevokeReason
    FROM cfg.Licence l
    JOIN sec.[User] u ON u.UserID = l.AssociateUserID
    WHERE l.IsCurrent = 1
    ORDER BY u.UserName;
END
GO
CREATE OR ALTER PROCEDURE cfg.usp_Licence_ForSession
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @UserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    DECLARE @RoleID TINYINT = (SELECT RoleID FROM sec.[User] WHERE UserID = @UserID);
    SELECT l.LicenceGuid, u.UserName AS AssociateUserName, l.LicenceText, l.ValidFrom, l.ValidTo,
           l.PlanCapTierID, l.MaxCompanies, l.MaxActiveUsers, l.RevokedUtc, l.RevokeReason
    FROM cfg.Licence l
    JOIN sec.[User] u ON u.UserID = l.AssociateUserID
    WHERE l.IsCurrent = 1
      AND ((@RoleID = 3 AND l.AssociateUserID = @UserID)
        OR (@RoleID IN (2, 4) AND EXISTS (SELECT 1 FROM sec.UserCompany uc WHERE uc.UserID = l.AssociateUserID AND uc.CompanyID = @CompanyID)));
END
GO
CREATE OR ALTER PROCEDURE cfg.usp_Licence_Revoke @AssociateUserName NVARCHAR(100), @Reason NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @ByUserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    IF @ByUserID IS NULL OR NOT EXISTS (SELECT 1 FROM sec.[User] WHERE UserID = @ByUserID AND RoleID = 1 AND IsActive = 1)
        THROW 50036, N'Only the Developer can revoke a licence.', 1;
    DECLARE @Why NVARCHAR(200) = LTRIM(RTRIM(@Reason));
    IF @Why IS NULL OR LEN(@Why) = 0
        THROW 50051, N'Give a reason for revoking the licence.', 1;
    DECLARE @AssociateID INT = (SELECT UserID FROM sec.[User] WHERE UserName = @AssociateUserName AND RoleID = 3);
    IF @AssociateID IS NULL
        THROW 50037, N'No Associate has that user name.', 1;
    IF NOT EXISTS (SELECT 1 FROM cfg.Licence WHERE AssociateUserID = @AssociateID AND IsCurrent = 1)
        THROW 50048, N'That Associate has no current licence.', 1;
    UPDATE cfg.Licence
    SET RevokedUtc = SYSUTCDATETIME(), RevokedByUserID = @ByUserID, RevokeReason = @Why
    WHERE AssociateUserID = @AssociateID AND IsCurrent = 1 AND RevokedUtc IS NULL;
    IF @@ROWCOUNT = 0
        THROW 50049, N'This licence is already revoked.', 1;
END
GO
CREATE OR ALTER PROCEDURE cfg.usp_Licence_Reinstate @AssociateUserName NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @ByUserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    IF @ByUserID IS NULL OR NOT EXISTS (SELECT 1 FROM sec.[User] WHERE UserID = @ByUserID AND RoleID = 1 AND IsActive = 1)
        THROW 50036, N'Only the Developer can reinstate a licence.', 1;
    DECLARE @AssociateID INT = (SELECT UserID FROM sec.[User] WHERE UserName = @AssociateUserName AND RoleID = 3);
    IF @AssociateID IS NULL
        THROW 50037, N'No Associate has that user name.', 1;
    IF NOT EXISTS (SELECT 1 FROM cfg.Licence WHERE AssociateUserID = @AssociateID AND IsCurrent = 1)
        THROW 50048, N'That Associate has no current licence.', 1;
    UPDATE cfg.Licence
    SET RevokedUtc = NULL, RevokedByUserID = NULL, RevokeReason = NULL
    WHERE AssociateUserID = @AssociateID AND IsCurrent = 1 AND RevokedUtc IS NOT NULL;
    IF @@ROWCOUNT = 0
        THROW 50050, N'This licence is not revoked.', 1;
END
GO
