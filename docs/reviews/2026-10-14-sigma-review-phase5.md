# Sigma review of Brave's Phase 5 pull (commit 797d950), 2026-10-14

Scope: payroll calculators in `Revolution.Modules.Payroll`, migrations 0300 to 0308 and 0700. Static review only: nothing was run (no SQL Server, no Windows, no screens). The Phase 5 gate stays open.

## Approved as written
- `EpfCalculator`: rates and ceiling come from the rate row. On a 15,000 wage it gives employee 1,800, EPS 1,250 (1,249.50 rounded away from zero), employer EPF 550. Employer total is rounded once and EPS taken out, so the two sides always match.
- `EsiCalculator`: ceiling, percentages, rounding kind and daily-wage exemption all from the row; the stay-eligible rule is a parameter.
- `NetPayCalculator`: shortfall carries forward, whole EMI deferred, EMI cap rounded down. Decimal only.
- `RunStateMachine` matches the table inside `pay.usp_Run_ChangeStatus` (2>1,3,4; 3>4,1; 4>5; 5>6,1) and the 10-character reason for reopening a locked run.
- Every Phase 5 migration sets `XACT_ABORT`, carries `CompanyID`, and each table group has its own tenant policy migration (0301, 0304, 0307). Guards pass.

## Findings for Brave (shared code is not touched)
1. **PtCalculator still rejects non-monthly states.** CR 0011 is delivered (`PtRule.IsDue`, migration 0402). Change it to: return 0 when `rule.IsDue(month)` is False; otherwise the slab amount, or `SpecialMonthTax` if that month is the special month. Remove the "not monthly" failure and add tests for a half-yearly and a yearly rule.
2. **Split months (EPF ceiling).** ADR 0013 says the ceiling changes mid-September 2026. `EpfCalculator` takes one rate row for the month. Decide with the CA which row governs a split month (for example the row in force on the last day of the wage period) and write that rule in the payroll ADR before coding. Do not decide it inside the calculator.
3. **Maker and checker in `pay.usp_Run_ChangeStatus`.** The rule "the approver is never the person who submitted" is not enforced in the database. The procedure takes `@UserID` as a parameter and does not compare it with the user who submitted. Read the approving user from `SESSION_CONTEXT(N'UserID')`, as the Associate procedures do (ADR 0014), and refuse 3 to 4 when it equals the `UserID` of the Submitted event (new error number in the 03xx lane).
4. **Skipping approval (2 to 4).** The comment says it is allowed only when the plan needs no approval, but the procedure does not check that. Keep the service check with `IApprovalService.IsRequired`, and say in the procedure header that the service is the gate. If a database check is wanted, ask Sigma for a CR.
5. **Net pay and statutory deductions.** `NetPayCalculator` takes statutory deductions in full even when pay is negative. That is correct (they are due), but add a test that names this so nobody changes it by accident.

## Still missing in Phase 5 (nothing to review yet)
Repositories, payroll services and screens for salary structure, run, loans and payslip data. Reports wait for the ADR 0005 verdict.

## Sigma next
Module registration contract and the real shell menu with a switch-company item; licence design ADR before Phase 10; sign-off migration when the CA confirms the rates.
