/* 0004_seed_role_permissions.sql
   Default role x module x action grants. Idempotent (inserts only what is missing).
   Roles: 1 Developer, 2 Owner, 3 Associate, 4 Operator.
   Modules: 1 Masters, 2 Employees, 3 Attendance, 4 Payroll, 5 Statutory, 6 Reports, 7 Biometric, 8 Assets,
            9 Expenses, 10 Security, 11 Licence, 12 Audit.
   Actions: 1 View, 2 Create, 3 Edit, 4 Delete, 5 Approve, 6 Export.
   Change the matrix with a NEW script, never by editing this one. */
SET XACT_ABORT ON;
GO
;WITH spec (RoleID, ModuleID, Actions) AS
(
    SELECT RoleID, ModuleID, Actions FROM (VALUES
        -- Developer: licence, user setup, diagnostics. No employer data.
        (1, 10, '1,2,3'), (1, 11, '1,2,3'), (1, 12, '1,6'),
        -- Owner: everything for the employer except licence assignment; final approver.
        (2, 1, '1,2,3,4,5,6'), (2, 2, '1,2,3,4,5,6'), (2, 3, '1,2,3,4,5,6'), (2, 4, '1,2,3,4,5,6'),
        (2, 5, '1,2,3,4,5,6'), (2, 6, '1,6'), (2, 7, '1,2,3'), (2, 8, '1,2,3,4,5,6'), (2, 9, '1,2,3,4,5,6'),
        (2, 10, '1,2,3'), (2, 12, '1,6'),
        -- Associate: prepares payroll and filings. No approve, no delete, no security or audit.
        (3, 1, '1,2,3'), (3, 2, '1,2,3,6'), (3, 3, '1,2,3,6'), (3, 4, '1,2,3,6'), (3, 5, '1,2,3,6'),
        (3, 6, '1,6'), (3, 7, '1'), (3, 8, '1,2,3'), (3, 9, '1,2,3,6'),
        -- Operator: data entry only.
        (4, 1, '1'), (4, 2, '1,2,3'), (4, 3, '1,2,3'), (4, 8, '1'), (4, 9, '1,2')
    ) v (RoleID, ModuleID, Actions)
)
INSERT sec.RolePermission (RoleID, ModuleID, ActionID)
SELECT s.RoleID, s.ModuleID, CAST(x.value AS TINYINT)
FROM spec s
CROSS APPLY STRING_SPLIT(s.Actions, ',') x
WHERE NOT EXISTS (SELECT 1 FROM sec.RolePermission p
                  WHERE p.RoleID = s.RoleID AND p.ModuleID = s.ModuleID AND p.ActionID = CAST(x.value AS TINYINT));
GO
