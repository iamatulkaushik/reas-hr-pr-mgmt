/* 0307_pay_loan_security_policy.sql
   Row-level tenant filter on the loan tables (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_PayLoan')
    EXEC (N'CREATE SECURITY POLICY pay.TenantPolicy_PayLoan
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.LoanType,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.Loan,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.LoanInstallment,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON pay.LoanRecovery
            WITH (STATE = ON)');
GO
