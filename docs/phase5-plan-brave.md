# Phase 5 plan (Brave): payroll core

Date: 2026-10-13. Based on origin/main `2e8911a`. Reads `docs/phase5-plan-sigma.md`.

## Verified before starting
| Check | Result |
|---|---|
| Repo pulled, Sigma's Phase 5 start commit present | Yes |
| `scripts/check-guards.sh` | All guards passed |
| `Revolution.Core` build, warnings as errors | 0 warnings, 0 errors (.NET 10 SDK) |
| Employees, Attendance builds | Not built here. They need NuGet (SqlClient). Blocked in this environment. Windows CI must confirm. |
| Migrations on SQL Server, screens, leak test | Not run. Still open gates for Phases 3 and 4. |
| Open change requests from Brave | None. CR 0010 accepted. New CR 0011 filed below. |

## Done in this increment (lane: module code, no database)
`Revolution.Modules.Payroll` (references Core only) and `Revolution.Modules.Payroll.Tests`.
- `Rupee`: decimal percent and rounding (nearest, up, down). Midpoint away from zero. Rounding kind comes from the rate row.
- `EpfCalculator`: employee share, employer EPS, employer EPF, EDLI, admin. Employer total is rounded once, then EPS is taken out, so employee and employer totals match. Ceiling and percentages come from `EpfRate`. Optional contribution above the ceiling.
- `EsiCalculator`: ceiling, percentages, rounding from `EsiRate`. Daily wage exemption. `stayEligible` carries the contribution-period rule.
- `StatutoryWage`: Code on Wages floor (exclusions count only up to the allowed share), or the sum of flagged components when the rule is off.
- `PtCalculator`: slab lookup by gender and gross, special month amount. Monthly rules only.
- 41 test cases. Result: 41 passed, 0 failed, through a throwaway runner with an xunit stand-in, warnings as errors. A real xunit run needs Windows CI.
- Test rates are fixtures in `StatutoryTestRates`. No product rate is in code.

## Assumptions to confirm (CA)
1. Employee EPF on the capped wage unless the employee opts for actual wage.
2. EPS is 8.33% of the capped wage. Employer EPF is employer total minus EPS.
3. Admin charge per employee has no minimum. The 500 establishment minimum goes in the return (Phase 7).
4. ESI daily-wage exemption compares the employee's daily wage with the row value.
5. PT slab edges: both ends included. A gap between slabs is a failure, not a guess.
6. Expected values in the tests are arithmetic from the fixture rates. The CA signs them before the gate.

## Next increments
1. Salary structure (lane 0300): components with flags, templates, effective-dated employee salary, revisions. Tables, tenant policy per table in its own migration (guard 6), procedures, XACT_ABORT (guard 8).
2. Wage builder: sums flagged components for a month and feeds `StatutoryWage`.
3. Payroll run: Draft, Processed, Approved, Locked, Paid. Approval only when `IApprovalService.IsRequired`. Reads the locked attendance month.
4. Loans and EMI. Payslip data.
5. Repository and service on `IStatutoryRateService`. Any failure stops the run with its message.

## Blocked
| Item | Needs |
|---|---|
| PT for half-yearly and yearly states | CR 0011 |
| Payslip reports | ADR 0005 verdict |
| Any rate in a real run | CA sign-off on rows |
| Windows CI, SQL Server runs | Sigma and CI |

## Owner decisions (2026-10-13), used until ADR 0012 and its Owner annex are committed
- Pay basis follows designation: minimum wager is daily, regular post is monthly fixed. Stored as `mst.Employee.PayTypeID` (1 daily, 2 monthly).
- Pro-rata: calendar days for monthly, 26 for daily wage.
- Overtime 2x. Weekly-off and holiday work is overtime. OT in EPF wage and OT in ESI wage are two company options, default off.
- Wage exclusions: every component except basic and DA, then the Code on Wages floor.
- Net pay rounds to the nearest rupee. A negative net carries the shortfall to next month.
- Pay cycle: 26-day cycle, read as 26th to 25th. Stored as a company setting, with PeriodFrom and PeriodTo on each run. Open: confirm the reading, and how attendance month lock follows it (Sigma).
- Loans: interest per loan type, fixed or reducing, chosen per loan. EMI is capped at 25% of net pay (75% stays payable). Cap is a company setting.
- Declarations: Associate enters them. No employee self-service (closed offline system).
- Bank file: State Bank of India first. Need the SBI bulk payment file specification from the Owner.
- Run per company per month. Factory is a breakdown inside the run. Edits until Approved. After Locked, arrears only.
- Biometric and RFID: last development phase. Only a PDF and a user manual exist for the device.
- Plan: Owner wants biometric on Platinum only. Sigma's mapping says Gold. Needs a CR and Owner confirmation.
- Salary shown plain, role-gated.

## Increment 2 (2026-10-13): salary structure and wage builder
- Migrations 0300 (tables), 0301 (tenant policy), 0302 (procedures). Tables: `pay.CompanyPolicy`, `pay.SalaryComponent`, `pay.EmployeeSalary`, `pay.EmployeeSalaryLine`. Salary is effective-dated. A revision is a new header. Money `DECIMAL(14,2)`, percentages `DECIMAL(7,4)`.
- Salary amount is always the monthly figure. A daily-wage employee earns amount over the divisor (26) per paid day. This reads the Owner's "26 for daily wagers" as the pro-rata divisor.
- Error numbers 50301 to 50304 in lane 0300 (component not found, salary date exists, no lines, bad lines).
- Code: `WageBuilder` with `PayPolicy`, `PayPeriod`, `SalaryLineInput`, `EarnedLine`, `WageBreakdown`. Overtime base, EPF wage and ESI wage follow the Owner's options. 56 tests, 56 passed through the throwaway runner.
- Not run: migrations on SQL Server, the tenant leak test for the new `pay` tables (Sigma adds them to the test).

### More assumptions for the CA
7. Paid days: present 1, half day 0.5, weekly off, holiday and paid leave 1, absent 0. Weekly off for a daily-wage employee is paid unless the CA says otherwise.
8. Overtime hourly rate: ordinary daily rate over the shift's full-day hours. Ordinary daily rate is monthly ordinary wage over calendar days (monthly employee) or over 26 (daily wage).
9. Overtime is a wage exclusion under the Code on Wages rule, unless the company option puts it in EPF or ESI wage.
10. A run over a 26th-to-25th period needs every attendance month it touches to be closed.

## Increment 3 (2026-10-13): payroll run
- Migrations 0303 (tables), 0304 (tenant policy), 0305 (procedures). Tables: `pay.PayrollRun`, `pay.PayrollRunLine`, `pay.PayrollRunEarning`, append-only `pay.PayrollRunEvent` (status history on every plan, ADR 0012).
- Status: Draft, Processed, Submitted, Approved, Locked, Paid. Changes follow one table in `pay.usp_Run_ChangeStatus`. `RunStateMachine` in code mirrors it, with tests.
- Lines change only in Draft or Processed (`pay.usp_Run_ReplaceLines`). Processing needs every attendance month in the period closed (error 50318).
- Submit stores a totals checksum (`pay.fn_RunChecksum`, SHA-256 over employee, gross, deductions and net). Approve compares it. A mismatch throws 50315. The service then cancels the approval and returns the run to Draft (ADR 0012). Needs SQL Server 2017 or later (STRING_AGG).
- Reopening a Locked run to Draft needs a written reason of 10 or more characters. One regular run per company per period start. A period of more than 31 days is refused (50319).
- Error numbers 50311 to 50319.
- Code: `PayrollRunStatus`, `RunStateMachine`, `NetPayCalculator`. Net pay: gross less statutory, other deductions and carry-in. Below zero means net 0 and the shortfall carries forward. EMI is capped at the company share (25%) of pay before EMI, rounded down. The rest is deferred.
- 85 tests, 85 passed through the throwaway runner.
- Not run: migrations 0300 to 0305 on SQL Server (no instance here), tenant leak test for the new tables, approval service wiring (`PayrollRun` entity, summary template, at most 500 characters), audit names `PayrollRun.*`.


## Increment 4 (2026-10-13): loans and EMI
- Migrations 0306 (tables), 0307 (tenant policy), 0308 (procedures). Tables: `pay.LoanType`, `pay.Loan`, `pay.LoanInstallment`, `pay.LoanRecovery`. 0305 was amended (not merged yet): `pay.usp_Run_ReplaceLines` now refuses a run that still has loan recoveries (50324).
- Interest per loan type: flat or reducing balance, yearly rate, maximum installments. A loan copies method and rate at sanction.
- The application builds the schedule. `pay.usp_Loan_Create` checks that principal parts add up and that amount is principal plus interest.
- Recoveries are written when a run is Locked (`pay.usp_Recovery_Save`) and removed when it is reopened (`pay.usp_Recovery_ReverseRun`). An installment not fully recovered stays due. It is scheduled again in the next run (deferred EMI).
- Scheduled EMI for a run: everything outstanding with a due month up to the month of the period end. Recovery goes to the oldest month first.
- Error numbers 50321 to 50328.
- Code: `InterestMethod`, `LoanScheduleCalculator`, `LoanRecoveryAllocator`. 101 tests, 101 passed through the throwaway runner.
- Not built yet: part-prepayment, skipped months, foreclosure, interest waiver. The Owner has not asked for them in version 1.
- Not run: migrations 0306 to 0308 on SQL Server.

### Next
Run service: reads attendance, salary, policy, rates and open installments. Calls wage builder, EPF, ESI, PT, net pay. Saves lines. Submit, approve (approval service, `PayrollRun` entity), lock, apply recoveries. It needs the Data project and `AppServices` from Sigma, so it waits for Sigma's push. Until then: payslip data shape and payroll reports (`rpt` procedures, lane 0700).

## Increment 5 (2026-10-13): payroll report data
- Migration 0700: `rpt.usp_Payslip_Get`, `rpt.usp_WageRegister_Get`, `rpt.usp_PayrollSummary_Get`. Read only. Needs schema `rpt` from Sigma's 0014. Result-set columns are in `docs/reports-payroll-data.md`.
- Done after your answers: `OvertimeAmount` added to `pay.PayrollRunLine` (0303), the run procedures (0305) and both reports. The payslip returns UAN, ESI number and bank account as stored (ciphertext, last four, key versions). The application decrypts them, writes one `aud.usp_Reveal_Insert` entry per print job, and gives the helper a temporary file. Nothing is decrypted in SQL.
- Not run: 0700 on SQL Server.

## Increment 6 (2026-10-14): gratuity
- `GratuityCalculator`, `GratuityTerms`, `GratuityResult`. Wage (basic and DA) times days per year over the month divisor times counted years. All values from the rule row. 13 tests, 114 tests in all, 114 passed.
- Service runs from join date to exit date, both days included. Part year counts as a year only when it is in excess of the round-up months (more than six). Exactly six months does not round up.
- Minimum service must be met, except where the caller waives it (death or disablement). Regular and fixed-term minimums are separate rule values. Cap applied last.
- CR 0013 asks Sigma for the Core `GratuityRule` type and a read on `IStatutoryRateService`. Until then the calculator takes `GratuityTerms`.

### More assumptions for the CA
11. "In excess of six months" means more than six months, by date. Six months to the day does not round up.
12. Continuous service has no breaks. Unpaid leave and breaks in service are not deducted in version 1.
13. Last drawn wage is the monthly basic plus DA in force on the exit date.
14. Seasonal establishments and piece-rate workers are not covered.

### Waiting
ECR, ESI upload and SBI bank file builders need the official format files (Owner, evening).
