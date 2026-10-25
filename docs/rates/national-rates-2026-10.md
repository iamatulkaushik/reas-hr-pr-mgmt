# National statutory rates loaded by migration 0403

Loaded 2026-10-14 from published national sources. **All rows are `Verified = 0`.** Payroll and TDS refuse unverified rows while `RateVerificationRequired` is on. The CA checks each row against the primary source, then Sigma writes a sign-off migration (template at the end).

Sources were read from secondary publications (law-firm and payroll-vendor notes) that quote the notifications. The primary texts to check against: Ministry of Labour gazette notification S.O. 5109(E) of 17-09-2026 (EPF ceiling), ESIC contribution page, Code on Wages 2019 s.2(y) and its 2026 Central Rules, Finance Act 2025 and the Income-tax department's FY 2026-27 rate tables.

## EPF, EPS, EDLI (`stat.EpfRate`)
| Item | Up to 16-09-2026 | From 17-09-2026 |
|---|---|---|
| Effective from | 01-09-2014 | 17-09-2026 |
| Employee | 12% | 12% |
| Employer EPF / EPS | 3.67% / 8.33% | 3.67% / 8.33% |
| EDLI, admin | 0.5%, 0.5% | 0.5%, 0.5% |
| Admin minimum per establishment | 500 | 500 |
| Wage ceiling (EPF, EPS, EDLI) | 15,000 | 25,000 |

Notes for Brave:
- The 25,000 ceiling starts mid-month. **September 2026 is a split month**: 1 to 16 September on 15,000 and 17 to 30 September on 25,000. `GetEpfRate(asOf)` returns one row, so a month that spans the change needs two calls and a day split. October 2026 onward needs no split.
- Your test fixtures still use 15,000. That is fine for tests; add a case at 25,000 (employee 3,000, EPS 2,083, EPF 917, EDLI 125).
- EPS is open only to members whose wage did not exceed the ceiling on joining or on 17-09-2026. That eligibility flag belongs on the employee statutory flags, not in the rate row.
- Admin charge is 500 per month per establishment (75 if no member contributed). The row stores the 500 minimum.

## ESI (`stat.EsiRate`), effective 01-07-2019
Employee 0.75%, employer 3.25%, wage ceiling 21,000, no employee share when the average daily wage is 176 or less, rounded **up** to the next rupee. Unchanged by the EPF ceiling change.
- Not in the table yet: the 25,000 ceiling for persons with disabilities, the contribution-period rule (a member who crosses 21,000 stays covered to the end of the April to September or October to March period), and overtime being left out of the eligibility test. Raise a change request when payroll needs them.

## Wage rule (`stat.WageRule`), effective 21-11-2025
Code on Wages 2019 s.2(y) is in force: excluded components above 50% of total remuneration are added back to wages. Row: `UseCodeWage = 1`, `WageFloorPct = 50`.

## TDS, financial year 2026-27 (`FinancialYear = 2026`)
Unchanged from 2025-26.
| | Old regime | New regime |
|---|---|---|
| Standard deduction | 50,000 | 75,000 |
| Rebate (87A): income up to / maximum | 5,00,000 / 12,500 | 12,00,000 / 60,000 |
| Cess | 4% | 4% |

New regime slabs (all age bands): 0 to 4,00,000 nil; to 8,00,000 5%; to 12,00,000 10%; to 16,00,000 15%; to 20,00,000 20%; to 24,00,000 25%; above 30%.
Old regime slabs: below 60: nil to 2,50,000, 5% to 5,00,000, 20% to 10,00,000, 30% above. Age 60 to 79: nil to 3,00,000, then as above. Age 80 and above: nil to 5,00,000, 20% to 10,00,000, 30% above.
Surcharge on tax by total income: 50 lakh to 1 crore 10%; 1 to 2 crore 15%; old regime 2 to 5 crore 25% and above 5 crore 37%; new regime above 2 crore 25%.
Not in the tables, so the TDS engine must handle them: marginal relief on the 87A rebate just above 12 lakh and on surcharge, and the 15% cap on surcharge for special-rate income.

## Not loaded
Professional tax slabs and due months per state (none needed for Phase 5; Haryana has none), LWF, minimum wages, bonus, gratuity, leave encashment, overtime limits.

## Sign-off migration template
When the CA has checked the rows, tell Sigma the date and the reference. Sigma writes this as the next free migration number (never an ad hoc UPDATE):

```
UPDATE stat.EpfRate SET Verified = 1, VerifiedOn = '<yyyy-mm-dd>', SourceRef = SourceRef + N' CA ref: <reference>' WHERE Verified = 0 AND EffectiveFrom IN ('2014-09-01', '2026-09-17');
-- same for stat.EsiRate (2019-07-01), stat.WageRule (2025-11-21), stat.TdsParam (FinancialYear 2026, RegimeID 1 and 2)
```
Until then, for development only, a Developer can set the `RateVerificationRequired` setting to 0 in the test database. Never in a pilot or production database.

## Sign-off status (updated 2026-10-24)
CA Vijay Tyagi signed all rows of the verification pack on 24-10-2026. Migration 0411 sets `Verified = 1` for the rows in parts A to F (EPF, ESI, wage rule, TDS FY 2026 both regimes, Haryana labour welfare, Haryana minimum wage). The record is in `docs/ca-signoff/ca-signoff-record-2026-10-24.md`. The open questions in part G are not covered and nothing for them is loaded.
