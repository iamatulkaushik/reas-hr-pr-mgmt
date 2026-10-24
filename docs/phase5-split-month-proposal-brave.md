# Phase 5 proposal: EPF ceiling in a split month (Brave, for the CA)

Date: 2026-10-24. Answers Sigma's review finding 2 (2026-10-14). Nothing is coded. Sigma writes the final rule into the payroll ADR after the CA decides.

## Problem
ADR 0013: the EPF wage ceiling changes in mid-September 2026. `EpfCalculator` takes one rate row for the month. A wage period (26th to 25th, or a calendar month) can contain two rate rows.

## Options
| # | Rule | Effect |
|---|---|---|
| A | Row in force on the last day of the wage period | One row, one number. Simple. Matches the month the wages belong to. |
| B | Row in force on the first day of the wage period | One row. Lags the change by one period. |
| C | Split the period by days and apply each row to its share | Most exact. Two calculations per employee, rounding twice. ECR carries one wage figure per member, so it must be summed back. |
| D | Row in force on the payment date | Depends on payment date, not on wages earned. Weak. |

## Brave recommends
A. Last day of the wage period. Reasons: one row per run, the same rule for EPF and ESI, and ECR shows one wage per member. The CA must confirm that EPFO accepts it for the transition month.

## Questions for the CA
1. Which row governs a period that spans the change: A, B, C or D?
2. Does the same rule apply to ESI (ceiling and percentage) and to the PT slabs?
3. Does the rule hold for arrears paid after the change for months before it? (Brave proposes: use the row for the month the arrears belong to.)

## Coding after the decision
- Payroll run picks the rate row with `GetEpfRate(asOf)`, where `asOf` is the date the rule names. `EpfCalculator` stays unchanged.
- Known-answer tests: one period before the change, one after, one spanning it, verified by the CA.
