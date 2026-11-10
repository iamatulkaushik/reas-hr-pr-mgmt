# ADR 0025: Bulk import and multi-row entry

Status: accepted (Owner, 2026-11-10: "process them"). Owner: Sigma. First user: the Employees module (Multiple Employee, Bulk Import Employee); next: Attendance (Multiple, Import).

**One list, two ways to fill it.** "Multiple" and "Import" are the same screen, `BulkEntryForm` in `Revolution.UI.Common`: an editable list with the template's columns. Multiple starts with blank rows and takes typing or a paste from Excel. Import opens a file and puts its rows in the same list. The user corrects what is wrong in one place, so no second correction screen is needed. A module supplies only an `ImportTemplate` (its columns) and an `IBulkRowHandler` (check one row, key of one row, save one row).

**Shared code (Sigma).** Core: `ImportTemplate`, `ImportColumn`, `ImportTable`, `ImportRow`, `ImportReport`, `ImportLimits`, `ImportValue` (dates, numbers, yes/no, ids as people write them in India), `IBulkRowHandler`, `ITableImporter`, `BulkImportRunner`. Services: `ExcelTableImporter` (.xlsx, .xls, .csv, .tsv, with RevolutionAssociates.Excel). UI.Common: `BulkEntryForm`. Tests: `ImportValueTests`, `ImportTemplateTests`, `ExcelTableImporterTests`, `BulkImportRunnerTests`.

**Rules.**
1. **Row by row.** Rows that pass are saved; rows that fail stay in the list with a reason. One bad row never blocks the others. There is no all-or-nothing mode: each row goes through the module's own service (permission, validation, field encryption, audit), exactly as on the single screen, and a file-wide rollback would have to span the encryption and audit steps. A file can be loaded again after correction; rows already saved are refused as duplicates by the module's own duplicate checks.
2. **Check before Save.** Check saves nothing. Save asks for confirmation, then saves the rows that pass.
3. **Limits** (`ImportLimits`): 2,000 data rows, 60 columns and 5 MB per file. Larger lists are split.
4. **Every cell is text** when read. Numbers lose their trailing `.0` (a 12-digit Aadhaar typed as a number stays 12 digits); dates become `yyyy-mm-dd`. Modules read values only through `ImportValue`. Only the first sheet is read, only values (no formula is evaluated, no macro runs).
5. **Columns** are matched by name, ignoring order, case, spaces and punctuation; a template column may list aliases; extra columns in a file are ignored and listed; a missing required column, or two columns for one template column, stops the import with the column names.
6. **Same item twice in one list** (the handler's `RowKey`) is refused from the second time. The database's own duplicate checks stay the authority.
7. **Privacy.** The file reader, the runner and the handler never put a cell value in a message, a log or an exception text. A handler's message says which column is wrong, not what it held. An exception inside a handler becomes "The row could not be saved." / "The row could not be checked." Nothing writes a temporary file. The blank template and the rejected-rows file are written as Excel 97-2003 text cells (the existing `ITableExporter`). The rejected-rows file holds the same values as the user's own list, including ids; the screen tells the user to delete it when done.
8. **Permission and plan** are the module's: the same permission as the single screen creates a row (Create for new employees), and the plan feature of the module. The menu entry needs View on the module.
9. **Audit.** Each saved row is audited by the module's service as on the single screen. The module adds one batch entry after Save: how many rows were saved and rejected, no values.
10. **Create only in v1.** An import creates new items; it does not update existing ones. Updating by import (salary heads, bank details) is a separate decision.
