# Phase 9 plan (Brave): assets and expenses

Date: 2026-11-08. Based on origin/main `5b784bd`. Phase 9 is for the Platinum plan only (`PlanFeature.AssetsAndExpenses`). Permissions are already seeded (0004): Assets, Owner all, Associate View, Create and Edit, Operator View; Expenses, Owner all, Associate View, Create, Edit and Export, Operator View and Create. Migration lane 0600 to 0699, error numbers 56000 to 56999, report `Reports > Detailed > Assets and Recoveries` already in the menu as planned. There is no schema for them yet (change request 0026).

The plan text says nothing more about what these two modules do. The design below is Brave's proposal, written so each question can be answered with a word. The Owner answered on 2026-11-11 (next section); the follow-ups are answered too. Nothing is built until Sigma answers CR 0026 and CR 0028.

## Owner answers, 2026-11-11, and what they change
Where this section differs from the proposal below, this section wins.
| # | Owner answer | Design effect |
|---|---|---|
| 1 | Any item issued to an employee: laptop, any device. Assets and Recovery is Platinum, Owner only, not for Associates. | Asset types stay a master the Owner edits. Assets permissions change to Owner only (Associate and Operator get none). Needs CR 0028, because 0004 is Sigma's lane. |
| 2 | Recovery at depreciated value. | Each asset type carries a useful life; recovery = purchase value less depreciation to the loss date. Straight line, floor of zero (follow-up A). |
| 3 | Yes, the asset value must be covered. | Unreturned assets show on the full and final settlement as a deduction for the depreciated value. They also block the settlement until the Owner clears them (follow-up B). |
| 4 | Employees can claim, entered by the Operator, verified by the Owner only. | Operator creates. Owner verifies. Associate has no access to expenses (follow-up C). |
| 5 | Only the Owner approves claims. | One approver: the Owner. The existing maker and checker hook (`IApprovalService`) is used with Operator as maker and Owner as checker. |
| 6 | Paid separately, in cash or by bank. | Not through the payroll run. No reimbursement earning head. A claim is closed by a payment record: mode (cash or bank), date, reference. |
| 7 | Bills noted and stored in the database for future records. | Bill number, date, vendor and note are stored with the claim. Scanned files are not stored (follow-up D: a later Owner-only module, protected directory). |
| 8 | Limits as decided by the Owner. | Limits per category, per claim and per month, set by the Owner in settings. Claims over a limit are refused. |

Follow-up answers, 2026-11-11:
- A. Depreciation: yes. Straight line over a useful life in months set per asset type, floor of zero.
- B. Both: unreturned assets deduct their depreciated value on the full and final, and also block the settlement until the Owner clears them.
- C. Associates have no access to expenses.
- D. Details only (bill number, date, vendor, note). A scanned upload, if needed, is a later Owner-only module that stores files in a protected directory. Not part of Phase 9.

All questions are answered. Phase 9 now waits only for Sigma on CR 0026 and CR 0028.

## Assets (company items in the hands of employees)
- Asset types as a master (laptop, mobile, tools, uniform, safety shoes, helmet, ID card, SIM, key). An asset is one tagged item: tag number, type, description, serial number, purchase date and value, status (in stock, issued, returned, lost, damaged, written off).
- Issue to an employee with a date, the condition and an expected return date. Return with a date and the condition. Lost or damaged sets a recovery amount, decided by the Owner or the Associate.
- A recovery is taken from the employee through the regular payroll run as an other deduction, by the same mechanism overpaid arrears use (the recovery ledger: held at Process, released at Draft or Reopen), so pay never goes below zero and the rest carries forward.
- Full and final: unreturned assets show on the settlement as a deduction line, as the plan's report "Assets and Recoveries" implies.
- Report: assets by employee, assets not returned, recoveries pending and recovered.

## Expenses (employee claims)
- Categories as a master (travel, food, medical, mobile and internet, stationery, other), each with an optional limit per claim or per month.
- A claim: employee, date of expense, category, amount, a short description. An Operator (the employee's own login) can create and view; the Associate can edit and export; the Owner approves and rejects. Status: submitted, approved, rejected, paid. Approval goes through the existing maker and checker hook (`IApprovalService`).
- Payment: an approved claim is paid as a reimbursement line in the next regular payroll run (a non-taxable earning head, not part of the EPF or ESI wage), so it appears on the payslip.
- Export for the Associate: approved and paid claims by month.

## Questions for the Owner
1. Assets: what is issued (laptop, mobile, tools, uniform, safety items, ID card)? Do you track the purchase value of each item?
2. When an item is lost or damaged, is the recovery the full value, a depreciated value, or an amount the Owner types each time?
3. Should unreturned assets stop the full and final settlement or only appear on it as a deduction?
4. Expenses: do employees claim themselves from their own login, or does the Associate enter claims on their behalf?
5. Who approves: the Owner only, or the Associate first and the Owner after?
6. Are claims reimbursed through the payroll run as a non-taxable payslip line, or paid separately in cash or by bank?
7. Are bills or receipts to be attached (this needs a place to keep files) or only noted by number?
8. Are there limits per category, per claim or per month?

## Steps once answered
| Step | What |
|---|---|
| 0600 | Schema (Sigma, change request 0026), tables and procedures for asset types, assets, issue and return |
| 0601 | Asset recovery through the regular run and on the full and final settlement |
| 0602 | Expense categories, claims, approval, payment through the regular run |
| 0603 | Plain screens, the Assets and Recoveries report, tests |
| 0604 | The ReportStudio checks on real data (Windows and Crystal needed): golden image files, one payslip layout previewed, printed and saved as PDF, a bulk run of 50 payslips with page numbers restarting, a Form 130 style certificate on one page |
