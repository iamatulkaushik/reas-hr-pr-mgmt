/* 0304_pay_run_security_policy.sql
   Row-level tenant filter on the payroll run tables (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_PayRun')
    EXEC (N'CREATE SECURITY POLICY pay.TenantPolicy_PayRun
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.PayrollRun,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.PayrollRunLine,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.PayrollRunEarning,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.PayrollRunEvent
            WITH (STATE = ON)');
GO
