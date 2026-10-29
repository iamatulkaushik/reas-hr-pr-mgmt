# Phase 6 plan (Brave): TDS on salary

Date: 2026-10-25. Based on origin/main `4ae6062`. Reads `docs/phase6-contract-sigma.md` and CR 0018. Tax year 2026-27 names: Form 124, Form 130, Form 138, section 392.

## Owner decisions that fix the scope
Both regimes as options per employee. TDS spread equally over the remaining months. Previous employer income yes, perquisites optional. HRA exemption with metro list and rent proof. Form 130 Part A by file import. Form 138 FVU-ready. All deduction sections as options. No employee self-service: the Associate enters declarations.

## Order of work (each step ends with a zip)
| # | Step | Lane | Needs |
|---|---|---|---|
| 1 | `TdsCalculator` (pure, no database). **Done 2026-10-25**, 25 tests | Payroll module | Nothing: rules come in as `TdsRule` |
| 2 | `HraExemptionCalculator`. **Done 2026-10-25**, 14 tests | Payroll module | `HraMetroList` from Sigma; rent share and PAN threshold need a stat home (CR 0018 addendum) |
| 3 | Declaration tables and procs, migrations 0312 to 0314. **Written 2026-10-25, not run** (no SQL Server here; Windows CI runs the isolation test) | Payroll database | None |
| 4 | `TdsDeclarationService`, `TdsProjectionService`, `SqlTdsRepository`, wiring in `PayrollServices` and `PayrollModule`. **Done 2026-10-25**, 78 new tests; the SQL repository and wiring are not compiled here (SqlClient) | Payroll module | 1 to 3 |
| 5 | Hook into the payroll run line: monthly TDS amount | Payroll run service | 4. Change request 0019 accepted by Sigma 2026-10-28. 5a **done 2026-10-28**: migration 0323, `RunLineResult.TdsAmount`, tax cap in `NetPayCalculator`, calculator hook, 11 tests. 5b **done 2026-10-28**: `EmployeeTaxFacts`, `IRunTaxSource`, `RunTdsPlanner`, tax hook and `TdsWarnings` in `PayrollRunService.Process` (new optional constructor arguments), `SalaryLineView.IsHra`, migration 0324 (`usp_RunTax_Facts`, `usp_RunInput_List` with `IsHra`), `SqlRunTaxSource`, 11 tests. Still to do: wire `SqlRunTaxSource` and `TdsProjectionService` into `PayrollServices`/`PayrollModule`, `SalaryComponentItem.IsHra` for the salary screen, `PayslipRow` mapping after Sigma's 0701 |
| 6 | Form 130 data, Form 138 data, FVU-ready file | Payroll module | CA answers 4 and 5 of CR 0018 |

## Calculator rules (step 1)
1. Annual taxable income: projected salary for the year (earned months plus remaining months at the current structure) plus previous employer income, minus exempt allowances, minus HRA exemption (old regime), minus standard deduction.
2. Old regime: minus declared deductions, each capped at its `TdsDeductionLimit` row. A section with no row counts as zero.
3. Slabs by age band: date of birth against 31 March of the year.
4. Rebate: income up to the limit gets the lower of tax and the maximum. Marginal relief just above the limit: tax cannot exceed the income above the limit.
5. Surcharge by band, with marginal relief. 15 percent cap for special-rate income is not in scope (salary only).
6. Cess on tax plus surcharge.
7. Monthly TDS: (annual tax minus tax already deducted, including previous employer) divided by remaining months, rounded to the nearest rupee, `MidpointRounding.AwayFromZero`. The last month takes the balance.
8. Regime comes from the employee's declaration. No declaration means the new regime (default in the Act); the screen shows that.
9. A failure from `GetTdsRule` stops TDS for that run with the service message.

## New tables (migrations 0312 onward, no CompanyID except through the employee)
`pay.TdsDeclaration` (employee, financial year, regime, status Draft / Submitted / Locked). `pay.TdsDeclarationItem` (section code, sub-limit key, declared amount, proof reference, verified amount). `pay.TdsHraRent` (employee, FY, months, monthly rent, city, landlord name, **landlord relationship**, landlord PAN, required above 1,00,000 a year: Form 124). `pay.TdsPrevEmployer` (income, perquisites, TDS, PF, PT). `pay.TdsMonthLine` (run line, projected annual tax, TDS this month). Landlord PAN is encrypted like other identifiers (ADR 0011). Proof files are stored as references only.

## Migrations written
0312 tables (`TdsDeclaration`, `TdsDeclarationItem`, `TdsHraRent`, `TdsPrevEmployer`, `TdsMonthLine`). 0313 tenant policy. 0314 procs: declaration Get, Save, SetStatus; item List and Replace; rent List and Replace (landlord PAN arrives already encrypted, month ranges checked inside the financial year, no overlap); previous employer Get, Save, Clear. Errors 50340 to 50345. `TdsMonthLine` procs come with step 5 (0315).

## Step 4 rules to confirm with the CA
A deduction item counts at its verified amount when proof was checked, otherwise its declared amount. A previous employer's provident fund and professional tax are not added to deductions automatically; the Associate declares them as items. No declaration means the new regime. Projection uses the first day of the run month for rate dates. Two flagged constants wait for `stat.TdsRuleValue`: `HraRentSharePct` (10) and `LandlordPanRentThreshold` (1,00,000).

## Not in this phase
Full and final tax and arrears (Phase 7). Perquisite valuation tables beyond what the Owner enables. Employee portal.

## Open before step 6
CA answers to CR 0018 questions 1, 4 and 5. A sample Part A file from TRACES. A sample FVU validation report, or the version of the file format to follow.

## Build note
Tests run through the throwaway runner on the .NET 10 SDK (10.0.113), warnings as errors, xunit stand-in. Every project, the CI workflow and the SDK already target .NET 10; nothing needed changing. A real xunit run needs Windows CI.
