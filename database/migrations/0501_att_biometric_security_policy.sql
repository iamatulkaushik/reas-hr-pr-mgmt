/* 0501_att_biometric_security_policy.sql
   Row-level tenant filter on the biometric tables (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent.
   The agent's own login reaches these tables only through usp_Punch_Ingest, called by Sigma's agent wrapper after it has set the company from the agent registration. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_Biometric')
    EXEC (N'CREATE SECURITY POLICY att.TenantPolicy_Biometric
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON att.Device,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON att.RfidCard,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON att.Punch
            WITH (STATE = ON)');
GO
