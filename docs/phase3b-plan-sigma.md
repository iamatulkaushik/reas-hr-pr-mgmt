# Phase 3b: the Employee screens that are still planned (Sigma to Brave)

Date: 2026-11-10. Owner decision: build them. Five menu entries are still "Planned" in `MenuLayoutModule`: Multiple Employee, Bulk Import Employee, Education, Disable / Retire, Delete. This is the work order. Sigma has delivered the shared parts (ADR 0025); the screens, handler and migrations are Brave's.

## 1. What Sigma delivered
`Revolution.Core`: `ImportTemplate`, `ImportColumn`, `ImportTable`, `ImportRow`, `ImportReport`, `ImportLimits`, `ImportValue`, `IBulkRowHandler`, `ITableImporter`, `BulkImportRunner`. `Revolution.Services`: `ExcelTableImporter`. `Revolution.UI.Common`: `BulkEntryForm`. Read ADR 0025 first.

Not wired yet: `ModuleHost` does not expose the importer. Create `New ExcelTableImporter()` and `New ExcelTableExporter()` in `Revolution.App` and hand them to the shell module (the same way `employees.Export` is passed to Payroll); no change request needed.

## 2. Multiple Employee and Bulk Import Employee (`employee.add.multiple`, `employee.add.import`)
One template, one handler, two screens:
```
Dim form = New BulkEntryForm("Multiple Employee", template, handler, importer, exporter, startWithFile:=False, userName)
Dim form = New BulkEntryForm("Bulk Import Employee", template, handler, importer, exporter, startWithFile:=True, userName)
```
**Template "Employees"** (the required columns are what the single screen refuses to save without; check against `EmployeeValidator` and `PlacementValidator` and keep them in step):

| Column | Required | Aliases | What to enter |
|---|---|---|---|
| Full Name | Yes | Name, Employee Name | up to 100 characters |
| Date of Birth | Yes | DOB | dd/mm/yyyy; in the past; at least 14 years before joining |
| Date of Joining | Yes | Joining Date, DOJ | dd/mm/yyyy |
| Gender | Yes | Sex | Male, Female, Other (or M, F, O) |
| Employment Type | Yes | | Permanent, Contract, Fixed term, Casual, Apprentice, Trainee |
| Worker Type | Yes | | Worker, Staff, Manager |
| Pay Type | Yes | | Daily wage or Monthly salary |
| Unit | Yes | Factory, Branch | a unit code or name of this company |
| Father or Spouse Name | No | | up to the single screen's limit |
| Department, Section, Designation, Grade | No | | name or code from the company's masters |
| Marital Status | No | | Single, Married, Widowed, Divorced |
| Nationality | No | | default Indian |
| Mobile | No | Mobile No | 10 digits starting 6 to 9 |
| Email | No | | |
| PAN, Aadhaar, UAN, ESI IP No | No | | as the single screen validates them; spaces and hyphens allowed (`ImportValue.Compact`) |
| Bank Account, IFSC | No | | both or neither |

**Handler rules.**
- `Validate`: read values with `ImportValue`; look the masters up by name or code, case-insensitively, from lists loaded once per Check (not one query per row); build the same `EmployeeDraft` and `EmployeePlacement` as the single screen and run the same validators. Messages name the column ("Unit is not in your list.", "Date of Birth must be dd/mm/yyyy."), never a value.
- `RowKey`: the first of Aadhaar, PAN, UAN that is filled; otherwise name and date of birth.
- `Save`: call `EmployeeService.Create`. It checks permission, encrypts the ids, audits, and raises `EmployeeRuleException` for the database's duplicate rules; show that message on the row. A successful note is `Code <EmpCode>`; when the service returns an id warning, the row is saved and the note says the ids were not saved, without the ids.
- After Save, one batch audit entry (`BulkImport`, "n saved, m rejected").
- Salary is not part of this import: salary structures stay on their own screens (Phase 5).

Menu: add both entries to `EmployeesShellModule`; when pushed, Sigma changes the two `AddPlanned` lines in `MenuLayoutModule` to `AddScreen`. Permission Create on Employees, plan Employees.

## 3. Education (`employee.update.education`)
New table `mst.EmployeeEducation` (company, employee, qualification, institute, board or university, year of passing, result as text or percentage, sort order), many rows per employee, in the style of the nominees panel (`EmployeeNomineesPanel`): replace-all-for-the-employee save procedure and a get procedure. Row-level security policy on the table (guard 6). Permission Edit on Employees; audit entry `EducationSave`. Script number in the 01xx lane, after 0109 and after any script that touches the same table (ADR 0024).

## 4. Disable / Retire (`employee.retire`)
No new SQL: `EmployeeService.RecordExit` and `Reactivate` exist. Screen: find an employee, choose Left, Retired, Deceased or Absconded, an exit date (not before joining), confirm. For an employee who has left, show Reactivate. Show the database's refusal as it is (for example 51008 when the settlement is approved or paid). After an exit, offer to open Full and Final for that employee. Permission Edit on Employees.

## 5. Delete (`employee.delete`)
Owner only: the Delete permission on Employees is seeded for Owner and nobody else. New procedure `mst.usp_Employee_Delete` (employee id, row version): allowed only when nothing refers to the employee: no attendance, punches, payroll lines, loans, arrears, recoveries, separation or settlement. Otherwise it refuses with "This employee has history. Use Disable / Retire instead." When allowed it removes the employee's family, nominees, bank, ids, statutory flags and education in one transaction, then the employee, and the service writes an audit entry with the employee code (no values). The screen asks the user to type the employee code to confirm. Codes are not reused. The default is Retire; Delete exists for a mistaken entry.

## 6. Tests Brave adds
Handler: each validation message; masters by name and by code; a repeated key; the same template on the grid and from a file (round trip through `ExcelTableExporter.ToXls` and `ExcelTableImporter`); a save refused by a duplicate rule leaves the row in the list; the batch audit entry; no value in any message. Education: save and get, row-level security. Delete: allowed on a clean employee, refused with history, refused without the Delete permission. Retire: exit before joining refused, reactivate after an approved settlement refused. Add the new tables and procedures to the database tests (tenant isolation and EXECUTE-only access).

## 7. Order and gate
1. Wire the importer and exporter; Multiple and Import with the handler (one push).
2. Disable / Retire.
3. Education.
4. Delete.
Gate for each: CI green on both jobs, the screen run with one Owner and one Operator and screenshotted, and Sigma's review. The same `BulkEntryForm` will serve Attendance (Multiple and Import, planned entries 310 and 320) after this.

## 8. Decisions taken (Owner can change them)
Create only, no update by import. 2,000 rows and 5 MB per file. Row by row, never all or nothing. The blank template and the rejected-rows file are Excel 97-2003 (.xls); files to read may be .xlsx, .xls or .csv.
