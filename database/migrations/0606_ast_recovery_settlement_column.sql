/* 0606_ast_recovery_settlement_column.sql
   Phase 9 step 0601b: a recovery can also be taken on the full and final settlement. AppliedFnfID is the pay.FnfSettlement that holds it (StatusID 3), as AppliedRunID is the regular run.
   A recovery is held by a run or by a settlement, never both. Brave lane 0600-0699. Idempotent. Never edit after merge. */
SET XACT_ABORT ON;
GO
IF COL_LENGTH(N'ast.AssetRecovery', N'AppliedFnfID') IS NULL
    ALTER TABLE ast.AssetRecovery ADD AppliedFnfID INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_AssetRecovery_OneHolder' AND parent_object_id = OBJECT_ID(N'ast.AssetRecovery'))
    ALTER TABLE ast.AssetRecovery ADD CONSTRAINT CK_AssetRecovery_OneHolder CHECK (AppliedRunID IS NULL OR AppliedFnfID IS NULL);
GO
