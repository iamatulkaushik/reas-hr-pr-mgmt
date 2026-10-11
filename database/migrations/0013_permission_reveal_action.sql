/* 0013_permission_reveal_action.sql
   Adds permission action 8 (Reveal): show a protected id in clear (PAN, Aadhaar, UAN, ESI IP, bank account, document number).
   Every reveal is written to aud.RevealLog by the service. Seed: Owner and Associate may Reveal on Employees (module 2).
   Operator may not: data entry only. To change this, add a new migration. Idempotent. */
SET XACT_ABORT ON;
GO
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RolePermission_Action'
           AND definition NOT LIKE N'%8%')
    ALTER TABLE sec.RolePermission DROP CONSTRAINT CK_RolePermission_Action;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_RolePermission_Action')
    ALTER TABLE sec.RolePermission ADD CONSTRAINT CK_RolePermission_Action CHECK (ActionID BETWEEN 1 AND 8);
GO
INSERT sec.RolePermission (RoleID, ModuleID, ActionID)
SELECT v.RoleID, v.ModuleID, v.ActionID
FROM (VALUES (2, 2, 8), (3, 2, 8)) v (RoleID, ModuleID, ActionID)
WHERE NOT EXISTS (SELECT 1 FROM sec.RolePermission p WHERE p.RoleID = v.RoleID AND p.ModuleID = v.ModuleID AND p.ActionID = v.ActionID);
GO
