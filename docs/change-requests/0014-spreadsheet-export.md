# Change request 0014: Excel 97-2003 (.xls) export in Services

- Requested by: Brave
- Needed by module / phase: Phase 7 (ESIC monthly contribution sheet); later Phase 6 and 8 reports
- What is missing or wrong in the shared code: No Excel library or exporter exists in the solution. The ESIC template instructions say the file must be saved as Excel 97-2003 (.xls), every column as Text. The module builds the rows; nothing writes the file.
- Proposed signature or schema change: Add one NuGet package that writes `.xls` (BIFF). Sigma picks it; licence must allow redistribution. Services gets `ITableExporter.WriteXls(path As String, sheetName As String, table As IReadOnlyList(Of String())) As Void`. All cells Text. First row header. No formulas, no styling. Core and Data unchanged.
- Workaround until done: `EsicSheetBuilder.Build` returns the table as text cells. Tests check the table, not a file.
- Sigma's decision (accepted / changed / rejected) and date: pending
