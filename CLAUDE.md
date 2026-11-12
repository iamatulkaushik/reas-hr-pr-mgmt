# CLAUDE.md: Revolution HRMS

Read `docs/adr/` and `docs/OWNERSHIP.md` before changing anything.

- VB.NET, WinForms, .NET 10, SQL Server Standard, stored procedures only, reports in ReportStudio (ADR 0005; Crystal Reports was dropped).
- Layering: UI -> Services -> Data -> Core. UI never touches Data. No MessageBox in Services or Data.
- Shared projects (Core, Data, Services, UI.Common, database/) are owned by Sigma. Do not edit them from a module task. Write a change request instead.
- Option Strict On. Decimal for money, rounded with MidpointRounding.AwayFromZero. Dates dd-MM-yyyy. Financial year 1 April to 31 March.
- Statutory rates and slabs come from effective-dated tables, never from code.
- Database: every change is a new numbered script in `database/migrations`; never edit a merged one (the runner checks checksums). The scripts must build an empty database in file order: never change a table or procedure in a script numbered before the script that creates it, and keep a patch in the last definition of a procedure (ADR 0024). Customer special builds (biometric, ADR 0021) keep their scripts in `database/special/<build>` with numbers from 0900. Every table with a company id needs a row-level security policy.
- Attendance is monthly (one row per employee per month) on every plan; daily attendance is the Platinum feature `DailyAttendance` (ADR 0026).
- Plans and roles: a feature belongs to the plan in `PlanFeatureMap` (Arrears is Gold, ADR 0023); role grants are seeded by migrations 0004 to 0032. Menu gates and service gates use the same `PlanFeature`.
- Database tests (`tests/Revolution.Database.Tests`) run against a real SQL Server in the CI job `database`; locally they do nothing unless `HRMS_TEST_CONNECTION` is set.
- No screen checks a role or plan name directly. Use IPermissionService and IEntitlementService.
- Icons: only `AppIcon` through `IconProvider` (ADR 0020, `docs/icons.md`). No image files, no codepoints in forms. No member for it means no icon: show text only. Need a new icon: change request.
- Definition of done: CI green, tests pass, screen actually run and checked, other developer reviewed.
- In forms and controls, write the type of every For Each variable (`For Each heading As String In ...`) and never call a variable text, top, left, name, size, tag, font or parent: VB ignores case, so it becomes the inherited property and fails with BC30039 or BC30369. Everywhere, including tests, a For Each variable without a type must not be named like a System namespace (text, io, linq, data, net, xml, security ...): it binds to the namespace and fails with BC30112. `scripts/check-guards.sh` checks the common names.
- One class per file. Methods up to 50 lines. No commented-out code.
