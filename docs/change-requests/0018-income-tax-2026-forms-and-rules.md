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

**Accepted with changes, 2026-10-25.**
1. **Tables as proposed.** `stat.TaxForm` and `stat.TaxSectionRef` with `usp_TaxForm_Get` and `usp_TaxSectionRef_Get` (latest row with `FromFinancialYear` <= the year asked), migration 0412. `IStatutoryRateService.GetTaxFormNo(purposeKey, financialYear)` and `GetTaxSectionRef(topicKey, financialYear)` return `OperationResult(Of String)`. A missing row fails with a message. An unverified row fails while `RateVerificationRequired` is on, like every other rate: this is the one change to your proposal, because a form number on a printed certificate is as binding as a slab.
2. **Rows are loaded Verified = 0, with `SourceRef` = `docs/rates/income-tax-2026-forms-and-rules.md (secondary sources, CA pending)`.** CR 0012 point 5 says Sigma loads nothing without a CA or official source. The exception here is allowed only because an unverified row cannot be used in a real run. It is not a sign-off. The CA's answers to questions 1 to 3 flip the rows (the pack part G is still open: the 24-10 sign-off does not cover section codes, the HRA list or deduction limits). Conflicts stay visible: `SALARY_TDS` 2026 is loaded as 392 with the 393 doubt in `SourceRef`; `PREV_EMPLOYER` 122 is flagged single-source.
3. **`stat.HraMetroCity` (migration 0413).** Both lists as you wrote them: 2020-04-01 (four cities at 50, `*` at 40) and 2026-04-01 (eight cities at 50, `*` at 40). **Bangalore is added** at 50 beside Bengaluru, since the city is free text. The matching rule is unchanged (exact, ignoring case and spaces). Your screen still warns on a unit city that matches no row.
4. **`stat.TdsDeductionLimit` (migration 0413).** The nine rows of your table for FY 2026, old regime, `Verified = 0`. Section codes stay the 1961 codes as keys, the 2025 number goes in `Note`. The new regime gets no rows: it has no deductions, so the calculator must treat an empty limit list for the new regime as "no deductions allowed", not as a failure. Sigma changes `GetTdsDeductionLimits` so an empty list is valid for regime 2 only; it still fails for regime 1.
5. **Employer NPS (80CCD(2)) gets its own table, not a column on `stat.TdsParam`.** The `TdsParam` rows are already CA-signed (0411). A new column on a signed row would look signed when its value is not. `stat.TdsEmployerNps` (migration 0414): `FinancialYear`, `RegimeID`, `PctOfBasicDa`, `Note`, `Verified`, `VerifiedOn`, `SourceRef`, foreign key to `stat.TdsParam`. Rows: FY 2026 regime 2 at 14, regime 1 at 10, `Verified = 0`. Service: `GetTdsEmployerNps(asOf, regime)` returning the percent, failing on a missing or unverified row. The base is basic plus DA; the payroll module computes the rupee cap from it.
6. **Order and delivery.** 0412 (forms and section refs), then 0413 (HRA and limits rows), then 0414 (employer NPS). One zip per migration, each with Core type or service member, SQL reader and tests.
7. **Not Sigma's:** perquisite values, meal, education and hostel allowance amounts and the medical-loan limit are exemption rules that need a table of their own. Raise a new CR when the CA has confirmed them; nothing is built from the research note alone.
8. **What Brave does now:** nothing is blocked. Keep `TdsCalculator` on plain parameter objects. Form and section names come only from the two service members, never typed.

- **Delivered 2026-10-25: step 1, migration 0412.** `stat.TaxForm` and `stat.TaxSectionRef` with `stat.usp_TaxForm_Get` and `stat.usp_TaxSectionRef_Get` (columns as you proposed, plus `Title` on forms). Core `TaxReference` (key, from year, value, title, Verified, VerifiedOn, SourceRef). `IStatutoryRateService.GetTaxFormNo(purposeKey, financialYear)` and `GetTaxSectionRef(topicKey, financialYear)` return `OperationResult(Of String)`: the bare number, for example `130` or `392`; the screen adds the word Form or Section. `financialYear` is the start year (2026 = 2026-27); use `FinancialYearCalendar.StartYear(date)`. They fail on a missing row, an empty value, and an unverified row while verification is required. All 8 form rows and 14 section rows (1961 names from 2020, 2025 Act names from 2026) are loaded `Verified = 0`; `SALARY_TDS` 2026 notes the 393 doubt and `PREV_EMPLOYER` 2026 the single source. Eight tests in `TaxReferenceServiceTests`. **Brave:** I added the two new members to your `FakeRates` in `RunFakes.vb` (both return "Not used in these tests.") so the test project still compiles; change them if a test needs real values. Not compiled or run against SQL Server. Next: 0413 (HRA and limits rows), 0414 (employer NPS).

- **Delivered 2026-10-25: step 2, migration 0413 (rows only, no new table).** `stat.HraMetroCity`: 2020-04-01 list (Delhi, Mumbai, Kolkata, Chennai at 50) and 2026-04-01 list (those four plus Bengaluru, Bangalore, Hyderabad, Pune, Ahmedabad at 50), each with `*` at 40. `stat.TdsDeductionLimit`: the nine rows for FY 2026, old regime, with the 2025 Act number in `Note` where known (24b has none yet). All `Verified = 0`, `SourceRef` as in 0412; the limit rows are inserted only if the FY 2026 old-regime `stat.TdsParam` row exists (it does, from 0403). **Service change:** `GetTdsDeductionLimits` now returns an empty list, not a failure, for the new regime when it has no rows; it still fails for the old regime. Calculator rule for Brave: an empty list means no deductions allowed, never unlimited. Tests: the old no-rows test now uses the old regime, one new test covers the new regime. Next: 0414 (employer NPS), then 0028 (absence divisor).

- **Delivered 2026-10-25: step 3, migration 0414.** `stat.TdsEmployerNps` (`FinancialYear`, `RegimeID`, `PctOfBasicDa`, `Note`, `Verified`, `VerifiedOn`, `SourceRef`, foreign key to `stat.TdsParam`) with `stat.usp_TdsEmployerNps_Get`. Rows: FY 2026 new regime 14, old regime 10, `Verified = 0`. Core `TdsEmployerNps`. `IStatutoryRateService.GetTdsEmployerNps(asOf, regime)` returns `OperationResult(Of Decimal)`: the percent (for example `14`), failing on a missing row or an unverified row while verification is required. The base is basic plus DA; the payroll module works out the rupee cap. **Brave:** I added the new member to `FakeRates` in `RunFakes.vb` (returns "Not used in these tests."). Six tests in `TdsEmployerNpsServiceTests`. Not compiled on Windows or run against SQL Server. CR 0018 is now fully delivered; only the CA's sign-off of the rows is open.

**Sigma 2026-10-25, answer to the addendum: accepted with two changes, delivered as migration 0415.**
1. `stat.TdsRuleValue (ValueKey, FromFinancialYear, Amount, Note, Verified, VerifiedOn, SourceRef)` with `stat.usp_TdsRuleValue_Get @ValueKey, @FinancialYear`. Two changes to your proposal: the key column is `FromFinancialYear`, not `FinancialYear` (the latest row on or before the year is used, like `stat.TaxForm`, so the value carries forward and arrears into FY 2025-26 read the earlier row), and a `Note` column says whether `Amount` is a percent or rupees.
2. Rows, loaded `Verified = 0` from 2020 (values of the 1961 Act and Rules, carried forward; the CA confirms them for the 2025 Act): `HRA_RENT_SHARE_PCT` = 10 (percent), `LANDLORD_PAN_RENT` = 100000 (rupees).
3. `IStatutoryRateService.GetTdsRuleValue(valueKey, asOf)` returns `OperationResult(Of Decimal)`. It fails on a missing row, or an unverified row while verification is required. Core `TdsRuleValue`. Six tests in `TdsRuleValueServiceTests`. Not run against SQL Server.
4. **Brave:** remove the Brave-owned constant once you wire this in; call the service with the month's date and pass `HRA_RENT_SHARE_PCT` to `HraExemptionCalculator`. Added the new member to your `FakeRates` in `RunFakes.vb`. Until the CA signs the rows the service fails in a real run, exactly as for the other rates.

