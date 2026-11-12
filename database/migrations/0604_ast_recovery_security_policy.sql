/* 0604_ast_recovery_security_policy.sql
   Adds ast.AssetRecovery to the tenant policy created in 0601 (ADR 0007). Idempotent. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_predicates WHERE target_object_id = OBJECT_ID(N'ast.AssetRecovery'))
    EXEC (N'ALTER SECURITY POLICY ast.TenantPolicy_Assets
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON ast.AssetRecovery');
GO
