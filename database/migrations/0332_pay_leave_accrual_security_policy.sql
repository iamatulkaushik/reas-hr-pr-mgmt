/* 0332_pay_leave_accrual_security_policy.sql
   Row-level tenant filter on the leave accrual tables (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_PayLeaveAccrual')
    EXEC (N'CREATE SECURITY POLICY pay.TenantPolicy_PayLeaveAccrual
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.LeaveAccrualSetting,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.LeaveAccrualRun
            WITH (STATE = ON)');
GO
