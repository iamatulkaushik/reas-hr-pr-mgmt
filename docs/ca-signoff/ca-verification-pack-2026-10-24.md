# CA verification pack: statutory rows loaded in the repository

Prepared by Sigma on 2026-10-24 from migrations 0403, 0408 and 0409 and `docs/rates/national-rates-2026-10.md`. Every row below is `Verified = 0`, so payroll and TDS refuse it while `RateVerificationRequired` is on. The CA checks each row against the primary source, ticks it, and fills the sign-off record at the end. Sigma then writes the sign-off migration from that record. Nothing is verified from a secondary source alone.

## A. Provident fund (`stat.EpfRate`)
| Check | Effective from | Employee | Employer EPF / EPS | EDLI / admin | Admin minimum | Wage ceiling (EPF, EPS) | EDLI ceiling | Source in the row |
|---|---|---|---|---|---|---|---|---|
| [ ] | 01-09-2014 | 12% | 3.67% / 8.33% | 0.5% / 0.5% | 500 | 15,000 | 15,000 | EPF Scheme 1952, EPS 1995, EDLI 1976 |
| [ ] | 17-09-2026 | 12% | 3.67% / 8.33% | 0.5% / 0.5% | 500 | 25,000 | 25,000 | S.O. 5109(E), 17-09-2026 |

## B. Employees' State Insurance (`stat.EsiRate`)
| Check | Effective from | Employee | Employer | Wage ceiling | No employee share when daily wage is | Rounding | Source |
|---|---|---|---|---|---|---|---|
| [ ] | 01-07-2019 | 0.75% | 3.25% | 21,000 | 176 or less | up to the next rupee | ESIC rates since 01-07-2019 |

## C. Definition of wages (`stat.WageRule`)
| Check | Effective from | Rule | Source |
|---|---|---|---|
| [ ] | 21-11-2025 | Code on Wages 2019 s.2(y): excluded components above 50% of total remuneration are added back to wages | Code on Wages 2019 in force from 21-11-2025 |

## D. Income tax, financial year 2026-27 (`stat.TdsParam`, `TdsSlab`, `TdsSurcharge`)
| Check | Regime | Standard deduction | Rebate: income up to | Rebate maximum | Cess |
|---|---|---|---|---|---|
| [ ] | Old | 50,000 | 5,00,000 | 12,500 | 4% |
| [ ] | New | 75,000 | 12,00,000 | 60,000 | 4% |

New regime slabs, every age band: 0 to 4,00,000 nil; to 8,00,000 5%; to 12,00,000 10%; to 16,00,000 15%; to 20,00,000 20%; to 24,00,000 25%; above 24,00,000 30%. [ ]

Old regime slabs: [ ]
| Age band | Slabs |
|---|---|
| Below 60 | nil to 2,50,000; 5% to 5,00,000; 20% to 10,00,000; 30% above |
| 60 to 79 | nil to 3,00,000; 5% to 5,00,000; 20% to 10,00,000; 30% above |
| 80 and above | nil to 5,00,000; 20% to 10,00,000; 30% above |

Surcharge on tax by total income: [ ]
| Regime | 50 lakh to 1 crore | 1 to 2 crore | 2 to 5 crore | Above 5 crore |
|---|---|---|---|---|
| Old | 10% | 15% | 25% | 37% |
| New | 10% | 15% | 25% (above 2 crore) | 25% |

Source in the rows: Finance Act 2025 rates, "unchanged by Budget 2026". From 1 April 2026 the Income-tax Act, 2025 applies (salary TDS under section 392). The CA confirms the rates still hold under the new Act and gives the new section references.

## E. Haryana labour welfare fund (`stat.LabourWelfareRule`, state 6)
| Check | Effective from | Employee | Employer | Frequency and due months | Wage cutoff | Source |
|---|---|---|---|---|---|---|
| [ ] | 01-04-2019 | 0.2% of wages, at most 25 a month | twice the employee amount | monthly, all months (the board page's reading; **CA to confirm**) | none | Haryana Labour Welfare Board page, Punjab Labour Welfare Fund Act 1965 s.9-A (`docs/sources/haryana-lwf.md`) |

## F. Haryana minimum wages (`stat.MinimumWage`, state 6, zone 1)
Effective 01-04-2026, Gazette Extraordinary No. 51-2026/Ext., Notification 2/25/26-2 Lab (`docs/sources/haryana-minimum-wage-2026-04-09.pdf`).
| Check | Skill | Per month | Per day |
|---|---|---|---|
| [ ] | Unskilled | 15,220.71 | 585.41 |
| [ ] | Semi-skilled | 16,780.74 | 645.41 |
| [ ] | Skilled | 18,500.81 | 711.56 |
| [ ] | Highly skilled | 19,425.85 | 747.14 |

## G. Questions the CA answers (they decide what Sigma loads next)
1. **September 2026 EPF split month** (`docs/phase5-split-month-proposal-brave.md`): option A (rate on the last day), B (first day), C (split by days) or D (payment date)?
2. **EPS eligibility** at the new ceiling, and whether the EDLI ceiling also moved to 25,000.
3. **ESI:** the contribution-period rule (April to September, October to March), the 25,000 ceiling for persons with disabilities, and overtime in the eligibility test.
4. **Haryana LWF:** how often it is paid, due date, wage definition, whether it rounds to whole rupees.
5. **Income-tax Act 2025:** deduction sections and limits per regime for `stat.TdsDeductionLimit`; the HRA city list and percentages for `stat.HraMetroCity`; marginal relief on rebate and surcharge.
6. **Professional tax:** slabs and due months for each state the Owner's units are in (Haryana has none).
7. **Other rules not loaded yet:** statutory bonus, gratuity, overtime limits, leave encashment under the new labour codes, and state labour welfare rules other than Haryana.
8. **Known-answer cases** for the Phase 5 gate: 10 to 15 worked salaries with the expected EPF, ESI, PT and TDS.

## H. Sign-off record (the CA fills this in)
- CA name and membership number:
- Firm:
- Date of sign-off (yyyy-mm-dd):
- Reference or letter number:
- Sections verified (A to F, or list the rows):
- Rows checked but corrected (give the right values):
- Rows not verified:
- Signature or attached signed copy (file name in `docs/ca-signoff/`):

## What Sigma does with the signed record
One new migration (next free number, never an ad hoc UPDATE) sets `Verified = 1`, `VerifiedOn` and appends the CA reference to `SourceRef` for exactly the rows the record lists, for `stat.EpfRate`, `EsiRate`, `WageRule`, `TdsParam`, `LabourWelfareRule` and `MinimumWage`. Rows the CA corrects are fixed in a separate migration first. Rows not verified stay at 0 and payroll keeps refusing them.
