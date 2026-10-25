# ADR 0005: Crystal Reports on .NET 10

Status: **open. ReportStudio proposed as the replacement for Crystal (see 2026-10-25 below); the Owner decides after a Windows spike.** Owner: Brave runs the spike, Sigma reviews.

Question: does the current SAP Crystal Reports runtime for .NET run inside a net10.0-windows WinForms app?

Spike pass criteria: a report with a parameter and a SQL data source previews, prints and exports to PDF from a net10.0-windows test app on a clean Windows machine.

If it fails: reports run in a small .NET Framework 4.8 (x86) helper process that the app launches with a data file. Record the verdict and SDK version here before Phase 5.

## Review of the first spike commit (Sigma, 2026-10-11)
`src/Revolution.ReportViewer` is in the solution as a .NET Framework 4.8 project with Crystal Reports 13.0.4003 packages. It is the fallback route of this ADR. As committed it is an empty Windows Forms template: `Form1` has no code, there is no report file, no parameter, no data source, no export, and nothing in the main app launches it. It proves nothing yet. No verdict can be recorded.

To finish the spike (Brave, then Sigma reviews):
1. Set the platform to x86 in the project (`PlatformTarget`), because the Crystal runtime is 32-bit. Check `TargetFramework` builds on `windows-latest` CI.
2. Confirm where the `CrystalReports.*` 13.0.4003 packages come from and that SAP's licence allows redistribution with the installer. They are not from SAP's own installer. If they are unofficial, use the SAP runtime installer instead and say so here.
3. Add one real report with one parameter and a SQL data source (a payslip stub is enough). Preview, print and export to PDF on a clean Windows machine.
4. Define how the main app calls it: separate process, a data file as input (as above), exit code and error text back. No database connection string in the command line.
5. Data access. The app login can only execute stored procedures (ADR 0004), so a report must read through a procedure or a view with an explicit grant. Decide which before Phase 5.
6. Record here: verdict, runtime version, package source, how it ships.

Brave: please do not change the project shape (target, packages) without Sigma's OK. If something blocks steps 1 to 3, write it up and wait.

## CI note (Sigma, 2026-10-14, approved by owner)
`Revolution.ReportViewer` (net481, Crystal COM references, SAP install path) cannot build on the `windows-latest` runner. CI skips it: `.github/workflows/ci.yml` builds and tests every other project one by one. It stays in `Revolution.slnx`, so build it locally where Crystal is installed. Revisit when the spike gives a verdict (a build without COM references could return to CI).

## Update 2026-10-24
Atul (Owner) is looking for a current, up-to-date alternative to Crystal Reports before the redistribution terms are chased. Decision pending. Until then: no new Crystal-specific work, and the report data procedures (`rpt.*`) stay as they are, because any report tool reads the same procedures. Brave's report code should stay behind the existing reporting interfaces so the tool can change.

## Proposal 2026-10-25: ReportStudio instead of Crystal (Sigma, for the Owner's decision)
Atul supplied ReportStudio (a stand-alone C# report designer, viewer and exporter; `.rsr` JSON layouts; PDF, Excel and Word output) as the alternative.

**What was checked.** Its Core engine (model, expressions, layout, sample data) compiles on .NET 10 and its 14 engine tests pass. Export, viewer and designer were not built (no package source in the review environment). Its own notes say it was never compiled on Windows. So this is a proposal, not a verdict.

**Why it fits.** The data path already decided in ADR 0012 is "the app runs the `rpt` procedure on its own tenant-safe connection and hands the report tool the rows". ReportStudio's engine takes pre-loaded rows, so the same path works and tenant isolation (SESSION_CONTEXT and row-level security) stays in our Data layer. It has no licence fee and no redistribution question, which is what ADR 0013 and ADR 0018 were still waiting on for Crystal. It has bands, groups, aggregates, page numbers, and PDF, Excel and Word export. It is a separate C# library; VB.NET calls it as any assembly.

**Conditions (the change list sent to its developer, `docs/reportstudio-change-request-for-developer.md`).**
1. Build on net10.0 and net10.0-windows, 0 warnings, tests in CI.
2. A `Host` data source: no query and no connection string in a layout when used by the HRMS. A layout names fields only. This keeps ADR 0001 (stored procedures only) and guard 1 (no SQL outside `database/migrations`).
3. Database drivers move out of Core into an optional project.
4. Page numbers restart per group (one payslip per employee).
5. Headless `ToPdf` and Excel output for bulk payslips and e-mail.
6. Indian number format, dd-MM-yyyy dates, amount in words, a field schema in the layout, Segoe UI, embedded fonts, sample payslip and certificate reports.

**Spike (replaces the Crystal spike; Brave, on a Windows machine, Sigma reviews).** Pass when: the solution builds with 0 warnings; one payslip layout runs from rows given by a test host; it previews, prints and exports to PDF; a bulk run of 50 payslips gives 50 PDFs with page numbers restarting; a Form 130 style certificate lays out on one page; the layout file holds no query or connection string. Record the verdict, the version and how it ships here.

**If it passes.** ReportStudio is the report tool. Then Sigma updates `README.md`, `CLAUDE.md`, `docs/PLAN.md` (stack line, Phase 0 gate and risks), ADR 0001, `docs/phase5-plan-sigma.md` and `docs/reports-payroll-data.md`; `Revolution.ReportViewer` (net481, Crystal COM) is removed from the solution and the CI note above is dropped. The `rpt.*` procedures do not change. Brave wraps it in `Revolution.Reports` behind the existing reporting interfaces.

**If it fails.** The question reopens: Crystal in the .NET Framework 4.8 helper (the fallback above) once the Owner has the redistribution terms in writing, or another tool.

**Until the Owner decides.** No new Crystal-specific work, and no ReportStudio code in the main solution. Nothing in this proposal changes ADR 0001, `CLAUDE.md` or the plan yet.
