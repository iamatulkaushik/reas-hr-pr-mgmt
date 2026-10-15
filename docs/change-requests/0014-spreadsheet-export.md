# Change request 0014: Excel 97-2003 (.xls) export in Services

- Requested by: Brave
- Needed by module / phase: Phase 7 (ESIC workbook); later Phase 6 and 8 reports
- What is missing or wrong in the shared code: No Excel library or exporter exists in the solution. The ESIC template says the file must be Excel 97-2003 (.xls), every column as Text. The workbook has two sheets: the upload sheet (read by the portal) and a static instructions sheet. The module builds both as text tables; nothing writes the file.
- Proposed signature or schema change: Add one NuGet package that writes .xls (BIFF). Sigma picks it; licence must allow redistribution. Move `ExportSheet` (Name, Table As IReadOnlyList(Of String())) from the Payroll module to Core. Services gets `ITableExporter.WriteXls(path As String, sheets As IReadOnlyList(Of ExportSheet))`. Sheet order is kept. All cells Text. First row of each sheet is the header. No formulas, no styling. Data unchanged.
- Pending: whether the optional ESIC detail sheet ships as a third sheet or a separate file. Exporter must handle any number of sheets either way.
- Workaround until done: `EsicWorkbookBuilder.Build` returns the two sheets as text tables. Tests check the tables, not a file.
- Sigma's decision (accepted / changed / rejected) and date: pending
