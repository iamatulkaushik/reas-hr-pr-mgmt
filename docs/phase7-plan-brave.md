# Phase 7 plan (Brave): arrears and full and final

Date: 2026-10-26. Based on origin/main `055017a` plus the Phase 6 steps 5 and 6 zip. Bank files, ECR and the ESIC sheet are already built (PayrollFileService); what is left in Phase 7 for Brave is arrears and full and final (F&F).

## Owner decisions that fix the scope
Gratuity from the rule table, paid in F&F. Notice pay recovery in F&F. Leave types, accrual, carry-forward and encashment as the law allows. Bonus in Phase 7 (rule table 0407). After Approved a run is edited only through arrears (after Locked, only arrears). F&F status on the employee: 1 Open, 2 Calculated, 3 Approved, 4 Paid (mst.Employee.FnFStatusID). Exit reasons: 1 Resignation, 2 Retirement, 3 Death, 4 Disablement, 5 Termination, 6 Absconding, 7 Contract end, 8 Retrenchment.

## Order of work
| # | Step | Needs |
|---|---|---|
| 1 | `GratuityCalculator`. **Done 2026-10-26**, 15 tests | Rule row (`GetGratuityRule`) |
| 2 | `NoticePayCalculator`. **Done 2026-10-27**, 8 tests | Decisions A and B |
| 3 | `BonusCalculator`. **Done 2026-10-27**, 10 tests | Rule row 0407, decision C |
| 4 | `LeaveEncashmentCalculator` and `FnfDueWindow`. **Done 2026-10-27**, 8 tests. Leave ledger tables and procs **done 2026-10-27** (4b, migrations 0317 to 0319, not run on SQL Server); `LeaveService`, `SqlLeaveRepository` **done 2026-10-27** (4c), 12 tests | Decision D |
| 5 | F&F tables, procs and service: components, status Open to Paid, maker-checker. Tables and procs (0320 to 0322, not run on SQL Server) and `FnfCalculator`, `FnfTransition` **done 2026-10-27**, 10 tests; `FnfService`, `SqlFnfRepository` **done 2026-10-27**, 13 tests (step 5b). Not wired into `PayrollServices` and `PayrollModule` yet, to avoid merge clashes with unpushed Phase 6 work | 1 to 4 |
| 6 | F&F run integration: last month to the exit date, loan closure, final TDS | Phase 6 steps 5 and 6. `FnfAssembler` and `FnfAssemblyInput` **done 2026-10-27** (6a), 6 tests; `FnfTdsTrueUp` (exit-month projection and shortfall or refund) **done 2026-10-27** (6b), 8 tests; `FnfPlanner`, `FnfFacts`, `FnfPlan` **done 2026-10-29** (6c): one pure call from the facts to the lines, totals, due window and notes, 13 tests. 6d **done 2026-10-29**: `FnfService.Prepare` reads the facts (`IFnfFactsSource`, `SqlFnfFactsSource`, migration 0325 `usp_Fnf_Facts`), the bonus and gratuity rule rows, the leave ledger (each encashable type cut to its own limit) and the final tax true-up (the final run's tax counts as deducted; skipped with a note when no run covers the exit date), then calls `FnfPlanner`. Nothing is saved until the Owner confirms and `SaveLines` runs. 13 tests. `PayrollModule` wires it. Test screens **added 2026-10-30** (`PayrollRunForm`, `PayslipListForm`, `LeaveLedgerForm`, `LoansForm` and `TdsDeclarationForm` too, under Payroll, same cross-lane notes) at the Owner's request ("map prepared screens to the menu for testing"): `FnfForm` in `Revolution.Modules.Payroll.UI` (new file), offered by `PayrollShellModule`, placed by one `AddScreen` line in Sigma's `MenuLayoutModule` under Payroll, Full and final. Plain layout, code-built, not compiled or run here: Sigma reviews the cross-lane edits (two one-line additions) and restyles it. The final-month salary rule is the run's net pay with a typed override, still to be confirmed by the Owner. Needs Phase 6 step 5 (change request 0019): run lines do not store TDS yet, so tax already deducted cannot be read |
| 7 | Arrears: off-cycle run (RunNo 2 and up), difference per month, ECR arrear file (8 fields). 7a **done 2026-11-01**: `ArrearsCalculator` (due minus paid per wage month, EPF and ESI as contribution(due) minus contribution(paid), ceiling-safe), `ArrearsEcrRow`, `ArrearsEcrWriter` (eight fields, positive amounts only), 10 tests. 7b **done 2026-11-01**: `Section89ReliefCalculator` (Form 10E method, tax by delegate per year), `Section89Year`, `Section89Result`; employer-paid notice pay (`FnfPrepareOptions.EmployerEndedService`, `FnfFacts.EmployerEndedService`, `FnfAssemblyInput.NoticePayable`, paid as an Other earning line); last month's salary removed from the settlement (the regular run pays it; `FnfComponentKind.LastSalary` stays only because the database check allows it); `LeaveAccrualSetting` and `LeaveAccrualCalculator` (change request 0020 for the stored setting); bonus % at exit stays the Owner's choice (already an option). Not built (7c): off-cycle arrears run tables, service and screen, reading paid and due earnings per month, the tax step that applies the relief | Decisions E to G answered 2026-11-01 |

## Gratuity (step 1)
Days of wages for each counted year from the rule row (days, month divisor, minimum years, part-year limit, cap). Service runs from joining to the end of the exit date. Whole years decide eligibility. A part year counts as a full year for the amount when it is more than the limit (six months and a day yes, exactly six months no). A death or disablement waives the minimum years. The caller says which heads make the last wage (Basic plus DA) and whether the employee is fixed term. Rounded once, nearest rupee, then capped. Not decided in code: whether apprentices, trainees and casual staff are covered. The F&F service will treat them as not covered unless the Owner says otherwise (decision H).

## Decisions (Owner, 2026-10-27)
A. **Notice pay.** Notice days come from the separation record (NoticePeriodDays, per employee). Served days run from the notice date to the last working day, the last day marked as resigned. Wage is gross. Recovery is short days times the daily wage.
B. **Wage.** Gross. Applied to leave encashment as well until the Owner says otherwise.
C. **Bonus at exit.** Paid in F&F.
D. **Leave.** A minimal leave ledger first: type, accrued days, taken days, balance, carried forward.
E. **Arrears.** An option on the deduction side. The Owner decides in payments whether to apply it.
F. **Arrears tax.** Section 89 relief (Owner, 2026-11-01).
G. **Arrears contributions.** Rates of the wage month (Owner, 2026-11-01). The caller passes each month's rate rows in `ArrearsMonthInput`.
H. **Gratuity coverage.** Yes: permanent and fixed term only. Contract, casual, apprentice and trainee not covered.
I. **Final month.** F&F is settled in the month after exit, from the 1st to the 7th (`FnfDueWindow`). Salary to the exit date still on calendar days.

## Still open
1. Employer-paid notice pay: **built** 2026-11-01.
2. Arrears: off-cycle run tables, service and screen, and the tax step that applies section 89 relief (7c).
3. Leave accrual: calculator built; the stored company setting needs change request 0020 to Sigma. Encashment and carry-forward caps are now columns on `pay.LeaveType`, set by the Owner.
4a. No change request needed for a leave plan feature: leave is gated by the existing `PlanFeature.FullAndFinal`.
4b. The F&F procs update `mst.EmployeeSeparation.FnFStatusID` from the `pay` lane. Sigma to review.
5. Bonus percentage at exit: the Owner picks inside the rule's range, default is the minimum.

## Not in this phase
Leave module screens. Biometric and RFID (last phase). Official return file layouts (Phase 6, waiting for the CA).
