/* 0603_ast_recovery_tables.sql
   Phase 9 step 0601a: recovery of a lost or damaged asset from the employee who held it. Brave lane 0600-0699. Idempotent. Never edit after merge. Tenant policy is 0604, procedures are 0605.
   ast.AssetRecovery: one row per hand-over (IssueID), at most one active row. The amount is decided by the Owner; the default is the depreciated value on BasisDate (straight line over the type's useful life,
   floor of zero, whole rupees, computed in the service and stored here as DepreciatedValue). A different amount needs a Reason.
   StatusID: 1 Open (waiting to be taken), 2 Cancelled, 3 Recovered (held by a regular run, or taken on the full and final settlement; AppliedRunID is the run, step 0601b wires it). Rows are never deleted.
   Needs a unique key on ast.AssetIssue (CompanyID, IssueID) for the foreign key. Error numbers 53510 to 53513 (0605). Expenses use 53520 and up. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_AssetIssue_Company' AND parent_object_id = OBJECT_ID(N'ast.AssetIssue'))
    ALTER TABLE ast.AssetIssue ADD CONSTRAINT UQ_AssetIssue_Company UNIQUE (CompanyID, IssueID);
GO
IF OBJECT_ID(N'ast.AssetRecovery', N'U') IS NULL
BEGIN
    CREATE TABLE ast.AssetRecovery
    (
        RecoveryID       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AssetRecovery PRIMARY KEY,
        CompanyID        INT           NOT NULL CONSTRAINT FK_AssetRecovery_Company REFERENCES mst.Company (CompanyID),
        AssetID          INT           NOT NULL,
        IssueID          INT           NOT NULL,
        EmployeeID       INT           NOT NULL,
        BasisDate        DATE          NOT NULL,
        DepreciatedValue DECIMAL(12,2) NOT NULL CONSTRAINT CK_AssetRecovery_Dep CHECK (DepreciatedValue >= 0),
        Amount           DECIMAL(12,2) NOT NULL CONSTRAINT CK_AssetRecovery_Amount CHECK (Amount > 0),
        Reason           NVARCHAR(200) NULL,
        StatusID         TINYINT       NOT NULL CONSTRAINT DF_AssetRecovery_Status DEFAULT 1 CONSTRAINT CK_AssetRecovery_Status CHECK (StatusID BETWEEN 1 AND 3),
        AppliedRunID     INT           NULL,
        AppliedUtc       DATETIME2(0)  NULL,
        CancelledUtc     DATETIME2(0)  NULL,
        CancelReason     NVARCHAR(200) NULL,
        CreatedUtc       DATETIME2(0)  NOT NULL CONSTRAINT DF_AssetRecovery_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CreatedByUserID  INT           NULL,
        CONSTRAINT FK_AssetRecovery_Asset FOREIGN KEY (CompanyID, AssetID) REFERENCES ast.Asset (CompanyID, AssetID),
        CONSTRAINT FK_AssetRecovery_Issue FOREIGN KEY (CompanyID, IssueID) REFERENCES ast.AssetIssue (CompanyID, IssueID),
        CONSTRAINT FK_AssetRecovery_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID)
    );
    CREATE UNIQUE INDEX UX_AssetRecovery_Active ON ast.AssetRecovery (CompanyID, IssueID) WHERE StatusID IN (1, 3);
    CREATE INDEX IX_AssetRecovery_Employee ON ast.AssetRecovery (CompanyID, EmployeeID, StatusID);
END
GO
