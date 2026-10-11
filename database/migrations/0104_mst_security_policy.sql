/* 0104_mst_security_policy.sql
   Row-level tenant filter on every mst table that has a CompanyID (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent.
   mst.Company is left out on purpose: login reads the plan of one company before a session exists (mst.usp_Company_GetPlan).
   mst.State, mst.District, mst.Bank and mst.DocumentType are shared reference data with no CompanyID. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_Masters')
    EXEC (N'CREATE SECURITY POLICY mst.TenantPolicy_Masters
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.CompanyProfile,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.Factory,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.Department,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.Section,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.Grade,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.Designation,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.EmployeeCodeSeq,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.Employee,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.EmployeeStatutory,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.EmployeeBank,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.EmployeeAddress,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.EmployeeFamily,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.EmployeeNominee,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.EmployeeDocument,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON mst.EmployeeSeparation
            WITH (STATE = ON)');
GO
