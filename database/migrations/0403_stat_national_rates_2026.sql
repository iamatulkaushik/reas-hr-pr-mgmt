/* 0403_stat_national_rates_2026.sql
   National statutory rates as published on 2026-10-14: EPF, ESI, wage rule (Code on Wages), TDS for financial year 2026-27.
   Loaded as Verified = 0. They are NOT signed off. A row counts for payroll only after the CA confirms it against the source
   (see docs/rates/national-rates-2026-10.md) and Verified is set to 1 with VerifiedOn. Professional tax, LWF, minimum wage,
   bonus and gratuity rows are not in this script.
   Idempotent: a row that already exists (signed or not) is never touched. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
/* EPF: wage ceiling 15,000 from 01-09-2014, 25,000 from 17-09-2026 (S.O. 5109(E)). Rates unchanged. Admin charge minimum 500 a month. */
INSERT stat.EpfRate (EffectiveFrom, EmployeePct, EmployerEpfPct, EmployerEpsPct, EdliPct, AdminPct, AdminMinimum, WageCeiling, EdliWageCeiling, Verified, SourceRef)
SELECT v.EffectiveFrom, 12, 3.67, 8.33, 0.5, 0.5, 500, v.Ceiling, v.Ceiling, 0, v.SourceRef
FROM (VALUES
    (CAST('2014-09-01' AS DATE), CAST(15000 AS DECIMAL(12,2)), N'EPF Scheme 1952, EPS 1995, EDLI 1976; ceiling 15,000 since 01-09-2014. Published national rule, CA sign-off pending.'),
    (CAST('2026-09-17' AS DATE), CAST(25000 AS DECIMAL(12,2)), N'Ministry of Labour S.O. 5109(E) dated 17-09-2026, ceiling 25,000. Published national rule, CA sign-off pending.')
) v (EffectiveFrom, Ceiling, SourceRef)
WHERE NOT EXISTS (SELECT 1 FROM stat.EpfRate e WHERE e.EffectiveFrom = v.EffectiveFrom);
GO
/* ESI: 0.75 and 3.25 percent, ceiling 21,000, no employee share when the average daily wage is 176 or less, rounded up to the next rupee. */
INSERT stat.EsiRate (EffectiveFrom, EmployeePct, EmployerPct, WageCeiling, DailyWageExemption, RoundingID, Verified, SourceRef)
SELECT '2019-07-01', 0.75, 3.25, 21000, 176, 2, 0,
       N'ESIC rates in force since 01-07-2019; ceiling 21,000 since 01-01-2017; daily wage exemption 176. Published national rule, CA sign-off pending.'
WHERE NOT EXISTS (SELECT 1 FROM stat.EsiRate WHERE EffectiveFrom = '2019-07-01');
GO
/* Wage rule: Code on Wages 2019 section 2(y), in force 21-11-2025. Excluded components above 50 percent of total remuneration are added back to wages. */
INSERT stat.WageRule (EffectiveFrom, UseCodeWage, WageFloorPct, Notes, Verified, SourceRef)
SELECT '2025-11-21', 1, 50,
       N'Code on Wages 2019 s.2(y): excluded components above 50 percent of total remuneration are added back to wages.',
       0, N'Code on Wages 2019 in force 21-11-2025. Published national rule, CA sign-off pending.'
WHERE NOT EXISTS (SELECT 1 FROM stat.WageRule WHERE EffectiveFrom = '2025-11-21');
GO
/* TDS financial year 2026-27 (FinancialYear 2026). Regime 1 old, 2 new. Slabs unchanged from Finance Act 2025; Budget 2026 changed nothing. */
INSERT stat.TdsParam (FinancialYear, RegimeID, StandardDeduction, RebateIncomeLimit, RebateMaximum, CessPct, Verified, SourceRef)
SELECT v.FY, v.RegimeID, v.Sd, v.RebLimit, v.RebMax, 4, 0, v.Src
FROM (VALUES
    (CAST(2026 AS SMALLINT), CAST(1 AS TINYINT), CAST(50000 AS DECIMAL(12,2)), CAST(500000 AS DECIMAL(14,2)), CAST(12500 AS DECIMAL(12,2)),
        N'FY 2026-27 old regime. Finance Act 2025 rates, unchanged by Budget 2026. Published national rule, CA sign-off pending.'),
    (CAST(2026 AS SMALLINT), CAST(2 AS TINYINT), CAST(75000 AS DECIMAL(12,2)), CAST(1200000 AS DECIMAL(14,2)), CAST(60000 AS DECIMAL(12,2)),
        N'FY 2026-27 new regime. Finance Act 2025 rates, unchanged by Budget 2026. Published national rule, CA sign-off pending.')
) v (FY, RegimeID, Sd, RebLimit, RebMax, Src)
WHERE NOT EXISTS (SELECT 1 FROM stat.TdsParam p WHERE p.FinancialYear = v.FY AND p.RegimeID = v.RegimeID);
GO
/* Slabs. A slab runs from FromAmount to ToAmount; the top slab has no ToAmount. New regime: the same slabs for every age band. */
INSERT stat.TdsSlab (FinancialYear, RegimeID, AgeBandID, SlabNo, FromAmount, ToAmount, RatePct)
SELECT 2026, s.RegimeID, a.AgeBandID, s.SlabNo, s.FromAmount, s.ToAmount, s.RatePct
FROM (VALUES
    (CAST(2 AS TINYINT), CAST(1 AS TINYINT),       0, 400000,  0),
    (2, 2,  400000,  800000,  5),
    (2, 3,  800000, 1200000, 10),
    (2, 4, 1200000, 1600000, 15),
    (2, 5, 1600000, 2000000, 20),
    (2, 6, 2000000, 2400000, 25),
    (2, 7, 2400000,    NULL, 30)
) s (RegimeID, SlabNo, FromAmount, ToAmount, RatePct)
CROSS JOIN (VALUES (CAST(1 AS TINYINT)), (2), (3)) a (AgeBandID)
WHERE NOT EXISTS (SELECT 1 FROM stat.TdsSlab t WHERE t.FinancialYear = 2026 AND t.RegimeID = s.RegimeID AND t.AgeBandID = a.AgeBandID AND t.SlabNo = s.SlabNo);
GO
/* Old regime slabs by age band: 1 below 60, 2 aged 60 to 79, 3 aged 80 and above. */
INSERT stat.TdsSlab (FinancialYear, RegimeID, AgeBandID, SlabNo, FromAmount, ToAmount, RatePct)
SELECT 2026, 1, s.AgeBandID, s.SlabNo, s.FromAmount, s.ToAmount, s.RatePct
FROM (VALUES
    (CAST(1 AS TINYINT), CAST(1 AS TINYINT),       0,  250000,  0),
    (1, 2,  250000,  500000,  5),
    (1, 3,  500000, 1000000, 20),
    (1, 4, 1000000,    NULL, 30),
    (2, 1,       0,  300000,  0),
    (2, 2,  300000,  500000,  5),
    (2, 3,  500000, 1000000, 20),
    (2, 4, 1000000,    NULL, 30),
    (3, 1,       0,  500000,  0),
    (3, 2,  500000, 1000000, 20),
    (3, 3, 1000000,    NULL, 30)
) s (AgeBandID, SlabNo, FromAmount, ToAmount, RatePct)
WHERE NOT EXISTS (SELECT 1 FROM stat.TdsSlab t WHERE t.FinancialYear = 2026 AND t.RegimeID = 1 AND t.AgeBandID = s.AgeBandID AND t.SlabNo = s.SlabNo);
GO
/* Surcharge bands on the tax, by total income. Old regime tops out at 37 percent, new regime at 25 percent. */
INSERT stat.TdsSurcharge (FinancialYear, RegimeID, SlabNo, FromIncome, ToIncome, RatePct)
SELECT 2026, s.RegimeID, s.SlabNo, s.FromIncome, s.ToIncome, s.RatePct
FROM (VALUES
    (CAST(1 AS TINYINT), CAST(1 AS TINYINT),  5000000, 10000000, 10),
    (1, 2, 10000000, 20000000, 15),
    (1, 3, 20000000, 50000000, 25),
    (1, 4, 50000000,     NULL, 37),
    (2, 1,  5000000, 10000000, 10),
    (2, 2, 10000000, 20000000, 15),
    (2, 3, 20000000,     NULL, 25)
) s (RegimeID, SlabNo, FromIncome, ToIncome, RatePct)
WHERE NOT EXISTS (SELECT 1 FROM stat.TdsSurcharge t WHERE t.FinancialYear = 2026 AND t.RegimeID = s.RegimeID AND t.SlabNo = s.SlabNo);
GO
INSERT stat.RuleChangeLog (Area, Description, EffectiveFrom, SourceRef)
SELECT v.Area, v.Description, v.EffectiveFrom, v.SourceRef
FROM (VALUES
    (N'EPF', N'Wage ceiling raised from 15,000 to 25,000 for EPF, EPS and EDLI. Rates unchanged.', CAST('2026-09-17' AS DATE), N'S.O. 5109(E) dated 17-09-2026'),
    (N'Wage', N'Code on Wages definition of wages (50 percent rule) in force.', CAST('2025-11-21' AS DATE), N'Code on Wages 2019 s.2(y)'),
    (N'TDS', N'FY 2026-27 slabs, standard deduction, rebate, surcharge and cess loaded, unchanged from FY 2025-26.', CAST('2026-04-01' AS DATE), N'Finance Act 2025; Budget 2026 no change')
) v (Area, Description, EffectiveFrom, SourceRef)
WHERE NOT EXISTS (SELECT 1 FROM stat.RuleChangeLog l WHERE l.Area = v.Area AND l.Description = v.Description);
GO
