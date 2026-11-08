/* 0032_permission_associate_security_edit.sql
   ADR 0022. Idempotent. Never edit after merge: add a new number.
   Roles: 1 Developer, 2 Owner, 3 Associate, 4 Operator. Module 10 Security. Action 3 Edit.
   The Associate may Edit in Security: reset the temporary password of the Owner of a company the Associate works on. Developer and Owner already have Edit (0004). */
SET XACT_ABORT ON;
GO
INSERT sec.RolePermission (RoleID, ModuleID, ActionID)
SELECT 3, 10, 3
WHERE NOT EXISTS (SELECT 1 FROM sec.RolePermission WHERE RoleID = 3 AND ModuleID = 10 AND ActionID = 3);
GO
