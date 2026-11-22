# ADR 0028: ReportStudio in the HRMS

Status: accepted 2026-11-21 (Owner: the finished ReportStudio is ready, CI green, reports window working). Owner of this ADR: Sigma. Follows ADR 0005 (tool chosen) and ADR 0012 (data path).

## Decision
- **Source and pin.** ReportStudio comes from https://github.com/revolutionassociates/ReportStudio as a git submodule at `external/ReportStudio`, pinned to commit `d398950ed311907c0e40566a32167e9d1388a31c`. Moving the pin is a deliberate commit. ADR 0005 said "built assemblies at a pinned version"; a pinned source commit gives the same pin, brings its NuGet packages along, and keeps binaries out of git. If build time hurts, switch to a package feed.
- **Projects.** `Revolution.Reports` (net10.0: catalogue, `IReportDataSource`, `SqlReportDataSource`) and `Revolution.Reports.UI` (net10.0-windows: `ReportScreen`, `ReportsShellModule`). Only the UI project references ReportStudio (`ReportStudio.UI`, which brings Core and Export). Guard 11 enforces this and forbids `ReportStudio.Data` (database drivers), Designer, Web, Cli and Viewer.
- **Data.** Host-only. The data source runs an `rpt.*` procedure on the tenant-safe connection and hands the first result set to the viewer. `ViewerControl.HostOnly = True`, so a layout can never name a query, a connection or a file. No SQL outside `database/migrations` (guard 1).
- **Layouts.** `reports/layouts/*.rsr`, copied next to the app in `Reports/`. The Owner edits them in the ReportStudio Designer. Layouts print no form number or section number as text (ADR 0005, `docs/reportstudio-required-changes.md` item 6).
- **Parameters.** The screen, not the user, fills `CompanyName`, `PeriodText`, `MonthText`, `AsOnText`, `IsDraft` and `ShowTds`, and only for names the layout declares. DRAFT shows while the run is below Approved.
- **Fonts.** Ubuntu, copied by ReportStudio.Export (ADR 0005, Owner decision).
- **Removed.** `src/Revolution.ReportViewer` (Crystal helper), the stale `reports/ReportStudio` copy, the CI skip for the helper.

## Menu
Reports: Basic Report, Salary Sheet, Wage Register, Abstract, Muster Roll (new screens), Payslip (still the payslip list). Plan gates: Basic none, payroll reports BasicPayroll, Muster Attendance. View on `ModuleKey.Reports` is checked by the shell.

## Not done here
- Payslip printing is done: Print... on the payslip screen opens `ReportViewWindow` on `PayslipReportRows`; full ids go through `PayslipService`, which checks Export and logs the reveal. The headless bulk run (`ReportRunner.ToPdf`) waits for the 50-payslip check of Phase 9; the viewer exports the whole run to PDF meanwhile.
- An audit entry per report opened; role grants for `ModuleKey.Reports` must be checked on a real login.
- Muster Roll in daily-attendance mode (Platinum): the monthly table is empty there, the sheet shows blanks.
- Golden images and the 50-payslip run (Phase 9).
- Nothing here was compiled by Sigma (no Windows, no NuGet). CI and a Windows run decide.
