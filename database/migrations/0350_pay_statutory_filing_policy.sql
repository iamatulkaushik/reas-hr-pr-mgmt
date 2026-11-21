/* 0350_pay_statutory_filing_policy.sql
   Row-level tenant filter on pay.StatutoryFiling (ADR 0007), using sec.fn_TenantPredicate from 0005. Idempotent. Needs 0349. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_StatutoryFiling')
    EXEC (N'CREATE SECURITY POLICY pay.TenantPolicy_StatutoryFiling
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.StatutoryFiling
            WITH (STATE = ON)');
GO
