# Phase 3 plan (Brave): masters and employees, encrypted IDs

## Done on branch feature/employees-id-validation
- Revolution.Modules.Employees: Aadhaar (Verhoeff), PAN, IFSC, UAN, ESI IP, mobile, bank account validators. SensitiveIdMask. EmployeeCode. EmployeeValidator. PayType. EmployeeDraft.
- Revolution.Modules.Employees.Tests: 59 cases (xunit).
- Module build: 0 warnings, 0 errors.
- Tests: the same test files ran 59 passed, 0 failed through a throwaway runner. Real xunit run is not done. NuGet is blocked here. Needs Windows CI.

## Blocked, waiting on Sigma
| Item | Waiting for |
|---|---|
| Add projects to solution | CR 0007 |
| mst.* tables and stored procedures | CR 0003 (migration lane, procedure runner) |
| Encrypt and decrypt IDs | CR 0006 (IFieldCrypto) |
| Employee and master forms | CR 0004 (BaseForm, theme, controls) |
| Run screens with real permissions | CR 0002 (stubs) |

## Next, once unblocked
1. Migrations 0100-0199: states, districts, banks, company, factory, department, designation, employee, nominee. Unique (CompanyID, EmpCode).
2. Stored procedures `mst.usp_<Entity>_<Action>` with company filter.
3. EmployeeRepository, EmployeeService (validates, encrypts IDs, audits, returns OperationResult).
4. frmEmployee list and edit. Masked ID fields.
5. Tests: tenant leak, duplicate code, under-age, masked display.

## Assumptions to confirm
- Minimum employment age 14 (Factories Act). Confirm with CA or labour consultant.
- ESI IP number is 10 digits. Employer code is the 17 digit one.
- Aadhaar check is format and checksum only. It does not prove issue.
- PAN holder type letters accepted: A B C F G H J L P T K.
- Mask logic lives in the module. CR 0006 `IFieldCrypto.Mask` can drop.
