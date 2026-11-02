/* 0328_pay_arrears_security_policy.sql
   Row-level tenant filter on the arrears tables (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_PayArrears')
    EXEC (N'CREATE SECURITY POLICY pay.TenantPolicy_PayArrears
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.ArrearsClaim,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.ArrearsClaimMonth
            WITH (STATE = ON)');
GO
