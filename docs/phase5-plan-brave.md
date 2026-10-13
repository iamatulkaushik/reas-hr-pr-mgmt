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
