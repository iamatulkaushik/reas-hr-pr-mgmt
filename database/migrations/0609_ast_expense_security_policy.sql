/* 0609_ast_expense_security_policy.sql
   Row-level tenant filter on the expense tables (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_Expenses')
    EXEC (N'CREATE SECURITY POLICY ast.TenantPolicy_Expenses
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON ast.ExpenseCategory,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON ast.ExpenseClaim
            WITH (STATE = ON)');
GO
