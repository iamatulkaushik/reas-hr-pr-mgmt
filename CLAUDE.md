# CLAUDE.md: Revolution HRMS

Read `docs/adr/` and `docs/OWNERSHIP.md` before changing anything.

- VB.NET, WinForms, .NET 10, SQL Server Standard, stored procedures only, Crystal Reports.
- Layering: UI -> Services -> Data -> Core. UI never touches Data. No MessageBox in Services or Data.
- Shared projects (Core, Data, Services, UI.Common, database/) are owned by Sigma. Do not edit them from a module task. Write a change request instead.
- Option Strict On. Decimal for money, rounded with MidpointRounding.AwayFromZero. Dates dd-MM-yyyy. Financial year 1 April to 31 March.
- Statutory rates and slabs come from effective-dated tables, never from code.
- No screen checks a role or plan name directly. Use IPermissionService and IEntitlementService.
- Definition of done: CI green, tests pass, screen actually run and checked, other developer reviewed.
- One class per file. Methods up to 50 lines. No commented-out code.
