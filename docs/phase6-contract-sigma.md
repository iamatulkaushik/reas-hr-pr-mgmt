# Phase 6 contract: TDS rates (Sigma)

Date: 2026-10-13. Shared code Brave uses for Phase 6 (TDS, declarations, Form 16, Form 24Q). Brave builds the calculation in VB services; no rate, slab, limit or cess goes in code.

## What is delivered
| Item | Where |
|---|---|
| `IStatutoryRateService.GetTdsRule(asOf, regime, ageBand)` returns `OperationResult(Of TdsRule)` | Core, Services |
| `TdsRule`: standard deduction, rebate (87A) income limit and maximum, cess percent, slabs, surcharge bands, Verified, SourceRef | Core |
| `TdsSlab`, `TdsSurchargeBand`, `TdsRegime` (Old 1, New 2), `TdsAgeBand` (General 1, Senior 2, SuperSenior 3) | Core |
| `FinancialYearCalendar.StartYear(date)` and `Label(startYear)`: 1 April to 31 March, 2026 means 2026-27 | Core |
| `SqlStatutoryRateRepository.GetTdsRule` over `stat.usp_TdsParam_Get`, `usp_TdsSlab_List`, `usp_TdsSurcharge_List` (0401, unchanged) | Data |

## Rules for the caller
- The financial year comes from the date asked. A payroll month in January 2027 uses 2026-27.
- The service fails, with a message the screen can show, when there is no parameter row, no slabs, or the row is not signed off by the CA (while `RateVerificationRequired` is on). TDS must stop on a failure, never guess.
- The CA's sign-off sits on the parameter row and covers its slabs and surcharge bands together.
- No surcharge bands means no surcharge applies. The CA confirms that when signing off the year.
- Percentages are percent values (5 means 5 percent). Money is `Decimal`, rounded with `MidpointRounding.AwayFromZero`.
- Employee age band is Brave's input (date of birth against the last day of the financial year); the rule only supplies the slabs.

## Not delivered (Brave's lane, 0300 to 0399 for payroll tables, 0400 range stays Sigma's for stat.*)
- Declarations, proofs, previous-employer income, regime choice per employee, Form 16 and 24Q data. Ask for shared changes with a change request.
- No TDS rate rows are loaded. Each row arrives by migration and the CA signs it off.
