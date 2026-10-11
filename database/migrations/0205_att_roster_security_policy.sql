/* 0205_att_roster_security_policy.sql
   Tenant row filter on the 0204 tables (ADR 0007). Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_AttendanceRoster')
    EXEC (N'CREATE SECURITY POLICY att.TenantPolicy_AttendanceRoster
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON att.EmployeeShift,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON att.Holiday,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON att.WeeklyOffRule
            WITH (STATE = ON)');
GO
