/* 0313_pay_tds_security_policy.sql
   Row-level tenant filter on the TDS tables (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_PayTds')
    EXEC (N'CREATE SECURITY POLICY pay.TenantPolicy_PayTds
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.TdsDeclaration,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.TdsDeclarationItem,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.TdsHraRent,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.TdsPrevEmployer,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.TdsMonthLine
            WITH (STATE = ON)');
GO
