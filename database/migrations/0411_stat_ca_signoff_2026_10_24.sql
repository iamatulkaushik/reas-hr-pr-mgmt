/* 0411_stat_ca_signoff_2026_10_24.sql
   CA sign-off. CA Vijay Tyagi, 24-10-2026, covering all rows of the verification pack (parts A to F):
     A  stat.EpfRate        effective 2014-09-01 and 2026-09-17
     B  stat.EsiRate        effective 2019-07-01
     C  stat.WageRule       effective 2025-11-21
     D  stat.TdsParam       financial year 2026, old and new regime (slabs and surcharge follow the parameter row)
     E  stat.LabourWelfareRule  Haryana (state 6) effective 2026-01-01, with the Rs 35 / Rs 70 ceiling from the CA's final reply
     F  stat.MinimumWage    Haryana (state 6) effective 2026-04-01, four skills, per month and per day
   Only rows still at Verified = 0 are touched, so a second run changes nothing. The CA reference is added to SourceRef (kept within 300 characters).
   Not covered, and not touched: the open questions in part G of the pack (September 2026 split month, EPS and EDLI, ESI contribution period, LWF due date,
   Income-tax Act 2025 sections, professional tax, bonus, gratuity, overtime, leave encashment) and every row of other tables. The CA's membership
   number and a letter number were not supplied; add them in a later migration if the CA's record carries one.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
DECLARE @On DATE = '2026-10-24';
DECLARE @Ref NVARCHAR(100) = N' CA ref: Vijay Tyagi, 24-10-2026, all rows of the verification pack.';

UPDATE stat.EpfRate
SET Verified = 1, VerifiedOn = @On, SourceRef = LEFT(ISNULL(SourceRef, N''), 300 - LEN(@Ref)) + @Ref
WHERE Verified = 0 AND EffectiveFrom IN ('2014-09-01', '2026-09-17');

UPDATE stat.EsiRate
SET Verified = 1, VerifiedOn = @On, SourceRef = LEFT(ISNULL(SourceRef, N''), 300 - LEN(@Ref)) + @Ref
WHERE Verified = 0 AND EffectiveFrom = '2019-07-01';

UPDATE stat.WageRule
SET Verified = 1, VerifiedOn = @On, SourceRef = LEFT(ISNULL(SourceRef, N''), 300 - LEN(@Ref)) + @Ref
WHERE Verified = 0 AND EffectiveFrom = '2025-11-21';

UPDATE stat.TdsParam
SET Verified = 1, VerifiedOn = @On, SourceRef = LEFT(ISNULL(SourceRef, N''), 300 - LEN(@Ref)) + @Ref
WHERE Verified = 0 AND FinancialYear = 2026 AND RegimeID IN (1, 2);

UPDATE stat.LabourWelfareRule
SET Verified = 1, VerifiedOn = @On, SourceRef = LEFT(ISNULL(SourceRef, N''), 300 - LEN(@Ref)) + @Ref
WHERE Verified = 0 AND StateID = 6 AND EffectiveFrom = '2026-01-01';

UPDATE stat.MinimumWage
SET Verified = 1, VerifiedOn = @On, SourceRef = LEFT(ISNULL(SourceRef, N''), 300 - LEN(@Ref)) + @Ref
WHERE Verified = 0 AND StateID = 6 AND EffectiveFrom = '2026-04-01';
GO
INSERT stat.RuleChangeLog (Area, Description, EffectiveFrom, SourceRef)
SELECT N'CA sign-off',
       N'Rows verified by the CA: EPF (2014-09-01, 2026-09-17), ESI (2019-07-01), wage rule (2025-11-21), TDS FY 2026 both regimes, Haryana labour welfare (2026-01-01), Haryana minimum wage (2026-04-01). Open questions in part G of the pack are not covered.',
       '2026-10-24', N'CA Vijay Tyagi, 24-10-2026, all rows of the verification pack'
WHERE NOT EXISTS (SELECT 1 FROM stat.RuleChangeLog WHERE Area = N'CA sign-off' AND EffectiveFrom = '2026-10-24');
GO
