# ADR 0001: Stack

Status: accepted. Owner: Sigma.

- VB.NET, WinForms, .NET 10 (net10.0-windows for UI projects, net10.0 for the rest).
- SQL Server **Standard** (SQL Agent jobs for backups, TDE available).
- ADO.NET with stored procedures only. No ORM, no inline SQL.
- Crystal Reports for printed output. See ADR 0005.
- Solution file is Revolution.slnx.
- Option Strict, Option Explicit and Option Infer are On for every project.
- Warnings are errors.
