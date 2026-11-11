/* 0033_ast_schema.sql
   Change request 0026. Idempotent. Never edit after merge: add a new number.
   Schema ast: the Assets and Expenses modules (Phase 9, Platinum plan, PlanFeature.AssetsAndExpenses). Brave's tables, procedures and reports start at 0600 (lane 0600 to 0699),
   after this script.
   The app login gets EXECUTE on the schema like every other schema (0006): stored procedures only, no table access (ADR 0004). Every ast table that carries a CompanyID
   needs its own row-level security policy in the script that creates it (guard 6 and the policy test fail the build otherwise). Errors for ast: 53500 to 53599. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'ast') EXEC (N'CREATE SCHEMA ast');
GO
GRANT EXECUTE ON SCHEMA::ast TO hrms_app;
GO
