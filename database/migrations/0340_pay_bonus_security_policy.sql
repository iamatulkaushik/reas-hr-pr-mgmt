/* 0340_pay_bonus_security_policy.sql
   Row-level tenant filter on pay.BonusRun (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_PayBonusRun')
    EXEC (N'CREATE SECURITY POLICY pay.TenantPolicy_PayBonusRun
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.BonusRun
            WITH (STATE = ON)');
GO
