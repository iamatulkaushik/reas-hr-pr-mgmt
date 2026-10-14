/* 0301_pay_security_policy.sql
   Row-level tenant filter on every pay table with a CompanyID (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent.
   Later pay tables get their own policy script, so a policy is never created in two scripts. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_PaySalary')
    EXEC (N'CREATE SECURITY POLICY pay.TenantPolicy_PaySalary
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.CompanyPolicy,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.SalaryComponent,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.EmployeeSalary,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.EmployeeSalaryLine
            WITH (STATE = ON)');
GO
