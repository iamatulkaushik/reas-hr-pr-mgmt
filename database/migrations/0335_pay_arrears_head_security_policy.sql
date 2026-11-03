/* 0335_pay_arrears_head_security_policy.sql
   Row-level tenant filter on pay.ArrearsClaimHead (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_PayArrearsHead')
    EXEC (N'CREATE SECURITY POLICY pay.TenantPolicy_PayArrearsHead
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.ArrearsClaimHead
            WITH (STATE = ON)');
GO
