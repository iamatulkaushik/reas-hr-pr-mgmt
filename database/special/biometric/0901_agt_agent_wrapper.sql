/* 0901_agt_agent_wrapper.sql (biometric special build, ADR 0021: apply with RevolutionSetup migrate database/special/biometric after the standard migrations)
   ADR 0012, ADR 0013, change requests 0023 and 0025. Idempotent. Never edit after merge: add a new number.
   Schema agt: the only door of the punch agent. The agent logs in as hrms_agent (a member of the database role hrms_agent, created by the Setup tool) and may execute
   agt procedures only. It has no SELECT on any table and no grant on att, sec or mst. The agt procedures reach those tables through ownership chaining (all schemas are owned by dbo).
   agt.usp_Agent_Authenticate is internal: it turns the key into a company and sets SESSION_CONTEXT CompanyID (not read-only, IsDeveloper off) for the rest of the call.
   It is denied to the agent and the app login; the two public procedures call it through the ownership chain.
   The key is the base64 of 32 random bytes (44 characters). The wrapper decodes it, takes SHA-256 and finds an active row by that hash. A key that is not valid base64, not 32 bytes,
   unknown or retired, an unknown device code, a device code of another company and an inactive device all raise the same error 53400. Numbers 53400 to 53409 are reserved for agt.
   att.usp_Punch_Ingest takes the company as a parameter, so it must never be callable by the app login: the standard migration 0505 denies it to hrms_app in every database.
   Ownership chaining skips the permission check on the chained procedure, so the wrapper still works. */
SET XACT_ABORT ON;
GO
IF SCHEMA_ID(N'agt') IS NULL EXEC (N'CREATE SCHEMA agt');
GO
IF DATABASE_PRINCIPAL_ID(N'hrms_agent') IS NULL CREATE ROLE hrms_agent;
GO
CREATE OR ALTER PROCEDURE agt.usp_Agent_Authenticate @AgentKey NVARCHAR(100), @CompanyID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Bytes VARBINARY(MAX) = NULL;
    IF @AgentKey IS NOT NULL AND LEN(@AgentKey) = 44
    BEGIN
        BEGIN TRY
            SET @Bytes = CAST(N'' AS XML).value('xs:base64Binary(sql:variable("@AgentKey"))', 'VARBINARY(MAX)');
        END TRY
        BEGIN CATCH
            SET @Bytes = NULL;
        END CATCH
    END
    IF @Bytes IS NULL OR DATALENGTH(@Bytes) <> 32 THROW 53400, N'The agent request was refused.', 1;
    DECLARE @Hash BINARY(32) = HASHBYTES('SHA2_256', @Bytes);
    DECLARE @RegistrationID INT;
    SELECT @RegistrationID = AgentRegistrationID, @CompanyID = CompanyID
    FROM sec.AgentRegistration
    WHERE KeyHash = @Hash AND IsActive = 1;
    IF @RegistrationID IS NULL THROW 53400, N'The agent request was refused.', 1;
    UPDATE sec.AgentRegistration SET LastSeenUtc = SYSUTCDATETIME()
    WHERE AgentRegistrationID = @RegistrationID AND (LastSeenUtc IS NULL OR LastSeenUtc < DATEADD(MINUTE, -1, SYSUTCDATETIME()));
    EXEC sp_set_session_context @key = N'IsDeveloper', @value = 0, @read_only = 0;
    EXEC sp_set_session_context @key = N'CompanyID', @value = @CompanyID, @read_only = 0;
END
GO
CREATE OR ALTER PROCEDURE agt.usp_Agent_Devices @AgentKey NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT;
    EXEC agt.usp_Agent_Authenticate @AgentKey, @CompanyID OUTPUT;
    SELECT DeviceCode, Brand, Host, Port, LastPunchTime
    FROM att.Device
    WHERE CompanyID = @CompanyID AND IsActive = 1
    ORDER BY DeviceCode;
END
GO
CREATE OR ALTER PROCEDURE agt.usp_Agent_Ingest @AgentKey NVARCHAR(100), @DeviceCode NVARCHAR(30), @PunchesJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT;
    EXEC agt.usp_Agent_Authenticate @AgentKey, @CompanyID OUTPUT;
    DECLARE @DeviceID INT = (SELECT DeviceID FROM att.Device WHERE CompanyID = @CompanyID AND DeviceCode = @DeviceCode AND IsActive = 1);
    IF @DeviceID IS NULL THROW 53400, N'The agent request was refused.', 1;
    EXEC att.usp_Punch_Ingest @CompanyID = @CompanyID, @DeviceID = @DeviceID, @PunchesJson = @PunchesJson;
END
GO
GRANT EXECUTE ON SCHEMA::agt TO hrms_agent;
GO
DENY EXECUTE ON OBJECT::agt.usp_Agent_Authenticate TO hrms_agent;
GO
DENY EXECUTE ON OBJECT::att.usp_Punch_Ingest TO hrms_agent;
GO
