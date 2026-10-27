/* 0321_pay_fnf_security_policy.sql
   Row-level tenant filter on the F&F tables (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_PayFnf')
    EXEC (N'CREATE SECURITY POLICY pay.TenantPolicy_PayFnf
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.FnfSettlement,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.FnfComponent
            WITH (STATE = ON)');
GO
