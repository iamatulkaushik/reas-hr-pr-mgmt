# Sigma review: Phase 9 asset recovery (migrations 0600 to 0607 and the recovery service)

Date: 2026-11-13. Reviewed in the repository (commit 30a7b76); not run (no .NET or SQL Server here). Scope: the SQL scripts 0600 to 0607, `AssetRecoveryService`, and how the recoveries reach payroll (`SqlAssetRecoveryLedger`, `SqlFnfAssetSource`, the arrears recovery ledger path). Not read line by line: the asset screens (none yet) and the test projects.

## 1. What is right
- **Tenancy:** every `ast` table has a row-level security policy (0601, 0604); the foreign keys are composite on `(CompanyID, ...)`, so a recovery cannot point at another company's asset, hand-over or employee. Guards and the ordering test pass.
- **Access:** stored procedures only; each reads the company from the session and filters on it; errors 53510 to 53517 are inside the range reserved for `ast` (53500 to 53599); Expenses start at 53520 as planned.
- **Money:** `DECIMAL(12,2)`, `Amount > 0`, whole rupees from the service; at most one active recovery per hand-over (filtered unique index); a recovery is never deleted, only cancelled with a reason.
- **Payroll hand-over:** reserve and release in the style of the overpaid-arrears recovery (all or none; released until the run is approved, locked or paid; the settlement refuses to change silently, 53516). Permissions are checked per action (View, Create, Edit) and the plan feature `AssetsAndExpenses` is checked in the services.

## 2. What needs a decision before recovery goes into a real payroll
Source: the Code on Wages, 2019, section 18 (permitted deductions and the 50 percent limit) and section 21 (deductions for damage or loss), read from public texts. Sigma is not a lawyer: the CA confirms whether the Code applies to the client's state and date, and what a state's rules add.

1. **Show cause (s.21).** No deduction for damage or loss may be made until the employee has had an opportunity to show cause. The service records nothing about it, so the Owner can create a recovery without it. Proposed: `AssetRecovery` gets `ShowCauseDate DATE NOT NULL` (the date the notice was given) and `EmployeeResponse NVARCHAR(200) NULL`; a recovery cannot be created without the date, and cannot be held by a run before that date has passed.
2. **Amount not above the loss (s.21).** Today the Owner may enter any amount with a reason of three characters, even above what the asset cost. Proposed: the amount cannot exceed the asset's purchase value (the most the employer can have lost); above the depreciated value it needs the reason, as now.
3. **Total deductions not above 50 percent of wages in a wage period (s.18).** The run caps only loan EMI (`emiCapPct`, 25 percent of net, with carry forward). An asset recovery goes in at its full amount through the arrears recovery path, so with a loan, a fine and a recovery the deductions can pass 50 percent, and net pay can go to zero. Proposed: a company policy `TotalDeductionCapPct` (default 50), the recovery taken only up to the room under the cap, and the rest left Open for the next run (as the EMI carry forward does). **Open question for the CA:** which deductions count towards the cap (the Code's list in s.18(2) includes more than damage and loss; whether tax and provident fund are counted is for the CA to say).
4. **The register.** Deductions for damage or loss, and their realisation, must be recorded in a register. The planned report "Assets and Recoveries" should be that register: employee, asset, date of the show-cause notice, the response, amount, instalments taken and the date of each. Add it to the Haryana forms catalogue.

## 3. Asked of Brave
Items 1, 2 and 4 are small and can go in the next push (a migration after 0607 for the two columns, the service rules, the report columns). Item 3 changes the run calculation (`NetPayCalculator` and the policy), so it needs its own known-answer tests and, first, the CA's answer on the counted deductions; until then recoveries should be switched off for real payrolls (Owner), or capped by hand.

## 4. Verdict
Steps 0601a and 0601b: accepted for the structure, the tenancy and the payroll hand-over. The wage-law points above are a condition for using recovery on a client's payroll, not for merging. The gate also needs the usual run on Windows with screenshots once the screens exist.
