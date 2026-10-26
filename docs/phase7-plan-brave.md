# Phase 7 plan (Brave): arrears and full and final

Date: 2026-10-26. Based on origin/main `055017a` plus the Phase 6 steps 5 and 6 zip. Bank files, ECR and the ESIC sheet are already built (PayrollFileService); what is left in Phase 7 for Brave is arrears and full and final (F&F).

## Owner decisions that fix the scope
Gratuity from the rule table, paid in F&F. Notice pay recovery in F&F. Leave types, accrual, carry-forward and encashment as the law allows. Bonus in Phase 7 (rule table 0407). After Approved a run is edited only through arrears (after Locked, only arrears). F&F status on the employee: 1 Open, 2 Calculated, 3 Approved, 4 Paid (mst.Employee.FnFStatusID). Exit reasons: 1 Resignation, 2 Retirement, 3 Death, 4 Disablement, 5 Termination, 6 Absconding, 7 Contract end, 8 Retrenchment.

## Order of work
| # | Step | Needs |
|---|---|---|
| 1 | `GratuityCalculator`. **Done 2026-10-26**, 15 tests | Rule row (`GetGratuityRule`) |
| 2 | Notice pay recovery calculator | Decisions A and B |
| 3 | Bonus calculator (pro rata at exit) | Rule row 0407, decision C |
| 4 | Leave encashment calculator | Decision D: no leave balance exists yet |
| 5 | F&F tables, procs and service: components, status Open to Paid, maker-checker | 1 to 4 |
| 6 | F&F run integration: last month to the exit date, loan closure, final TDS | Phase 6 steps 5 and 6 |
| 7 | Arrears: off-cycle run (RunNo 2 and up), difference per month, ECR arrear file (8 fields) | Decisions E to G |

## Gratuity (step 1)
Days of wages for each counted year from the rule row (days, month divisor, minimum years, part-year limit, cap). Service runs from joining to the end of the exit date. Whole years decide eligibility. A part year counts as a full year for the amount when it is more than the limit (six months and a day yes, exactly six months no). A death or disablement waives the minimum years. The caller says which heads make the last wage (Basic plus DA) and whether the employee is fixed term. Rounded once, nearest rupee, then capped. Not decided in code: whether apprentices, trainees and casual staff are covered. The F&F service will treat them as not covered unless the Owner says otherwise (decision H).

## Decisions needed
A. **Notice pay.** Days of notice come from where: the offer letter per employee, or one company rule? Recovery is notice days short times the daily wage (monthly wage divided by 26 for a monthly employee, the daily rate for a daily wager)? Does the employer also pay notice pay when it ends the service early?
B. **Which wage** for notice pay and leave encashment: Basic plus DA, or gross?
C. **Bonus at exit.** Paid in F&F for the part year worked if the employee meets the minimum days in the rule, on the same wage ceiling as the yearly bonus? Or only at the yearly run?
D. **Leave.** The Owner said leave balance is not built after F&F but is needed before it. Please confirm the order: a minimal leave ledger (type, accrued days, taken days, balance, carried forward) first, or F&F with leave encashment entered by hand for now?
E. **Arrears trigger.** A back-dated pay revision, an attendance correction after a lock, or both? Who may raise them?
F. **Arrears tax.** Paid in the month of the arrear run and taxed with that month's salary, with no relief under the old section 89, or does the CA want relief worked out?
G. **Arrears and EPF, ESI, PT.** Contributions on arrear wages at the rates of the month the arrear belongs to (as the ECR arrear file expects) or of the month paid?
H. **Who gets gratuity.** Permanent and fixed term only, with contract, casual, apprentice and trainee not covered?
I. **Final month.** Salary to the exit date on calendar days (as regular staff are prorated), and does a pending TDS shortfall or excess settle in the F&F month?

## Not in this phase
Leave module screens. Biometric and RFID (last phase). Official return file layouts (Phase 6, waiting for the CA).
