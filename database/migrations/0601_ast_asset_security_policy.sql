/* 0601_ast_asset_security_policy.sql
   Row-level tenant filter on the asset tables (ADR 0007). Uses sec.fn_TenantPredicate from 0005. Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_Assets')
    EXEC (N'CREATE SECURITY POLICY ast.TenantPolicy_Assets
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON ast.AssetType,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON ast.Asset,
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON ast.AssetIssue
            WITH (STATE = ON)');
GO
