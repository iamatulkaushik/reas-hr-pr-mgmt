/* 0413_stat_hra_and_deduction_limit_rows.sql
   CR 0018. Rows for stat.HraMetroCity and stat.TdsDeductionLimit (tables from 0406 and 0405).
   HRA city lists: from 2020-04-01 four metro cities at 50 percent; from 2026-04-01 eight at 50 percent (Bangalore added beside Bengaluru, because the
   city is free text and matching is exact). CityName '*' is every other city at 40 percent.
   Deduction limits: FY 2026, old regime (RegimeID 1) only, carried from the 1961 Act. The new regime has no deduction rows: an empty list means no
   deductions allowed. The 2025 Act section number is in Note where it is known. Employer NPS is a percent, not a rupee cap: see 0414.
   Every row is loaded Verified = 0 from docs/rates/income-tax-2026-forms-and-rules.md and CR 0018, which rest on secondary sources. They cannot be used in
   a real run until the CA signs them (pack part G is still open on these). Nothing here is a sign-off.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
DECLARE @Src NVARCHAR(120) = N'docs/rates/income-tax-2026-forms-and-rules.md (secondary sources, CA pending)';

INSERT stat.HraMetroCity (EffectiveFrom, CityName, IsMetro, ExemptPct, Verified, SourceRef)
SELECT v.EffectiveFrom, v.CityName, v.IsMetro, v.ExemptPct, 0, @Src
FROM (VALUES
    (CAST('2020-04-01' AS DATE), N'Delhi',     CAST(1 AS BIT), CAST(50 AS DECIMAL(5,2))),
    (CAST('2020-04-01' AS DATE), N'Mumbai',    CAST(1 AS BIT), CAST(50 AS DECIMAL(5,2))),
    (CAST('2020-04-01' AS DATE), N'Kolkata',   CAST(1 AS BIT), CAST(50 AS DECIMAL(5,2))),
    (CAST('2020-04-01' AS DATE), N'Chennai',   CAST(1 AS BIT), CAST(50 AS DECIMAL(5,2))),
    (CAST('2020-04-01' AS DATE), N'*',         CAST(0 AS BIT), CAST(40 AS DECIMAL(5,2))),
    (CAST('2026-04-01' AS DATE), N'Delhi',     CAST(1 AS BIT), CAST(50 AS DECIMAL(5,2))),
    (CAST('2026-04-01' AS DATE), N'Mumbai',    CAST(1 AS BIT), CAST(50 AS DECIMAL(5,2))),
    (CAST('2026-04-01' AS DATE), N'Kolkata',   CAST(1 AS BIT), CAST(50 AS DECIMAL(5,2))),
    (CAST('2026-04-01' AS DATE), N'Chennai',   CAST(1 AS BIT), CAST(50 AS DECIMAL(5,2))),
    (CAST('2026-04-01' AS DATE), N'Bengaluru', CAST(1 AS BIT), CAST(50 AS DECIMAL(5,2))),
    (CAST('2026-04-01' AS DATE), N'Bangalore', CAST(1 AS BIT), CAST(50 AS DECIMAL(5,2))),
    (CAST('2026-04-01' AS DATE), N'Hyderabad', CAST(1 AS BIT), CAST(50 AS DECIMAL(5,2))),
    (CAST('2026-04-01' AS DATE), N'Pune',      CAST(1 AS BIT), CAST(50 AS DECIMAL(5,2))),
    (CAST('2026-04-01' AS DATE), N'Ahmedabad', CAST(1 AS BIT), CAST(50 AS DECIMAL(5,2))),
    (CAST('2026-04-01' AS DATE), N'*',         CAST(0 AS BIT), CAST(40 AS DECIMAL(5,2)))
) AS v (EffectiveFrom, CityName, IsMetro, ExemptPct)
WHERE NOT EXISTS (SELECT 1 FROM stat.HraMetroCity t WHERE t.EffectiveFrom = v.EffectiveFrom AND t.CityName = v.CityName);

INSERT stat.TdsDeductionLimit (FinancialYear, RegimeID, SectionCode, SubLimitKey, MaxAmount, Note, Verified, SourceRef)
SELECT 2026, 1, v.SectionCode, v.SubLimitKey, v.MaxAmount, v.Note, 0, @Src
FROM (VALUES
    (N'80C',     N'',              CAST(150000 AS DECIMAL(14,2)), N'Carried from the 1961 Act. Section 123 in the 2025 Act.'),
    (N'80CCD1B', N'',              CAST(50000  AS DECIMAL(14,2)), N'Carried from the 1961 Act. Section 124 in the 2025 Act.'),
    (N'80D',     N'self',          CAST(25000  AS DECIMAL(14,2)), N'Carried from the 1961 Act. Section 126 in the 2025 Act.'),
    (N'80D',     N'self senior',   CAST(50000  AS DECIMAL(14,2)), N'Carried from the 1961 Act. Section 126 in the 2025 Act.'),
    (N'80D',     N'parent',        CAST(25000  AS DECIMAL(14,2)), N'Carried from the 1961 Act. Section 126 in the 2025 Act.'),
    (N'80D',     N'parent senior', CAST(50000  AS DECIMAL(14,2)), N'Carried from the 1961 Act. Section 126 in the 2025 Act.'),
    (N'80TTA',   N'',              CAST(10000  AS DECIMAL(14,2)), N'Carried from the 1961 Act. Section 153 in the 2025 Act.'),
    (N'80TTB',   N'',              CAST(50000  AS DECIMAL(14,2)), N'Carried from the 1961 Act. Section 153 in the 2025 Act.'),
    (N'24b',     N'',              CAST(200000 AS DECIMAL(14,2)), N'Home loan interest, carried from the 1961 Act. CA to confirm the 2025 Act number.')
) AS v (SectionCode, SubLimitKey, MaxAmount, Note)
WHERE EXISTS (SELECT 1 FROM stat.TdsParam p WHERE p.FinancialYear = 2026 AND p.RegimeID = 1)
  AND NOT EXISTS (SELECT 1 FROM stat.TdsDeductionLimit t
                  WHERE t.FinancialYear = 2026 AND t.RegimeID = 1 AND t.SectionCode = v.SectionCode AND t.SubLimitKey = v.SubLimitKey);
GO
