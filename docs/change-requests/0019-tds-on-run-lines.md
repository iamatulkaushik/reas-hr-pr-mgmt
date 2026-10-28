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

- Sigma's decision (accepted / changed / rejected) and date: **Accepted with changes, 2026-10-28.** Brave can start 0323 and the calculator now.

**Brave's items 1 to 6: accepted, with these points.**
1. **0323 constraint.** The new check `NOT (IsHra = 1 AND (IsBasic = 1 OR IsDa = 1))` goes in its own batch (after `GO`, or through `EXEC`), because the column must exist before the constraint statement compiles. Existing rows are all 0, so it passes.
2. **Procedures.** `usp_SalaryComponent_Save` gets `@IsHra BIT = NULL` as the last parameter; NULL keeps the stored value (new row: 0), so a caller that does not pass it still works (same pattern as 0028). `usp_PayrollRun_SaveLines` reads `TdsAmount` as an optional field defaulting to 0.
3. **When the cap bites.** If the tax is limited to what is left of gross, the line must carry a warning saying so, and the unmet amount must stay visible in the next month's projection. In the last month of the year and in F&F, tax that cannot be taken is shown as a shortfall, never dropped silently. The CA confirms how a final-month shortfall is treated.
4. **Tax already deducted.** Same company, same financial year, every locked run including arrears runs (RunNo 2 and up). Tax deducted by a previous employer comes from the declaration, not from run lines.
5. **Plan gate.** `PlanFeature.Tds` as proposed. With the feature off the column stays 0 and the projection is not called.

**Sigma's items.**
1. **Reports: accepted, migration 0701.** `0700` has three procedures, not two: `rpt.usp_Payslip_Get`, `rpt.usp_WageRegister_Get` and `rpt.usp_PayrollSummary_Get`. 0701 replaces all three with `CREATE OR ALTER` and adds `TdsAmount` (summary: `SUM`). Sigma writes it once Brave's 0323 is merged, so it applies after the column exists.
2. **Layouts: accepted, narrowed.** Sigma updates the report layouts. Brave first adds `TdsAmount` to `PayslipRow` / `Payslip` and the repository mapping (module code). The plan gate lives in the app, not in the data: the service passes a `ShowTds` report parameter from `IEntitlementService`. The label is the plain text "Income tax (TDS)". A section number is printed only from a verified `GetTaxSectionRef` row, never from an unverified one.
3. **Payroll run screen and salary component screen: not Sigma's.** These are forms in `Revolution.Modules.Payroll.UI`, which Brave owns (ADR 0002). Neither screen exists there yet (only the export form). Sigma adds their menu positions in `MenuLayoutModule` when the screens exist; the start-up check flags a screen without a position.
4. **Callers of `RunLineResult`.** All three are Brave's (`PayrollRunCalculator`, `SqlPayrollRunRepository`, `RunFakes.vb`). Sigma has none to update.

**Owner questions (Sigma's recommendation, the Owner decides).**
1. HRA tick on the component: yes. A tick is exact; a code is fragile.
2. Carrying unpaid tax to later months: yes, with point 3 above.

**Order.** Brave: 0323, `RunLineResult`, `NetPayCalculator`, calculator hook, service, then `PayslipRow` mapping. Sigma: 0701 and layouts after 0323 is merged. Nothing is blocked before then.

