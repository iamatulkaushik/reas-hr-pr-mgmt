# ADR 0008: Module data access, migration lanes, Reopen action

Status: accepted. Owner: Sigma. Resolves CR 0007, 0008, 0009.

- **Module repositories.** A feature module may hold its own `Sql<Name>Repository` and its interface inside the module project. It uses the public `IConnectionFactory` (tenant context already applied) and the public `StoredProcedure` helpers from Revolution.Data. Stored procedures only (ADR 0001); no inline SQL.
- **Layering still holds.** UI -> Services -> Data -> Core. A module's service takes its repository interface; forms never touch the repository.
- **Shared repositories stay Sigma's.** Security, audit, company and provisioning repositories remain in Core and Data.
- **Migration lanes.** Sigma 0001-0099. Brave, reviewed by Sigma: 0100-0199 masters and employees, 0200-0299 attendance, 0300-0399 payroll, 0400-0499 statutory and tax, 0500-0599 biometric, 0600-0699 assets and expenses. Every table with a CompanyID joins a tenant security policy in its own migration (ADR 0007). Never edit a merged script.
- **Reopen.** New `PermissionAction.Reopen` (7). `0008` widens the check constraint and grants Attendance Reopen to Owner only. A reopen needs a written reason (Brave's `MonthLockPolicy`) and an audit entry. Associates cannot reopen. To change that, add a new migration.
- **Solution file.** Brave may add `Revolution.Modules.*` project lines (and their tests) to Revolution.slnx without a change request. Guard 4 still fails a project that is missing.
