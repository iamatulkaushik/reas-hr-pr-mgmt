/* 0211_att_monthly_security_policy.sql
   Row-level tenant filter on the two monthly attendance tables (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. Needs 0210. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_AttendanceMonthly')
    EXEC (N'CREATE SECURITY POLICY att.TenantPolicy_AttendanceMonthly
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON att.AttendanceMonthly,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON att.AttendanceSetting
            WITH (STATE = ON)');
GO
