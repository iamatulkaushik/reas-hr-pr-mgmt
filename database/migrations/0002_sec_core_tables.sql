/* 0002_sec_core_tables.sql
   Company, user, role permission and audit tables. Idempotent. Never edit after merge: add a new number.
   Developer users have CompanyID NULL; every other role must belong to a company. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'mst.Company', N'U') IS NULL
BEGIN
    CREATE TABLE mst.Company
    (
        CompanyID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Company PRIMARY KEY,
        CompanyName NVARCHAR(200)     NOT NULL,
        PlanTierID  TINYINT           NOT NULL CONSTRAINT CK_Company_PlanTier CHECK (PlanTierID BETWEEN 1 AND 4),
        IsActive    BIT               NOT NULL CONSTRAINT DF_Company_IsActive DEFAULT 1,
        CreatedUtc  DATETIME2(0)      NOT NULL CONSTRAINT DF_Company_CreatedUtc DEFAULT SYSUTCDATETIME()
    );
END
GO
IF OBJECT_ID(N'sec.[User]', N'U') IS NULL
BEGIN
    CREATE TABLE sec.[User]
    (
        UserID             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_User PRIMARY KEY,
        CompanyID          INT           NULL CONSTRAINT FK_User_Company REFERENCES mst.Company (CompanyID),
        UserName           NVARCHAR(100) NOT NULL CONSTRAINT UQ_User_UserName UNIQUE,
        PasswordHash       VARBINARY(32) NOT NULL,
        PasswordSalt       VARBINARY(16) NOT NULL,
        Iterations         INT           NOT NULL,
        RoleID             TINYINT       NOT NULL CONSTRAINT CK_User_Role CHECK (RoleID BETWEEN 1 AND 4),
        IsActive           BIT           NOT NULL CONSTRAINT DF_User_IsActive DEFAULT 1,
        MustChangePassword BIT           NOT NULL CONSTRAINT DF_User_MustChange DEFAULT 1,
        FailedAttempts     INT           NOT NULL CONSTRAINT DF_User_FailedAttempts DEFAULT 0,
        LockedUntilUtc     DATETIME2(0)  NULL,
        LastLoginUtc       DATETIME2(0)  NULL,
        CreatedUtc         DATETIME2(0)  NOT NULL CONSTRAINT DF_User_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_User_Company CHECK ((RoleID = 1 AND CompanyID IS NULL) OR (RoleID <> 1 AND CompanyID IS NOT NULL))
    );
END
GO
IF OBJECT_ID(N'sec.RolePermission', N'U') IS NULL
BEGIN
    CREATE TABLE sec.RolePermission
    (
        RoleID   TINYINT NOT NULL CONSTRAINT CK_RolePermission_Role   CHECK (RoleID BETWEEN 1 AND 4),
        ModuleID TINYINT NOT NULL CONSTRAINT CK_RolePermission_Module CHECK (ModuleID BETWEEN 1 AND 12),
        ActionID TINYINT NOT NULL CONSTRAINT CK_RolePermission_Action CHECK (ActionID BETWEEN 1 AND 6),
        CONSTRAINT PK_RolePermission PRIMARY KEY (RoleID, ModuleID, ActionID)
    );
END
GO
IF OBJECT_ID(N'aud.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE aud.AuditLog
    (
        AuditID    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLog PRIMARY KEY,
        CompanyID  INT           NULL,
        UserID     INT           NULL,
        ModuleID   TINYINT       NOT NULL,
        Action     NVARCHAR(100) NOT NULL,
        EntityName NVARCHAR(100) NOT NULL,
        EntityID   NVARCHAR(100) NOT NULL,
        Details    NVARCHAR(2000) NOT NULL,
        CreatedUtc DATETIME2(3)  NOT NULL CONSTRAINT DF_AuditLog_CreatedUtc DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_AuditLog_Company_Created ON aud.AuditLog (CompanyID, CreatedUtc DESC);
END
GO
