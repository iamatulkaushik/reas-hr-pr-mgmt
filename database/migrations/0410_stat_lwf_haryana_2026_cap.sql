/* 0410_stat_lwf_haryana_2026_cap.sql
   Correction to 0408 (CA, final reply 2026-10-24). Haryana's monthly labour welfare ceiling is Rs 35 for the employee and Rs 70 for the employer
   from 1 January 2026 (Haryana Labour Welfare Board Notification No. HLWB/REV/2026/3436 dated 08-05-2026). The 0408 row (Rs 25 cap from 2019-04-01)
   is out of date: the ceiling was raised several times since, so that row would give a wrong amount for any later month. It is removed (it was never
   verified, so nothing depends on it) and the 2026 row is loaded. Periods before 2026-01-01 find no rule and fail; they are not guessed.
   The ceiling is indexed to the consumer price index every 1 January, so a new row is needed each January (effective 01-01 of that year).
   Loaded with Verified = 0 until the CA signs the verification pack. Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
DELETE FROM stat.LabourWelfareRule WHERE StateID = 6 AND EffectiveFrom = '2019-04-01' AND Verified = 0;
GO
IF NOT EXISTS (SELECT 1 FROM stat.LabourWelfareRule WHERE StateID = 6 AND EffectiveFrom = '2026-01-01')
    INSERT stat.LabourWelfareRule (StateID, EffectiveFrom, EmployeePct, EmployeeAmount, EmployerMultiple, EmployerAmount, FrequencyID, WageCutoff, DueMonths, Verified, SourceRef)
    VALUES (6, '2026-01-01', 0.20, 35.00, 2.00, 0.00, 1, 0.00, N'1,2,3,4,5,6,7,8,9,10,11,12', 0,
            N'Haryana Labour Welfare Board Notification No. HLWB/REV/2026/3436 dated 08-05-2026: 0.2% of wages, ceiling Rs 35 employee and Rs 70 employer from 01-01-2026; confirmed by the CA (final reply 2026-10-24). Ceiling is CPI-indexed every 1 January.');
GO
