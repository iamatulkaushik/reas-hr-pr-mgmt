/* 0001_init_schemas.sql
   Creates the schemas and the migration history table. Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'cfg') EXEC (N'CREATE SCHEMA cfg');
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'sec') EXEC (N'CREATE SCHEMA sec');
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'mst') EXEC (N'CREATE SCHEMA mst');
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'att') EXEC (N'CREATE SCHEMA att');
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'pay') EXEC (N'CREATE SCHEMA pay');
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'stat') EXEC (N'CREATE SCHEMA stat');
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'aud') EXEC (N'CREATE SCHEMA aud');
GO
IF OBJECT_ID(N'cfg.SchemaVersion', N'U') IS NULL
BEGIN
    CREATE TABLE cfg.SchemaVersion
    (
        ScriptNumber CHAR(4)        NOT NULL CONSTRAINT PK_SchemaVersion PRIMARY KEY,
        ScriptName   NVARCHAR(200)  NOT NULL,
        Checksum     CHAR(64)       NOT NULL,
        AppliedOn    DATETIME2(0)   NOT NULL CONSTRAINT DF_SchemaVersion_AppliedOn DEFAULT SYSUTCDATETIME(),
        AppliedBy    NVARCHAR(128)  NOT NULL CONSTRAINT DF_SchemaVersion_AppliedBy DEFAULT SUSER_SNAME()
    );
END
GO
