/* 0318_pay_leave_security_policy.sql
   Row-level tenant filter on the leave tables (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_PayLeave')
    EXEC (N'CREATE SECURITY POLICY pay.TenantPolicy_PayLeave
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.LeaveType,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.LeaveLedger
            WITH (STATE = ON)');
GO
