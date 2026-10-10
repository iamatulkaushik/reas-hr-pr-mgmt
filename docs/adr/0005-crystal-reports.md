# ADR 0005: Crystal Reports on .NET 10

Status: **open, spike required in Phase 0.** Owner: Brave runs the spike, Sigma reviews.

Question: does the current SAP Crystal Reports runtime for .NET run inside a net10.0-windows WinForms app?

Spike pass criteria: a report with a parameter and a SQL data source previews, prints and exports to PDF from a net10.0-windows test app on a clean Windows machine.

If it fails: reports run in a small .NET Framework 4.8 (x86) helper process that the app launches with a data file. Record the verdict and SDK version here before Phase 5.
