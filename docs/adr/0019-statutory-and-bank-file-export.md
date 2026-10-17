# ADR 0019: Statutory and bank file exports

Status: accepted 2026-10-17. Owner: Sigma. Decides change requests 0014 and 0016. Written without a compiler or a database: nothing here has been run.

## Context
ECR, the ESIC upload sheet and bank files must contain plain UAN, ESI IP number and bank account numbers (ADR 0010 keeps them encrypted at rest). Revealing one employee at a time, with a separate Reveal grant, does not fit a 500-employee export.

## Rules
1. **Who.** Export permission on Statutory (ECR, ESIC) or on Payroll (bank file) lets that export decrypt the ids its file needs. No separate Reveal grant is needed. Reveal stays for showing one id on screen.
2. **What.** Only the fields the file needs (UAN for ECR; UAN and ESI IP number for ESIC; account number for a bank file), only for the employees of the run being exported, and only for a run that is approved or locked. The module checks the run state.
3. **Log first, then decrypt.** The module calls `aud.usp_Reveal_InsertBatch` (migration 0024) before it decrypts anything: one row per employee, field names only, reason `ECR export`, `ESIC export` or `Bank file`. If that call fails, nothing is decrypted and no file is made. Plain values are never logged.
4. **Plain values stay in memory.** They exist only inside the export call, in the table being written, and then in the file. No temporary files, no clipboard, no caching, no cell contents in any log or error message.
5. **The file goes where the person chooses**, through a save dialog. The screen warns that the file holds plain ids, to be sent or uploaded securely and deleted afterwards.
6. **One audit entry per export** through `IAuditService`: module, kind of file, run, number of rows, user, time and file name. Never the contents.
7. **Licence and plan.** An expired licence or trial denies Export already (ADR 0018). Bank files stay in the plan that includes them (ADR 0003).
8. **Excel 97-2003 (.xls)** is written by NPOI (Apache-2.0) behind `ITableExporter` (Core). All cells are text, sheets keep their order, the first row is the header. The implementation lives in Revolution.Services. The optional ESIC detail sheet is a **separate file**, so the upload workbook keeps exactly the two sheets the portal reads.

## Who builds what
- **Sigma:** `aud.usp_Reveal_InsertBatch` and the Reason column (migration 0024, written); `ExportSheet` and `ITableExporter` in Core (written); `NpoiTableExporter` in Services, after the package version is checked against NuGet's vulnerability audit (not yet written).
- **Brave:** `RevealForExport(employeeIds, fields, reason)` on the Employees module (log batch first, then decrypt), the real `IPayrollFileSource`, the export screens, and switching the Payroll module to Core's `ExportSheet` (delete `Revolution.Modules.Payroll/ExportSheet.vb` and add `Imports Revolution.Core` to `EsicDetailSheetBuilder`, `EsicWorkbookBuilder` and `FileMapperOptionTests`).

## Risks accepted
- Anyone with Export on Statutory or Payroll can obtain plain ids for a whole run. That is the nature of the file. The control is the permission, the batch log and the file audit, reviewed by the Owner.
- A saved file can be copied. Rule 5 is a warning, not a control.
