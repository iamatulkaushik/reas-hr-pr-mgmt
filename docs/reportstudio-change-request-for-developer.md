# ReportStudio: changes needed for Revolution HRMS

From: Sigma. To: ReportStudio developer. Date: 2026-10-25.
Context: Revolution HRMS (VB.NET, WinForms, .NET 10, SQL Server) wants ReportStudio instead of Crystal Reports (ADR 0005).
Checked so far: the Core engine builds on .NET 10 and its 14 tests pass. Export, UI and Designer are not built yet.

## Must have (P1)

1. **Retarget to net10.0 and net10.0-windows.** Build everything on a Windows runner, fix compile errors (PDFsharp, colour editor), run all tests in CI. Warnings as errors.
2. **Host-supplied rows.** The HRMS loads data itself, through stored procedures and with the tenant session set. Add a data source provider `Host` with no query and no connection string.
   - Engine: `ReportEngine.Run(definition, parameters, rows)` (already close: `options.Rows`).
   - Viewer: `ViewerControl` takes a callback `Func<parameters, rows>`. Parameter dialog stays, rows come from the host.
3. **Split the database code out of Core.** Move `DataLoader` and the SqlClient, MySqlConnector and Npgsql references into an optional project, so Core and Export have no database packages.
4. **Safe files.** In `Host` mode ignore any query or connection string found in an `.rsr`, and never write one. Show a warning before running a file that has its own query.
5. **Page numbers per group.** Payslip runs need "Page 1 of 1" restarting for each employee (your roadmap item 4).
6. **Headless output.** `ReportRunner.ToPdf(definition, rows, parameters)` returning bytes (and the same for Excel), with no window, for bulk payslips and e-mail.

## Should have (P2)

7. **Field schema in the `.rsr`.** A list of field names and types, so the designer's field list works without a database (for example from a procedure's column list).
8. **Indian formats.** Digit grouping 12,34,567.00; dates dd-MM-yyyy; money as `decimal` rounded with `MidpointRounding.AwayFromZero`; a function for the amount in rupees and paise in words; Excel cells carrying the number format.
9. **Fonts.** Segoe UI by default; embed fonts in the PDF.
10. **Sample reports.** A payslip and a Form 16-style certificate as `.rsr` samples with fixed sample data and golden-image tests.

## Later (P3)

11. Charts, barcodes and QR, sub-reports and cross-tabs, zoom and rulers, drag between bands (your roadmap 3 to 5).

## How we will use it

- HRMS references the built assemblies, with a pinned version. `Revolution.Reports` (Brave) wraps them. Services return rows, never a window or a message box.
- Reports call stored procedures named `rpt.usp_*`, owned by Sigma. Report layouts only name fields.
- Any later change from our side comes as a numbered request like this one.

## Acceptance for P1

A Windows machine builds the solution with 0 warnings; tests pass; one payslip report runs from rows supplied by a test host, previews, prints and exports to PDF; a bulk run of 50 payslips produces 50 PDFs with page numbers restarting.
