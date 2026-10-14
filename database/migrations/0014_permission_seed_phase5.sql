/* 0014_permission_seed_phase5.sql
   Permission grants decided in ADR 0012 and ADR 0013. Idempotent (inserts only what is missing). Add a new script to change them.
   Roles: 1 Developer, 2 Owner, 3 Associate, 4 Operator. Modules: 4 Payroll, 7 Biometric, 10 Security.
   Actions: 1 View, 2 Create, 3 Edit, 5 Approve, 7 Reopen.
   - Owner may Reopen Payroll (a locked run reopens only with a written reason).
   - Associate may Approve Payroll as the second approver. The approver is never the person who submitted (ADR 0009).
   - Associate may Create and Edit Biometric (device, location and RFID card mapping).
   - Associate may View and Create in Security: create companies and Owner logins for the companies the Associate works on (ADR 0013). */
SET XACT_ABORT ON;
GO
INSERT sec.RolePermission (RoleID, ModuleID, ActionID)
SELECT v.RoleID, v.ModuleID, v.ActionID
FROM (VALUES (2, 4, 7), (3, 4, 5), (3, 7, 2), (3, 7, 3), (3, 10, 1), (3, 10, 2)) v (RoleID, ModuleID, ActionID)
WHERE NOT EXISTS (SELECT 1 FROM sec.RolePermission p WHERE p.RoleID = v.RoleID AND p.ModuleID = v.ModuleID AND p.ActionID = v.ActionID);
GO
