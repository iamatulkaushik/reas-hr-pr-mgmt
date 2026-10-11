/* 0008_permission_reopen_action.sql
   Adds permission action 7 (Reopen) for closed periods (CR 0009). Idempotent.
   Seed: Owner may Reopen Attendance. Associate and Operator may not.
   Widen the CHECK from 1..6 to 1..7, then insert the one grant if missing. */
SET XACT_ABORT ON;
GO
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RolePermission_Action'
           AND definition NOT LIKE N'%7%')
    ALTER TABLE sec.RolePermission DROP CONSTRAINT CK_RolePermission_Action;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RolePermission_Action')
    ALTER TABLE sec.RolePermission ADD CONSTRAINT CK_RolePermission_Action CHECK (ActionID BETWEEN 1 AND 7);
GO
IF NOT EXISTS (SELECT 1 FROM sec.RolePermission WHERE RoleID = 2 AND ModuleID = 3 AND ActionID = 7)
    INSERT sec.RolePermission (RoleID, ModuleID, ActionID) VALUES (2, 3, 7);
GO
