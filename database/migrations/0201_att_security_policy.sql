/* 0201_att_security_policy.sql
   Row-level tenant filter on every att table with a CompanyID (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_Attendance')
    EXEC (N'CREATE SECURITY POLICY att.TenantPolicy_Attendance
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON att.Shift,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON att.AttendanceDaily,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON att.MonthLock,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON att.MonthLockEvent
            WITH (STATE = ON)');
GO
