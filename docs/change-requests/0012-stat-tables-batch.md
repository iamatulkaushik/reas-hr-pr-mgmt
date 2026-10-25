# Change request 0012: New stat tables (batch)

- Requested by: Brave
- Needed by module / phase: see order below
- Accepted in principle by Sigma in ADR 0012. Pattern as 0400: no CompanyID, no tenant policy, read procedures only, rows loaded `Verified = 0`, flipped after CA sign-off. Each row has EffectiveFrom, Verified, VerifiedOn, SourceRef. Migrations 0402 and up. Sigma writes Core types, `IStatutoryRateService` members and SQL readers.

## Tables, in order of need
| # | Table | Phase | Columns (beyond the standard four) |
|---|---|---|---|
| 1 | `stat.TdsDeductionLimit` | 6 | FinancialYear, RegimeID, SectionCode (80C, 80D, 80CCD1B, 24b and others), SubLimitKey (self, senior parent, and so on), MaxAmount, Note. Sections are rows so any can be added without code. |
| 2 | `stat.HraMetroCity` | 6 | EffectiveFrom, DistrictID or CityName, IsMetro, ExemptPct (50 or 40) |
| 3 | `stat.BonusRule` | 5/7 | EffectiveFrom, EligibilityWageCeiling, CalcWageCeiling, MinPct, MaxPct, MinDaysWorked |
| 4 | `stat.PtDueMonth` | 5 | StateID, EffectiveFrom, MonthNo. Replaces CR 0011. Slab TaxAmount is the amount per due month. |
| 5 | `stat.LabourWelfareRule` | 7 | StateID, EffectiveFrom, EmployeeAmount, EmployerAmount, FrequencyID, wage cutoff, DueMonths. Owner will supply the official documents on request. |
| 6 | `stat.MinimumWage` | 7 | StateID, EffectiveFrom, ZoneNo, SkillID, Amount, UnitID (day or month). Check is a warning only. |

## Core additions asked of Sigma
Types for each table, read members on `IStatutoryRateService`, `PtRule.DueMonths As IReadOnlyList(Of Integer)`.

## Workaround until done
Payroll handles monthly PT only. Other tables block only their own phase.

## Decision
- Sigma's decision and date: **accepted with changes, 2026-10-22.** ADR 0012 pattern confirmed (no CompanyID, no tenant policy, read procedures only, rows loaded `Verified = 0`, flipped after CA sign-off).
  1. **Order and size of delivery.** One table per Sigma change, in this order: #1 `stat.TdsDeductionLimit`, #2 `stat.HraMetroCity` (both Phase 6, next), then #3 `stat.BonusRule`, #5 `stat.LabourWelfareRule`, #6 `stat.MinimumWage`. Each comes with its migration (0405 and up, in that order), Core type, one `IStatutoryRateService` member and SQL reader, and tests. Same rule as the other rates: a missing or unverified row is a failure, never a default.
  2. **#4 `stat.PtDueMonth` is already done.** CR 0011 delivered it (migration 0402, `stat.usp_PtDueMonth_List`, `PtRule.DueMonths`, `PtRule.IsDue`). Nothing more to build. If its columns differ from what you listed in this CR, raise a new change request.
  3. **#2 uses `CityName`, not `DistrictID`.** `mst.District` exists, but units and employee addresses store the city as free text (`City`), not a district id, and HRA metro status is by city. Columns: `EffectiveFrom`, `CityName`, `IsMetro`, `ExemptPct`, plus the standard four. Match is exact, ignoring case and surrounding spaces, on the `City` of the unit the employee works in. Brave: the Phase 6 screen should warn when a unit's `City` matches no row, so a spelling difference is seen and not silently treated as non-metro.
  4. **#1 shape as proposed.** One row per section and sub-limit, so a new section is a row and not code. Seed only section codes and sub-limit keys with `MaxAmount` and `Verified = 0`; amounts are the CA's.
  5. **No values from us.** Sigma loads no rate, wage or percentage without a `SourceRef` from the CA or an official document. For #5 and #6 the Owner supplies the documents, as you noted; those two are built only when the documents are in the repo's `docs/sources/` folder or linked in the CR.
  6. **#6 is a warning only**, as you said. The service returns the rule; the warning logic stays in the Payroll module.
- **Delivered 2026-10-22: #1 and #2.**
  - **#1 `stat.TdsDeductionLimit`** (migration 0405, `stat.usp_TdsDeductionLimit_List`). Keyed by `FinancialYear`, `RegimeID`, `SectionCode`, `SubLimitKey`, so like `stat.TdsParam` it has no `EffectiveFrom` (a financial year is the effective period) and has a foreign key to `stat.TdsParam`: load the parameter row first. `MaxAmount` is NOT NULL: a section with no row is **not allowed**, so treat it as zero, never as unlimited. Core `TdsDeductionLimit`; `IStatutoryRateService.GetTdsDeductionLimits(asOf, regime)` returns the list for the financial year of `asOf`, and fails when there are no rows or, while verification is required, when any row is unsigned (the message says how many).
  - **#2 `stat.HraMetroCity`** (migration 0406, `stat.usp_HraMetroCity_List`). A list is all rows sharing the latest `EffectiveFrom` on or before the date. **One row must have `CityName` = `*`: the percentage for every city not listed.** The service fails when the list has no `*` row, so no percentage is ever assumed in code. Core `HraCity` and `HraMetroList` (`ExemptPctFor(city)` matches ignoring case and spaces and falls back to the `*` row; `IsListed(city)` lets your screen warn about a city that matches no row); `IStatutoryRateService.GetHraMetroList(asOf)`.
  - No rates are seeded. Until the CA supplies rows, Phase 6 fails with a clear message, as intended. Tests: 5 for #1, 6 for #2.
- **Delivered 2026-10-22: #3 `stat.BonusRule`** (migration 0407, `stat.usp_BonusRule_Get @AsOf`). One row per `EffectiveFrom`; the row in force on the date is returned. A ceiling of 0 means no ceiling. Core `BonusRule` has the columns you listed (`EligibilityWageCeiling`, `CalcWageCeiling`, `MinPct`, `MaxPct`, `MinDaysWorked`, `Verified`, `VerifiedOn`, `SourceRef`) and two small helpers so the zero-ceiling rule is written once: `IsEligibleOnWage(monthlyWage)` (true up to and including the ceiling) and `CalculationWage(monthlyWage)` (the wage, capped at the calculation ceiling, never raised). `IStatutoryRateService.GetBonusRule(asOf)` fails on a missing row and on an unverified row while verification is required. Whether the calculation ceiling is replaced by the minimum wage when that is higher stays in the Payroll module; it will use #6 when that table exists. Seven tests.
- **Next from Sigma:** #5 `stat.LabourWelfareRule` and #6 `stat.MinimumWage`, when the Owner's documents are in the repo.
- **What Brave does now:** nothing is blocked by this CR until Phase 6. Start Phase 6 against #1 and #2 by writing your calculators to take plain parameter objects (as with `GratuityTerms`), and map to the Core types when the PR lands.

- **Delivered 2026-10-24: #5 `stat.LabourWelfareRule` and #6 `stat.MinimumWage`** (migrations 0408 and 0409, `stat.usp_LabourWelfareRule_Get @StateID, @AsOf`, `stat.usp_MinimumWage_List @StateID, @AsOf`). Built from the Owner's documents in `docs/sources/`. Loaded with Verified = 0: Haryana labour welfare from 2019-04-01 (0.2% of wages, at most 25, employer twice) and Haryana minimum wages from 2026-04-01 (one zone, four skills, per month and per day, as in the gazette).
  - **#5 shape differs from the CR table** because Haryana is a percentage with a cap: columns are EmployeePct, EmployeeAmount (flat amount, or the cap when the percentage is set), EmployerMultiple, EmployerAmount, FrequencyID, WageCutoff, DueMonths. Core `LabourWelfareRule` has `IsDue(month)`, `EmployeeContribution(wage)` and `EmployerContribution(wage)`; Brave calls them only in a due month. Amounts round to 2 decimals, away from zero; the CA confirms if Haryana rounds to whole rupees. Frequency and due date are not on the board's page: monthly and all months is the page's reading, for the CA to confirm.
  - **#6** `GetMinimumWages(stateId, asOf)` returns every row of the latest effective date for the state, and fails on none or any unverified row. Brave: the amounts are basic minimum rates that may not be split into allowances, and the Owner has decided payroll compares them with the **basic pay only** (not basic plus DA). The warning stays in the Payroll module.
  - Services: `IStatutoryRateService.GetLabourWelfareRule(stateId, asOf)` and `GetMinimumWages(stateId, asOf)`. Tests: `LabourWelfareRuleServiceTests`, `MinimumWageServiceTests`. Not compiled or run.
  - Haryana is `mst.State` id 6 (migration 0100).

- **Correction 2026-10-24 to #5:** migration 0410 replaces the Haryana 2019 row (cap Rs 25, out of date) with the row from 2026-01-01: 0.2% of wages, ceiling Rs 35 employee and Rs 70 employer (CA final reply; Notification HLWB/REV/2026/3436 dated 08-05-2026). Periods before 2026-01-01 have no rule and fail. The ceiling is CPI-indexed every 1 January: **Sigma loads a new row each January** (calendar reminder; the CA or the board notification gives the figure). `LabourWelfareRuleServiceTests` now use the 35/70 rule.
