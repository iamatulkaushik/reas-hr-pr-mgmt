/* 0900_sec_agent_registration.sql (biometric special build, ADR 0021: apply with RevolutionSetup migrate database/special/biometric after the standard migrations)
   ADR 0012, ADR 0013, change requests 0023 and 0025. Idempotent. Never edit after merge: add a new number.
   sec.AgentRegistration: one row per punch agent (one computer on a factory LAN) of one company. The key itself is never stored: only SHA-256 of its 32 random bytes.
   No tenant filter policy on this table, on purpose: the agent wrapper (0901) must find the row from the key before it knows the company. The agent login has no
   SELECT on the table, and the app login reaches it only through the three procedures below, which take the company from the session.
   sec.usp_Agent_Issue returns the key once, as base64 text (44 characters). It is shown to the user once and cannot be read again; a lost key is retired and a new one issued.
   Who may issue, list or retire: the Owner of the company (RoleID 2), an Associate that has the company (RoleID 3 and a sec.UserCompany row), or a Developer session. Not an Operator.
   Errors: 50002 no company in session, 53410 not allowed for this user, 53411 a label is needed (1 to 60 characters), 53412 no such agent registration in this company. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'sec.AgentRegistration', N'U') IS NULL
BEGIN
    CREATE TABLE sec.AgentRegistration
    (
        AgentRegistrationID INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_AgentRegistration PRIMARY KEY,
        CompanyID           INT           NOT NULL CONSTRAINT FK_AgentRegistration_Company REFERENCES mst.Company (CompanyID),
        Label               NVARCHAR(60)  NOT NULL,
        KeyHash             BINARY(32)    NOT NULL CONSTRAINT UQ_AgentRegistration_KeyHash UNIQUE,
        IsActive            BIT           NOT NULL CONSTRAINT DF_AgentRegistration_IsActive DEFAULT 1,
        CreatedByUserID     INT           NOT NULL CONSTRAINT FK_AgentRegistration_CreatedBy REFERENCES sec.[User] (UserID),
        CreatedUtc          DATETIME2(0)  NOT NULL CONSTRAINT DF_AgentRegistration_CreatedUtc DEFAULT SYSUTCDATETIME(),
        RetiredUtc          DATETIME2(0)  NULL,
        LastSeenUtc         DATETIME2(0)  NULL
    );
    CREATE INDEX IX_AgentRegistration_Company ON sec.AgentRegistration (CompanyID);
END
GO
CREATE OR ALTER PROCEDURE sec.usp_Agent_Issue @Label NVARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    DECLARE @UserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @UserID IS NULL OR NOT EXISTS
        (SELECT 1 FROM sec.[User] u WHERE u.UserID = @UserID AND u.IsActive = 1
            AND (u.RoleID = 1 OR (u.RoleID = 2 AND u.CompanyID = @CompanyID)
                 OR (u.RoleID = 3 AND EXISTS (SELECT 1 FROM sec.UserCompany uc WHERE uc.UserID = u.UserID AND uc.CompanyID = @CompanyID))))
        THROW 53410, N'Only the Owner or the Associate of this company can manage agent keys.', 1;
    DECLARE @Name NVARCHAR(60) = LTRIM(RTRIM(@Label));
    IF @Name IS NULL OR LEN(@Name) = 0 THROW 53411, N'Give the agent a label (for example the computer name).', 1;
    DECLARE @Key VARBINARY(32) = CRYPT_GEN_RANDOM(32);
    DECLARE @Id INT;
    INSERT sec.AgentRegistration (CompanyID, Label, KeyHash, CreatedByUserID)
    VALUES (@CompanyID, @Name, HASHBYTES('SHA2_256', @Key), @UserID);
    SET @Id = CAST(SCOPE_IDENTITY() AS INT);
    SELECT @Id AS AgentRegistrationID, CAST((SELECT @Key AS [*] FOR XML PATH(''), BINARY BASE64) AS NVARCHAR(100)) AS AgentKey;
END
GO
CREATE OR ALTER PROCEDURE sec.usp_Agent_List
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    DECLARE @UserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @UserID IS NULL OR NOT EXISTS
        (SELECT 1 FROM sec.[User] u WHERE u.UserID = @UserID AND u.IsActive = 1
            AND (u.RoleID = 1 OR (u.RoleID = 2 AND u.CompanyID = @CompanyID)
                 OR (u.RoleID = 3 AND EXISTS (SELECT 1 FROM sec.UserCompany uc WHERE uc.UserID = u.UserID AND uc.CompanyID = @CompanyID))))
        THROW 53410, N'Only the Owner or the Associate of this company can manage agent keys.', 1;
    SELECT AgentRegistrationID, Label, IsActive, CreatedUtc, RetiredUtc, LastSeenUtc
    FROM sec.AgentRegistration
    WHERE CompanyID = @CompanyID
    ORDER BY IsActive DESC, AgentRegistrationID DESC;
END
GO
CREATE OR ALTER PROCEDURE sec.usp_Agent_Retire @AgentRegistrationID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    DECLARE @UserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @UserID IS NULL OR NOT EXISTS
        (SELECT 1 FROM sec.[User] u WHERE u.UserID = @UserID AND u.IsActive = 1
            AND (u.RoleID = 1 OR (u.RoleID = 2 AND u.CompanyID = @CompanyID)
                 OR (u.RoleID = 3 AND EXISTS (SELECT 1 FROM sec.UserCompany uc WHERE uc.UserID = u.UserID AND uc.CompanyID = @CompanyID))))
        THROW 53410, N'Only the Owner or the Associate of this company can manage agent keys.', 1;
    IF NOT EXISTS (SELECT 1 FROM sec.AgentRegistration WHERE AgentRegistrationID = @AgentRegistrationID AND CompanyID = @CompanyID)
        THROW 53412, N'No such agent registration in this company.', 1;
    UPDATE sec.AgentRegistration SET IsActive = 0, RetiredUtc = SYSUTCDATETIME()
    WHERE AgentRegistrationID = @AgentRegistrationID AND CompanyID = @CompanyID AND IsActive = 1;
END
GO
