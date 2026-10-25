# Change request 0018: Income-tax Act 2025 forms, sections, HRA cities and deduction limits

- Requested by: Brave
- Needed by module / phase: Phase 6 (TDS, Form 124 declarations, Form 130, Form 138); Phase 7 (arrears into FY 2025-26 use old names)
- What is missing or wrong in the shared code: `stat.HraMetroCity` and `stat.TdsDeductionLimit` have no rows. There is no place for form numbers or section references, so a screen would have to type "Form 16" or "192". Tax year 2026-27 uses Form 124, 130, 138 and section 392; FY 2025-26 and earlier keep 12BB, 16, 24Q and 192. Research note: `docs/rates/income-tax-2026-forms-and-rules.md`.
- Workaround until done: Brave builds the calculator and services against `IStatutoryRateService` and fakes. No form number or rate in code.

## Proposed schema (migration 0412, Sigma writes it)
Same pattern as 0400: no CompanyID, no tenant policy, read procedures only, rows `Verified = 0` until the CA signs.

```sql
stat.TaxForm        (PurposeKey NVARCHAR(30), FromFinancialYear SMALLINT, FormNo NVARCHAR(10), Title NVARCHAR(100),
                     Verified, VerifiedOn, SourceRef; PK (PurposeKey, FromFinancialYear))
stat.TaxSectionRef  (TopicKey NVARCHAR(30), FromFinancialYear SMALLINT, SectionRef NVARCHAR(40),
                     Verified, VerifiedOn, SourceRef; PK (TopicKey, FromFinancialYear))
stat.usp_TaxForm_Get       @PurposeKey, @FinancialYear   -- latest row with FromFinancialYear <= @FinancialYear
stat.usp_TaxSectionRef_Get @TopicKey,   @FinancialYear
```
Service: `IStatutoryRateService.GetTaxFormNo(purposeKey, financialYear)` and `GetTaxSectionRef(topicKey, financialYear)`, each an `OperationResult(Of String)`. A missing row is a failure with a message, never a guess.

## Rows to load (all `Verified = 0`)
TaxForm: `DECLARATION` 2020 → 12BB, 2026 → 124; `SALARY_CERT` 2020 → 16, 2026 → 130; `SALARY_RETURN` 2020 → 24Q, 2026 → 138; `PREV_EMPLOYER` 2020 → 12B, 2026 → 122 (CA to confirm).

TaxSectionRef: `SALARY_TDS` 2020 → 192, 2026 → 392 (CA to settle 392 against 393); `DEDUCTION_INVEST` 80C → 123; `NPS` 80CCD → 124; `HEALTH_INSURANCE` 80D → 126; `SAVINGS_INTEREST` 80TTA/80TTB → 153; `NEW_REGIME` 115BAC → 202; `REBATE` 87A → 156.

HraMetroCity (EffectiveFrom 2026-04-01): Delhi, Mumbai, Kolkata, Chennai, Bengaluru, Hyderabad, Pune, Ahmedabad at IsMetro 1, 50; `*` at IsMetro 0, 40. Add **Bangalore** at 50 as well: city is free text and matching is exact. Older list (EffectiveFrom 2020-04-01): Delhi, Mumbai, Kolkata, Chennai at 50, `*` at 40.

TdsDeductionLimit, FY 2026, old regime (RegimeID 1). Carried from the 1961 Act. The 2025 Act may keep them; the CA confirms each:
| SectionCode | SubLimitKey | MaxAmount |
|---|---|---|
| 80C | | 1,50,000 |
| 80CCD1B | | 50,000 |
| 80D | self | 25,000 |
| 80D | self senior | 50,000 |
| 80D | parent | 25,000 |
| 80D | parent senior | 50,000 |
| 80TTA | | 10,000 |
| 80TTB | | 50,000 |
| 24b | | 2,00,000 |

Section codes stay as the engine keys; the 2025 number is in `Note` (123, 124, 126, 153). Employer NPS (80CCD(2)) is a percent of basic plus DA, not a rupee cap (14 percent new regime, 10 percent old): needs a percent column or a `stat.TdsParam` field. Sigma decides. New regime: no deduction rows except that.

## Questions for the CA (part G, extended)
1. 392 or 393 for salary TDS in Form 138 and Form 130.
2. 2025 Act numbers and limits above.
3. Meal 200, education 3,000, hostel 9,000, Rule 15 car and accommodation values, medical loan 2,00,000, gift 15,000.
4. Form 138 due dates and FVU / return file version for TY 2026-27.
5. Form 130 Part A: import layout from TRACES under the new rules.

## Added 2026-10-25: two more numbers need a stat home
`HraExemptionCalculator` takes the rent share of salary (10 percent by the rule) as a parameter, and the landlord PAN check needs the yearly rent threshold (1,00,000). Neither is in a table. Proposed: two rows in a small `stat.TdsRuleValue (FinancialYear, ValueKey, Amount, Verified, VerifiedOn, SourceRef)`, keys `HRA_RENT_SHARE_PCT` and `LANDLORD_PAN_RENT`, with the CA signing them like the others. Until then the caller passes the values from a Brave-owned constant in the service layer, flagged in code.

## Sigma's decision and date
