/* 0034_permission_assets_expenses_owner_only.sql
   Change request 0028 (Owner's answers for Phase 9). Idempotent. Never edit after merge: add a new number.
   Roles: 2 Owner, 3 Associate, 4 Operator. Modules: 8 Assets, 9 Expenses. Actions: 1 View, 2 Create, 3 Edit, 4 Delete, 5 Approve, 6 Export.
   Assets and Recoveries are the Owner's only: the Owner has all six actions; the Associate and the Operator have none.
   Expenses are entered by the Operator and verified and approved by the Owner only: the Owner has all six actions (Approve included), the Operator has View and Create, the Associate has none.
   0004 (and nothing after it) seeded more than this for the Associate and the Operator, so the rows are removed here, and the Owner and Operator rows are asserted again. */
SET XACT_ABORT ON;
GO
DELETE FROM sec.RolePermission WHERE RoleID IN (3, 4) AND ModuleID = 8;
DELETE FROM sec.RolePermission WHERE RoleID = 3 AND ModuleID = 9;
DELETE FROM sec.RolePermission WHERE RoleID = 4 AND ModuleID = 9 AND ActionID NOT IN (1, 2);
GO
INSERT sec.RolePermission (RoleID, ModuleID, ActionID)
SELECT v.RoleID, v.ModuleID, v.ActionID
FROM (VALUES (2, 8, 1), (2, 8, 2), (2, 8, 3), (2, 8, 4), (2, 8, 5), (2, 8, 6),
             (2, 9, 1), (2, 9, 2), (2, 9, 3), (2, 9, 4), (2, 9, 5), (2, 9, 6),
             (4, 9, 1), (4, 9, 2)) v (RoleID, ModuleID, ActionID)
WHERE NOT EXISTS (SELECT 1 FROM sec.RolePermission p WHERE p.RoleID = v.RoleID AND p.ModuleID = v.ModuleID AND p.ActionID = v.ActionID);
GO
