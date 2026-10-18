/* 0008_permission_reopen_action.sql
   Aligns CK_RolePermission_Action with the seeded actions (1..8). Idempotent.
   Re-asserts the Owner's Reopen grant on module 3 (already seeded by 0004). */
SET XACT_ABORT ON;
GO
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RolePermission_Action')
    ALTER TABLE sec.RolePermission DROP CONSTRAINT CK_RolePermission_Action;
GO
ALTER TABLE sec.RolePermission ADD CONSTRAINT CK_RolePermission_Action CHECK (ActionID BETWEEN 1 AND 8);
GO
IF NOT EXISTS (SELECT 1 FROM sec.RolePermission WHERE RoleID = 2 AND ModuleID = 3 AND ActionID = 7)
    INSERT sec.RolePermission (RoleID, ModuleID, ActionID) VALUES (2, 3, 7);
GO
