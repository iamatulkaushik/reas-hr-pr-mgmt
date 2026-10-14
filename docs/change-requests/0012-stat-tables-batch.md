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
- Sigma's decision and date: pending
