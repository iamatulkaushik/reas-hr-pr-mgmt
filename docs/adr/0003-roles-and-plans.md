# ADR 0003: Roles and plans

Status: accepted, details to confirm. Owner: Sigma.

Roles: Developer (hidden super-admin: diagnostics, plan and licence assignment), Owner (employer admin, final approver), Associate (prepares payroll and filings), Operator (data entry).

Plans: Copper, Silver, Gold, Platinum. Each plan includes everything in the plan below it.

Rule: no screen or service checks a plan name or role name directly. They ask IEntitlementService (plan features) and IPermissionService (role permissions). Both arrive in Phase 1.

Draft feature split: Copper = employees, attendance, basic payroll, payslips. Silver adds EPF/ESI, PT, loans, bank files. Gold adds TDS, Form 16/24Q, arrears, F&F, biometric, shift OT/LOP. Platinum adds dual approval, full audit trail, assets and expenses, multi-company.
