/* 0009_approval_requests.sql
   Maker-checker approvals (ADR 0009). One row per request. A filtered unique index allows only one pending
   request per entity. The tenant security policy hides other companies' rows. Idempotent. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'sec.ApprovalRequest', N'U') IS NULL
BEGIN
    CREATE TABLE sec.ApprovalRequest
    (
        ApprovalID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApprovalRequest PRIMARY KEY,
        CompanyID       INT            NOT NULL CONSTRAINT FK_ApprovalRequest_Company REFERENCES mst.Company (CompanyID),
        ModuleID        TINYINT        NOT NULL CONSTRAINT CK_ApprovalRequest_Module CHECK (ModuleID BETWEEN 1 AND 12),
        EntityName      NVARCHAR(100)  NOT NULL,
        EntityID        NVARCHAR(100)  NOT NULL,
        Summary         NVARCHAR(500)  NOT NULL,
        [Status]        TINYINT        NOT NULL CONSTRAINT DF_ApprovalRequest_Status DEFAULT 1
                                        CONSTRAINT CK_ApprovalRequest_Status CHECK ([Status] BETWEEN 1 AND 4),
        SubmittedBy     INT            NOT NULL CONSTRAINT FK_ApprovalRequest_SubmittedBy REFERENCES sec.[User] (UserID),
        SubmittedOn     DATETIME2(0)   NOT NULL CONSTRAINT DF_ApprovalRequest_SubmittedOn DEFAULT SYSUTCDATETIME(),
        DecidedBy       INT            NULL CONSTRAINT FK_ApprovalRequest_DecidedBy REFERENCES sec.[User] (UserID),
        DecidedOn       DATETIME2(0)   NULL,
        DecisionComment NVARCHAR(500)  NULL
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ApprovalRequest_OnePending')
    CREATE UNIQUE INDEX UX_ApprovalRequest_OnePending ON sec.ApprovalRequest (CompanyID, ModuleID, EntityName, EntityID)
        WHERE [Status] = 1;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ApprovalRequest_Entity')
    CREATE INDEX IX_ApprovalRequest_Entity ON sec.ApprovalRequest (CompanyID, ModuleID, EntityName, EntityID, ApprovalID DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_ApprovalRequest')
    EXEC (N'CREATE SECURITY POLICY sec.TenantPolicy_ApprovalRequest
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON sec.ApprovalRequest
            WITH (STATE = ON)');
GO
CREATE OR ALTER PROCEDURE sec.usp_Approval_Submit
    @ModuleID TINYINT, @EntityName NVARCHAR(100), @EntityID NVARCHAR(100), @Summary NVARCHAR(500), @SubmittedBy INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL
        THROW 50001, 'Approvals need a company session.', 1;
    IF EXISTS (SELECT 1 FROM sec.ApprovalRequest
               WHERE CompanyID = @CompanyID AND ModuleID = @ModuleID AND EntityName = @EntityName
                 AND EntityID = @EntityID AND [Status] = 1)
    BEGIN
        SELECT CAST(0 AS INT);
        RETURN;
    END
    INSERT sec.ApprovalRequest (CompanyID, ModuleID, EntityName, EntityID, Summary, SubmittedBy)
    VALUES (@CompanyID, @ModuleID, @EntityName, @EntityID, @Summary, @SubmittedBy);
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO
CREATE OR ALTER PROCEDURE sec.usp_Approval_Decide
    @ApprovalID INT, @Status TINYINT, @DecidedBy INT, @Comment NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    /* Approve (2) and Reject (3): decider is not the submitter. Cancel (4): decider is the submitter. */
    UPDATE sec.ApprovalRequest
       SET [Status] = @Status, DecidedBy = @DecidedBy, DecidedOn = SYSUTCDATETIME(), DecisionComment = @Comment
     WHERE ApprovalID = @ApprovalID AND [Status] = 1
       AND ((@Status IN (2, 3) AND SubmittedBy <> @DecidedBy) OR (@Status = 4 AND SubmittedBy = @DecidedBy));
    SELECT CAST(@@ROWCOUNT AS INT);
END
GO
CREATE OR ALTER PROCEDURE sec.usp_Approval_GetById @ApprovalID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.ApprovalID, a.ModuleID, a.EntityName, a.EntityID, a.Summary, a.[Status], a.SubmittedBy,
           s.UserName AS SubmittedByName, a.SubmittedOn, a.DecidedBy, d.UserName AS DecidedByName, a.DecidedOn, a.DecisionComment
      FROM sec.ApprovalRequest a
      JOIN sec.[User] s ON s.UserID = a.SubmittedBy
      LEFT JOIN sec.[User] d ON d.UserID = a.DecidedBy
     WHERE a.ApprovalID = @ApprovalID;
END
GO
CREATE OR ALTER PROCEDURE sec.usp_Approval_GetLatest @ModuleID TINYINT, @EntityName NVARCHAR(100), @EntityID NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) a.ApprovalID, a.ModuleID, a.EntityName, a.EntityID, a.Summary, a.[Status], a.SubmittedBy,
           s.UserName AS SubmittedByName, a.SubmittedOn, a.DecidedBy, d.UserName AS DecidedByName, a.DecidedOn, a.DecisionComment
      FROM sec.ApprovalRequest a
      JOIN sec.[User] s ON s.UserID = a.SubmittedBy
      LEFT JOIN sec.[User] d ON d.UserID = a.DecidedBy
     WHERE a.ModuleID = @ModuleID AND a.EntityName = @EntityName AND a.EntityID = @EntityID
     ORDER BY a.ApprovalID DESC;
END
GO
CREATE OR ALTER PROCEDURE sec.usp_Approval_ListPending @ModuleID TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.ApprovalID, a.ModuleID, a.EntityName, a.EntityID, a.Summary, a.[Status], a.SubmittedBy,
           s.UserName AS SubmittedByName, a.SubmittedOn, a.DecidedBy, d.UserName AS DecidedByName, a.DecidedOn, a.DecisionComment
      FROM sec.ApprovalRequest a
      JOIN sec.[User] s ON s.UserID = a.SubmittedBy
      LEFT JOIN sec.[User] d ON d.UserID = a.DecidedBy
     WHERE a.ModuleID = @ModuleID AND a.[Status] = 1
     ORDER BY a.ApprovalID;
END
GO
