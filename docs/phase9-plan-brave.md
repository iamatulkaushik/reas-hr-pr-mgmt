# Phase 9 plan (Brave): assets and expenses

Date: 2026-11-08. Based on origin/main `5b784bd`. Phase 9 is for the Platinum plan only (`PlanFeature.AssetsAndExpenses`). Permissions are already seeded (0004): Assets, Owner all, Associate View, Create and Edit, Operator View; Expenses, Owner all, Associate View, Create, Edit and Export, Operator View and Create. Migration lane 0600 to 0699, error numbers 56000 to 56999, report `Reports > Detailed > Assets and Recoveries` already in the menu as planned. There is no schema for them yet (change request 0026).

The plan text says nothing more about what these two modules do. The design below is Brave's proposal, written so each question can be answered with a word. Nothing is built until the Owner answers.

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
1. Assets: what is issued (laptop, mobile, tools, uniform, safety items, ID card)? Do you track the purchase value of each item? No
2. When an item is lost or damaged, is the recovery the full value, a depreciated value, or an amount the Owner types each time? Depraciated Value
3. Should unreturned assets stop the full and final settlement or only appear on it as a deduction? Yes Stop Full and Final Settlment
4. Expenses: do employees claim themselves from their own login, or does the Associate enter claims on their behalf? Operator Enter on their Behalf
5. Who approves: the Owner only, or the Associate first and the Owner after? Owner Only
6. Are claims reimbursed through the payroll run as a non-taxable payslip line, or paid separately in cash or by bank? Paid Separately in cash or bank
7. Are bills or receipts to be attached (this needs a place to keep files) or only noted by number? noted by number and details in remarks
8. Are there limits per category, per claim or per month? no

## Steps once answered
| Step | What |
|---|---|
| 0600 | Schema (Sigma, change request 0026), tables and procedures for asset types, assets, issue and return |
| 0601 | Asset recovery through the regular run and on the full and final settlement |
| 0602 | Expense categories, claims, approval, payment through the regular run |
| 0603 | Plain screens, the Assets and Recoveries report, tests |
| 0604 | The ReportStudio checks on real data (Windows and Crystal needed): golden image files, one payslip layout previewed, printed and saved as PDF, a bulk run of 50 payslips with page numbers restarting, a Form 130 style certificate on one page |
