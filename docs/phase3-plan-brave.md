# Phase 3 plan (Brave): masters and employees, encrypted IDs

## Done on branch feature/employees-id-validation
- Revolution.Modules.Employees: Aadhaar (Verhoeff), PAN, IFSC, UAN, ESI IP, mobile, bank account validators. SensitiveIdMask. EmployeeCode. EmployeeValidator. PayType. EmployeeDraft.
- Revolution.Modules.Employees.Tests: 59 cases (xunit).
- Module build: 0 warnings, 0 errors.
- Tests: the same test files ran 59 passed, 0 failed through a throwaway runner. Real xunit run is not done. NuGet is blocked here. Needs Windows CI.

## Done on branch feature/attendance-data (database lane 0100-0199)
- 0100: mst.Factory, Department, Designation, Bank, Employee, Nominee. Unique (CompanyID, EmpCode). Age check 14 years at join. Sensitive ids are ciphertext columns. Blind-index columns with unique keys per company for Aadhaar and PAN. Nominee has a composite key to the employee.
- 0101: tenant security policy on all six tables (ADR 0007).
- 0102: procedures. Department and designation save and list. Employee next code, list (no ids), get, save, set status. Nominee replace (JSON, shares must total 100).
- 0103: mst.State (36 states and union territories, GST codes, seeded) and mst.Factory.StateID. Needed for professional tax by state in Phase 5. No district master: nothing in the plan uses it.
- 0203 (attendance lane): real composite foreign key from att.AttendanceDaily to mst.Employee. Closes the open item from ADR 0008.
- CR 0010 filed: IFieldCrypto (encrypt, decrypt, blind index). Replaces CR 0006.
- Check done: all batches parse as T-SQL in sqlglot. That parser is loose on procedure bodies, so this is a weak check. Not run on SQL Server. Needs Windows CI.

## Blocked, waiting on Sigma
| Item | Waiting for |
|---|---|
| EmployeeRepository and EmployeeService (encrypt, decrypt, blind index) | CR 0010 (IFieldCrypto) |
| Employee and master forms | CR 0004 status, BaseForm now exists |

## Next, once unblocked
1. EmployeeRepository (module, Sql, stored procedures) and EmployeeService (validate, encrypt, audit, OperationResult).
2. frmEmployee list and edit with masked ID fields.
3. Tests: tenant leak (needs SQL Server), duplicate code, under-age, masked display.
4. Department and designation service.

## Assumptions to confirm
- Minimum employment age 14 (Factories Act). Confirm with CA or labour consultant. The database also enforces it.
- ESI IP number is 10 digits. Employer code is the 17 digit one.
- Aadhaar check is format and checksum only. It does not prove issue.
- PAN holder type letters accepted: A B C F G H J L P T K.
- Mask logic lives in the module.
- Duplicate Aadhaar or PAN per company is blocked. UAN and ESI IP are not unique-keyed yet. Confirm.
- Bank, department, designation and factory are per company. State is shared reference data.
