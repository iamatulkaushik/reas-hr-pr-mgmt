# CLAUDE.md: Revolution HRMS

Read `docs/adr/` and `docs/OWNERSHIP.md` before changing anything.

- VB.NET, WinForms, .NET 10, SQL Server Standard, stored procedures only, Crystal Reports.
- Layering: UI -> Services -> Data -> Core. UI never touches Data. No MessageBox in Services or Data.
- Shared projects (Core, Data, Services, UI.Common, database/) are owned by Sigma. Do not edit them from a module task. Write a change request instead.
- Option Strict On. Decimal for money, rounded with MidpointRounding.AwayFromZero. Dates dd-MM-yyyy. Financial year 1 April to 31 March.
- Statutory rates and slabs come from effective-dated tables, never from code.
- No screen checks a role or plan name directly. Use IPermissionService and IEntitlementService.
- Icons: only `AppIcon` through `IconProvider` (ADR 0020, `docs/icons.md`). No image files, no codepoints in forms. No member for it means no icon: show text only. Need a new icon: change request.
- Definition of done: CI green, tests pass, screen actually run and checked, other developer reviewed.
- In forms and controls, write the type of every For Each variable (`For Each heading As String In ...`) and never call a variable text, top, left, name, size, tag, font or parent: VB ignores case, so it becomes the inherited property and fails with BC30039 or BC30369. Everywhere, including tests, a For Each variable without a type must not be named like a System namespace (text, io, linq, data, net, xml, security ...): it binds to the namespace and fails with BC30112. `scripts/check-guards.sh` checks the common names.
- One class per file. Methods up to 50 lines. No commented-out code.
