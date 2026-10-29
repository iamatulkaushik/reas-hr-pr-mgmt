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
| 6 | F&F run integration: last month to the exit date, loan closure, final TDS | Phase 6 steps 5 and 6. `FnfAssembler` and `FnfAssemblyInput` **done 2026-10-27** (6a), 6 tests; `FnfTdsTrueUp` (exit-month projection and shortfall or refund) **done 2026-10-27** (6b), 8 tests; `FnfPlanner`, `FnfFacts`, `FnfPlan` **done 2026-10-29** (6c): one pure call from the facts to the lines, totals, due window and notes, 13 tests. Reading the facts from the database (separation, final-month run line, loan balance, gratuity and bonus rule rows, leave balance, bonus months) and a screen-facing `Prepare` on `FnfService` still to do (6d). Needs Phase 6 step 5 (change request 0019): run lines do not store TDS yet, so tax already deducted cannot be read |
| 7 | Arrears: off-cycle run (RunNo 2 and up), difference per month, ECR arrear file (8 fields) | Decisions E to G |

## Gratuity (step 1)
Days of wages for each counted year from the rule row (days, month divisor, minimum years, part-year limit, cap). Service runs from joining to the end of the exit date. Whole years decide eligibility. A part year counts as a full year for the amount when it is more than the limit (six months and a day yes, exactly six months no). A death or disablement waives the minimum years. The caller says which heads make the last wage (Basic plus DA) and whether the employee is fixed term. Rounded once, nearest rupee, then capped. Not decided in code: whether apprentices, trainees and casual staff are covered. The F&F service will treat them as not covered unless the Owner says otherwise (decision H).

## Decisions (Owner, 2026-10-27)
A. **Notice pay.** Notice days come from the separation record (NoticePeriodDays, per employee). Served days run from the notice date to the last working day, the last day marked as resigned. Wage is gross. Recovery is short days times the daily wage.
B. **Wage.** Gross. Applied to leave encashment as well until the Owner says otherwise.
C. **Bonus at exit.** Paid in F&F.
D. **Leave.** A minimal leave ledger first: type, accrued days, taken days, balance, carried forward.
E. **Arrears.** An option on the deduction side. The Owner decides in payments whether to apply it.
F, G. **Arrears tax and contributions.** Not answered yet.
H. **Gratuity coverage.** Yes: permanent and fixed term only. Contract, casual, apprentice and trainee not covered.
I. **Final month.** F&F is settled in the month after exit, from the 1st to the 7th (`FnfDueWindow`). Salary to the exit date still on calendar days.

## Still open
1. Employer-paid notice pay when the employer ends service early. Not built.
2. Arrears tax relief (old section 89) and which month's rates apply to arrear contributions (F, G).
3. Leave accrual rule (days per days worked, adult and adolescent). Needs a change request to Sigma for the Core class and a 04xx migration. Encashment and carry-forward caps are now columns on `pay.LeaveType`, set by the Owner.
4a. No change request needed for a leave plan feature: leave is gated by the existing `PlanFeature.FullAndFinal`.
4b. The F&F procs update `mst.EmployeeSeparation.FnFStatusID` from the `pay` lane. Sigma to review.
5. Bonus percentage at exit: the Owner picks inside the rule's range, default is the minimum.

## Not in this phase
Leave module screens. Biometric and RFID (last phase). Official return file layouts (Phase 6, waiting for the CA).
