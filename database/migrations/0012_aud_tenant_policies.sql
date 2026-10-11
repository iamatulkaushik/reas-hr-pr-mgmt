/* 0012_aud_tenant_policies.sql
   Row-level tenant filter on aud.ErrorLog and aud.RevealLog (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_ErrorAndReveal')
    EXEC (N'CREATE SECURITY POLICY aud.TenantPolicy_ErrorAndReveal
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON aud.ErrorLog,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON aud.RevealLog
            WITH (STATE = ON)');
GO
