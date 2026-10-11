/* 0101_mst_security_policy.sql
   Tenant row filter on the 0100 tables (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_Masters')
    EXEC (N'CREATE SECURITY POLICY mst.TenantPolicy_Masters
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.Factory,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.Department,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.Designation,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.Bank,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.Employee,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.Nominee
            WITH (STATE = ON)');
GO
