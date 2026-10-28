# Change request 0019: Income tax (TDS) on the payroll run line

- Requested by: Brave
- Needed by module / phase: Phase 6 step 5 (monthly TDS in the run); Phase 7 step 6c (F&F final tax true-up reads the tax already deducted)
- What is missing or wrong in the shared code: a run line has no TDS. `pay.PayrollRunLine` has no tax column, `RunLineResult` has no tax property, `NetPayCalculator` does not take tax, and the payslip and register reports (`0700_rpt_payroll_procs.sql`) cannot show it. Without a stored TDS per locked line, the projection cannot know the tax already deducted this year, so the monthly amount and the F&F true-up would both be wrong. `pay.SalaryComponent` also has no HRA flag (`IsBasic` and `IsDa` exist), so the HRA received in a month cannot be read from a line.
- Workaround until done: `TdsProjectionService` stays standalone and the run takes no tax. F&F (6c) asks for "tax already deducted" as a typed amount on the screen.

## What Brave does (payroll lane, 03xx)
1. Migration `0323_pay_run_line_tds.sql`, idempotent: `ALTER TABLE pay.PayrollRunLine ADD TdsAmount DECIMAL(14,2) NOT NULL DEFAULT 0` and `ALTER TABLE pay.SalaryComponent ADD IsHra BIT NOT NULL DEFAULT 0`. Procs `usp_PayrollRun_SaveLines`, `usp_PayrollRun_ListLines` and `usp_SalaryComponent_*` replaced with `CREATE OR ALTER` in the same file, carrying the new column. Old migrations stay untouched.
2. `RunLineResult` gets `TdsAmount` as the last constructor argument and property. `TotalDeductions` includes it.
3. `NetPayCalculator`: tax joins the statutory deductions (EPF, ESI, PT), so it is taken before EMI and is never deferred. It is limited to what is left of gross after the other statutory deductions, so net pay never goes below zero. Anything not taken is picked up by later months, because the projection spreads the remaining year's tax over the remaining months.
4. `PayrollRunCalculator.Calculate` gets an optional `tdsForGross As Func(Of WageBreakdown, OperationResult(Of Decimal))`, called after the wage is built. It stays pure: the service supplies the function.
5. `PayrollRunService.Process`, only when `PlanFeature.Tds` is on: for each employee build the twelve `TdsMonthFacts` (earned months from locked lines, this month from the line being built, later months at this month's figures), sum `TdsAmount` of the year's locked lines as tax already deducted, load the declaration, call `TdsProjectionService.Project`, and use `TdsThisMonth`. A failed projection fails that employee's line with the message, as the other rules do.
6. Tests for each step, hand-worked.

## What Sigma does (shared and screens)
1. `0700_rpt_payroll_procs.sql`: add `TdsAmount` to the payslip and register result sets (new migration `0701`, not an edit of 0700).
2. Payslip and register layouts: a deduction line "Income tax (TDS)". The label comes from `IStatutoryRateService.GetTaxSectionRef("SALARY_TDS", year)` once CR 0018 rows are loaded; hide the line when the plan has no `Tds`.
3. Payroll run screen: a TDS column in the line grid and in the line detail, hidden when the plan has no `Tds`.
4. Salary component screen: an "HRA" tick box beside Basic and DA. Basic and DA stay exclusive with HRA (new check in 0323).
5. Anything that builds `RunLineResult` directly (tests, `RunFakes.vb`) takes the new last argument; Brave updates the Payroll tests, Sigma any others.

## Order
Brave 0323 and the calculator first. Sigma's reports and screens can follow in the same phase, since the column defaults to 0 and nothing breaks before then.

## Questions for the Owner
1. Is a separate HRA tick on salary components acceptable, or is HRA found by the component code "HRA"? A tick is exact; a code is fragile.
2. When pay is too low to take the full tax in one month, is carrying the rest to later months right? Yes is assumed above.

- Sigma's decision (accepted / changed / rejected) and date:
