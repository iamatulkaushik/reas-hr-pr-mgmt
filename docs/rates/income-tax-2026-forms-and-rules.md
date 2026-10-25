# Income-tax Act 2025 and Rules 2026: what changes for payroll (Brave)

Date: 2026-10-25. Applies to tax year 2026-27 (1 April 2026 onwards). Financial years 2025-26 and earlier keep the 1961 Act and the old form numbers (certificates, corrections, arrears). **Nothing here is CA-verified.** Sources are secondary; the CA checks each line against the Act, the Income-tax Rules 2026 (notified 20-03-2026) and the department's concordance. Slabs, rebate, standard deduction and cess do not change (already loaded and signed, migration 0411).

## 1. Form renames (confirmed by several sources)
| Purpose | Until FY 2025-26 | From TY 2026-27 |
|---|---|---|
| Employee declaration of deductions and HRA | 12BB | **124** |
| Annual salary TDS certificate | 16 | **130** |
| Quarterly salary TDS return | 24Q | **138** |
| Annual tax statement | 26AS | 168 |
| TDS return, non-salary resident / non-resident / TCS | 26Q / 27Q / 27EQ | 140 / 144 / 143 |
| Declaration for nil tax (interest, dividend) | 15G / 15H | 121 |
| Previous employer salary and TDS statement | 12B / 12BAA | 122 (one source only; CA to confirm) |

Form 124 adds the landlord relationship, and the landlord PAN when yearly rent is above 1,00,000. Form 130 is the same certificate in a more structured layout. Q1 (April to June 2026) was the first return on Form 138.

## 2. Section codes (2025 Act)
| Topic | 1961 | 2025 |
|---|---|---|
| Salary TDS | 192 | **392** (most sources; two give 393 for the TDS provisions as a group, one gives 230. Unconfirmed, CA settles) |
| Chapter VI-A deductions | 80C | 123 (Schedule XV lists the investments) |
| NPS | 80CCD | 124 |
| Health insurance | 80D | 126 |
| Savings and deposit interest | 80TTA / 80TTB | 153 |
| New regime | 115BAC | 202 |
| Rebate | 87A | 156 |
| HRA exemption | 10(13A) | Schedule II, calculated under Rule 279 of the 2026 Rules |

Sources conflict on some numbers. The CA signs each row before it is used.

## 3. Rule changes that touch the calculator
- **HRA metro list: 8 cities at 50 percent** of basic plus DA: Delhi, Mumbai, Kolkata, Chennai, Bengaluru, Hyderabad, Pune, Ahmedabad. Every other city 40 percent. Before 1 April 2026 only the first four. Exemption is the least of: HRA received, rent paid minus 10 percent of basic plus DA, 50 or 40 percent of basic plus DA. Old regime only.
- Meal exemption: 200 per meal (was 50), under both regimes.
- Children's education allowance: 3,000 per month per child (was 100). Hostel allowance: 9,000 per month per child (was 300). Both for up to 2 children.
- Perquisite values (Rule 15) are higher for company cars (2,000 to 7,000 a month by engine size and 3,000 for a driver, electric vehicles included), lower for rent-free accommodation in metros (10 percent of salary). Owner decided perquisites are optional.
- Interest-free medical loan limit 2,00,000 (was 20,000). Gift exemption 15,000 (was 5,000). Single source; CA to confirm.

## 4. What changes in the system
| Item | Change | Owner |
|---|---|---|
| Form names on screens, reports and file names | Read from `stat.TaxForm` by financial year, never typed | Sigma table, Brave use |
| Section references | `stat.TaxSectionRef` | Sigma table |
| HRA city list | Rows in `stat.HraMetroCity` from 2026-04-01 (8 cities and `*` at 40) | Sigma loads, CA signs |
| Deduction limits | Rows in `stat.TdsDeductionLimit`, FY 2026, old regime | Sigma loads, CA signs |
| Declaration form (124): landlord name, relationship, PAN | New Brave tables, Phase 6 | Brave |

See CR 0018 and `docs/phase6-plan-brave.md`.

## 5. Sources read
tax2win.in (form list); patronaccounting.com (Rules 1962 vs 2026, form map); nriinformation.com (renames); cleartax.in; eztax.in (Form 124 landlord field); mstock.com, businesstoday.in, taxguru.in (HRA metro list, Rule 279); taxguru.in, taxheal.com, taxaj.com (section map; they conflict on 392 and 393); kpmg.com and bdo.in (education, hostel, meal, perquisite changes); activpayroll.com (Form 122).
