# SQL intake: the earlier 19-script set (00 to 18)

Status: reference only. **Nothing here runs.** The files are stored as `.sql.txt` so guard 1 (SQL only in `database/migrations`) and the migration runner ignore them.
Rule applied: the accepted ADRs (0001 to 0009) win over the old design. Anything worth keeping is ported into a numbered migration in the right lane, by the owner shown below.

## Why the set cannot be used as the database

| # | Old design | Repo requirement | Effect if used as is |
|---|---|---|---|
| 1 | Two databases (HRMS_Master, HRMS_Live), a yearly archive database, cross-database synonyms, `DB_CHAINING OFF` | One database `HRMS`, schemas cfg sec mst att pay stat aud (ADR 0004) | EXECUTE-only app login cannot read the other database |
| 2 | Row-level security and maker-checker use `USER_NAME()`, `IS_MEMBER()`, `ORIGINAL_LOGIN()`, one SQL login per person | One app login. People are rows in `sec.User`; tenant comes from `SESSION_CONTEXT` (ADR 0004, 0007) | Every user looks like the app login. Operators see no rows. Approver always equals maker, so every approval throws |
| 3 | No `CompanyID` on any table. `hr.Employee.EmpCode` unique across everything | Every tenant table has `CompanyID`, unique per company, tenant policy (ADR 0007) | No multi-company, no tenant isolation |
| 4 | Scripts are run-once (`CREATE TABLE` without guards, `USE`, `$(Var)`, `CREATE DATABASE`) | `NNNN_name.sql`, idempotent, applied by `RevolutionSetup` (ADR 0004) | Second run fails; runner cannot pass sqlcmd variables |
| 5 | Roles are DB roles; one of them is `role_ca`; no Owner | Developer, Owner, Associate, Operator; permissions are rows in `sec.RolePermission` (ADR 0003, 0007) | Wrong role set, no plan gating |
| 6 | Maker-checker forced on every payroll run, leave and input | Only when the plan has `DualApproval` (Platinum), through `sec.ApprovalRequest` (ADR 0009) | Copper to Gold could never finish a run |
| 7 | UAN and ESI IP stored in clear (masked only). PAN, Aadhaar, bank encrypted inside SQL Server with a certificate | UAN, ESI IP, Aadhaar, PAN, bank are AES-256 encrypted **in the application**; TDE for files (ADR 0004) | Key lives in the database, UAN and ESI IP unprotected |
| 8 | Payroll maths in T-SQL functions and procedures | Decimal maths in VB services, `MidpointRounding.AwayFromZero`, tests first, CA known-answer tests (CLAUDE.md, PLAN risks) | Cannot be unit-tested in xunit |
| 9 | Attendance tables `att.*` and month lock, different shape | `att` tables 0200 to 0202 are merged: company-scoped, month lock with append-only history, Reopen action (ADR 0008) | Two `att.Shift`, two lock models |
| 10 | Express edition, Task Scheduler backups | SQL Server Standard, SQL Agent jobs (ADR 0001, 0004) | Backup plan wrong |

## What to keep, and whose part it is

Owners follow `docs/OWNERSHIP.md`. Sigma = shared code, `database/`, security. Brave = feature modules and their migration lane (Sigma reviews every migration).

### Sigma

| Old file | Keep | Becomes |
|---|---|---|
| 01 Config, FinancialYear | Key/value settings. FY is computed (1 April to 31 March), not a table | `cfg.Setting`; FY helper in Core |
| 01 StatutoryRate, PTSlab, LWFRate, MinimumWage, OTRule, GratuityRule, BonusRule, WageRule | Effective-dated rate tables, `Verified` flag, `SourceRef` | `stat.*` tables and read procedures (lane 0400). Rows only after CA sign-off |
| 03 ErrorLog | Error log written by catch blocks | `aud.ErrorLog` |
| 03 `usp_CreateAuditTrigger` | Row-level JSON change trail | Triggers on `stat.*` only (who changed a rate). App audit stays `IAuditService` |
| 03/11 `UserFactory` | Operator limited to some factories | `sec.UserFactory` keyed by UserID, plus a factory term in the tenant predicate. Needs your go-ahead (see Open) |
| 06 RuleChangeLog | Log of legal updates applied to rates | `stat.RuleChangeLog` |
| 18 RevealLog, Last4 columns, ciphertext bound to the employee | Log every reveal. Show only last 4. Bind ciphertext to company and employee | `aud.RevealLog`, new permission action `Reveal` (like Reopen), `IFieldCrypto` with AES-GCM and associated data = CompanyID + EmployeeID |
| 13 `usp_Backup`, `usp_IndexMaintenance`, BackupLog | Verified backups, logged; index upkeep | SQL Agent job steps, Phase 10 |
| 00 database options | RCSI, Query Store, page verify, snapshot isolation | `Revolution.Setup` create-database step, Phase 10 |
| 11 server audit spec | Failed logins, role changes | Phase 10 hardening |
| 14 YearEnd | Archive database per year, leave carry-forward | Option only. Needs its own ADR (conflicts with one database) |
| 16 test generator | 20,000 employee load test | Phase 10 load test |

### Brave

| Old file | Keep | Phase and lane |
|---|---|---|
| 02 Employee, Statutory, Bank, Address, Family, Nominee, Document, Separation | Column set, CHECKs (age 14, dates), unique UAN / ESI | Phase 3, lane 0100. IDs become encrypted columns plus a blind-index hash |
| 01 Factory (licence no and date, max workers, EPF/ESI sub code), Department, Section, Grade, Designation (Category), DocumentType | Richer master columns | Phase 3, lane 0100 |
| 04/08 PunchStaging, RawPunch, Device, ImportBatch, `usp_ImportPunches` | Import with duplicate skip, one punch per second | Phase 8 (`IPunchSource`), lane 0500 |
| 04/08 Roster, ShiftChange, WeeklyOffRule, Holiday | Needed by the Phase 4 engine; 0200 has none | Phase 4 follow-up, lane 0200 |
| 04/08 LeaveType, LeaveRequest, LeaveBalance, LeaveLedger, accrual (1 per 20 days worked), annual grant, regularization, comp-off | Leave module, not yet in PLAN | BACKLOG, lane 0200 |
| 08 `usp_ProcessAttendance` rules | Night shift window, missing-punch flag, late minutes after grace, OT only above full day, OT rounding | Spec for `AttendanceEngine` tests |
| 04/08 MonthlyAttendance | Locked month figures that payroll reads | Phase 5 input, lane 0300 |
| 05 SalaryComponent flags (EPF/ESI/OT/bonus/gratuity wage, Code-on-Wages exclusion), SalaryTemplate, SalaryRevision, EmployeeSalary (effective-dated) | Salary structure | Phase 5, lane 0300 |
| 05 PayrollRun, Detail, DetailLine, Input, Hold, Payslip | Run model; status Draft, Processed, Approved, Locked, Paid | Phase 5. Approval through `IApprovalService` |
| 05 Advance, AdvanceRecovery, Loan | Loans and EMI (Silver) | Phase 5 |
| 05 Arrear, FnF, FnFLine, Gratuity, LeaveEncashment | Arrears and F&F (Gold) | Phase 7 |
| 05 BankAdvice, BankAdviceLine | Bank file (Silver) | Phase 5 / 7 |
| 06 EPF/ESI/PT contribution and return tables, Challan, ChallanLine, FilingCalendar, FilingStatus | Statutory output | Phase 5 / 7, lane 0400 |
| 06 TDSProjection, TDSDeduction, Form16 | Tax | Phase 6, lane 0400 |
| 09 payroll rules | Pro-rata rounding, advance recovery never makes net negative, ESI contribution-period rule (stays eligible to period end), lock steps, F&F parts, bonus formula | Spec for Phase 5 services and tests |
| 07 functions and 17 test vectors | EPF 12 / 3.67 / 8.33 / 0.5 / 0.5 on 15,000; ESI 0.75 / 3.25 on 21,000 rounded up; gratuity 15/26; Code-on-Wages 50% | Known-answer cases for xunit and the CA check. Not runtime code |
| 10 `usp_ExportECR`, `usp_ExportESI`, `usp_CreateChallans` | ECR line layout (`#~#`), ESI upload columns, challan heads | Phase 7 |
| 12 views | Payslip header and lines, salary register, statements | Phase 5 reports. Open: Crystal reads views or procedures (app login is EXECUTE only) |

### Extras not in PLAN (park in BACKLOG, Brave)

LWF, minimum-wage check, bonus, factory registers (weekly hours, overtime quarter, accident), adolescent certificate and medical, contractor and establishment licences, inspection notices, training and skills, promotions, increments, transfers, disciplinary cases, rehire, journal entries, reimbursements.

## Open decisions (defaults follow the ADRs; say if you want one changed)

1. **Roles.** Old set has a CA role. ADR 0003 has Owner. Default: no CA role; the CA reviews as Associate or outside the app.
2. **Factory scope.** Old design limits Operators to factories. Default: add it after Phase 3 as `sec.UserFactory` by UserID.
3. **Year end.** Archive database per year or one database with FY columns. Default: one database, decide in Phase 10.
4. **Reports.** Views for Crystal or stored procedures only. Default: procedures; decide in Phase 5.
5. **Rates and rules.** Every rate, ceiling, cap, slab and the Code-on-Wages and labour-code dates in the old seed are unverified. Default: load as `Verified = 0` and block payroll gate until the CA signs.

## Notes on 08 (attendance procedures) and 09 (payroll procedures)

Use as a specification for Brave's services and tests. Do not port the T-SQL.

### Rules worth keeping
- Import skips duplicate punches. Re-processing never overwrites a manual or regularised day (`Source = 'Device'` only).
- Night shift looks for punches from noon on the work date to noon next day.
- One punch gives a missing-punch flag, not a guess.
- Late minutes count only after the grace period.
- Overtime only above full day plus the shift's OT-after minutes. On a weekly off or holiday all worked minutes count. Under 30 minutes counts as nothing, then rounds down to a step.
- Leave accrual is idempotent by ledger reference. Leave is approved only if the paid balance covers it.
- Payroll reads only a locked month. Components are pro-rated by paid days. Advance recovery is capped so net pay never goes below zero.
- ESI: an employee who was eligible at the start of the contribution period (Apr to Sep, Oct to Mar) stays eligible to its end, tested on fixed ESI wages, not gross.
- Lock posts recoveries, marks arrears and overtime as paid, builds bank advice, payslips and journal.

### Differences from Brave's merged Phase 4 engine (Brave's engine stays authoritative)
| Old 08 | Phase 4 (merged) |
|---|---|
| Break comes off whenever span exceeds break minutes | Break comes off only past a per-shift limit |
| OT rounding and minimum from a factory rule | OT step per shift (1, 15, 30, 60) |
| Lock allowed any time once a summary exists | Lock only after month end and every day marked |
| Reopen needs only a reason text | Reopen needs 10+ characters, Owner only, append-only history (ADR 0008) |
| Regularised leave day always paid | Paid or unpaid follows the leave type |

### Defects not to copy
- `payroll.usp_LockPayroll` runs `compliance.usp_GenerateAll` after COMMIT. If it fails the run stays Locked with no statutory rows. Generate inside the same transaction.
- `payroll.usp_ComputeGratuity` inserts a new row on every call, and F&F calls it. Repeat F&F gives duplicate gratuity rows.
- `payroll.usp_ReopenPayroll` has no permission check, no reason length, no audit entry.
- `usp_ApprovePayroll`, `usp_ApproveLeave`, `usp_ApproveRegularization` compare logins. They must use user ids and the approval hook (Platinum only).
- No TDS calculation exists. `compliance.TDSProjection` is a table with no procedure; Phase 6 starts from zero.
- Rounding to whole rupees per component and the F&F divisors (leave encashment on wage / 26, notice recovery on gross / 30) are choices, not rules. Confirm with the CA before they become tests.
- Bonus uses the 8.33% minimum, a 21,000 eligibility ceiling and a 7,000 or minimum-wage calculation ceiling. Treat as unverified.

## Status after the first port (ADR 0010)
Done: shared tables (0010 to 0013), masters and employee core (0100 to 0108), attendance employee key (0203), statutory rate tables and readers (0400, 0401, empty).
Still to port, by owner: see the Brave table above (leave, rosters, salary structure, payroll run, loans, F&F, contributions, challans, ECR, TDS procedures).
