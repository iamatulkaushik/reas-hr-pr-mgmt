# ReportStudio: changes still required

Checked 2026-10-25 by Sigma on the updated drop. Engine verified (58 tests pass). These are what is left. Items 1 to 4 block "ok to join the solution".

## Blocking

1. **Windows build.** Export, UI, Designer, Viewer and Data were not built (no package source in the review). Run `dotnet build reports/ReportStudio/ReportStudio.sln -c Release -warnaserror` on Windows and fix what shows (PDFsharp 6.1.1, DocumentFormat.OpenXml 3.1.0, System.Drawing.Common, the colour editor).
2. **Fonts missing.** `fonts/` has only the README. The golden-image tests fail until `Ubuntu-Regular.ttf`, `Ubuntu-Bold.ttf`, `Ubuntu-Italic.ttf`, `Ubuntu-BoldItalic.ttf` and `UFL.txt` are added (Atul to download from design.ubuntu.com/font; the licence allows shipping them).
3. **Golden files not committed.** A missing golden file is created on the first run and the test passes, so today those tests check nothing. Run once on Windows with the fonts, review the text and PNG files, commit them, and only then count the tests.
4. **Spike pass criteria** from ADR 0005 (payslip and a certificate on a Windows machine, bulk run of 50 with page numbers restarting, no query or connection string in the layout file).

## Non-blocking

5. **`docs/DESIGN.md` is out of date.** It still says net8.0, "never compiled", connection strings stored in the file, and per-group page numbers as roadmap. Update it to the current state.
6. **Certificate layouts for the HRMS** must not carry form numbers or section references as text. The Form 16 sample says "Form 16", "Assessment year", "section 10", "Chapter VI-A", "section 87A" (1961 Act wording). The HRMS layouts take the form number and section labels as parameters (`{@FormNo}`) fed from `IStatutoryRateService.GetTaxFormNo` and `GetTaxSectionRef`.
7. **Packaging.** Decide how the HRMS references it (project reference to the staged source, or built assemblies at a pinned version). Version is `0.1.0`.
8. **Guard.** When it joins, Sigma adds a drift guard that no `Revolution.*` project references `ReportStudio.Data` or its database drivers.
9. **Fonts decision.** Reports use Ubuntu fonts (embedded in the PDF), not Segoe UI, because Segoe UI cannot be shipped. Owner to confirm.
