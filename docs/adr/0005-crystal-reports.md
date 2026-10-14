# ADR 0005: Crystal Reports on .NET 10

Status: **open. Spike started, not yet valid (see review below).** Owner: Brave runs the spike, Sigma reviews.

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
