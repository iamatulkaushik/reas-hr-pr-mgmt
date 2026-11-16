/* 0416_stat_esi_disabled_ceiling.sql
   Change request 0030. Statutory lane 0400-0499. Idempotent. Never edit after merge.
   stat.EsiRate.DisabledWageCeiling: wage ceiling for an employee with a disability. NULL means the same ceiling as everyone else.
   The 2019-07-01 row gets 25000 (ESIC coverage page: 21,000 a month, 25,000 for a person with disability). That row was CA-verified in 0411 BEFORE this column
   existed, so Verified is left as it was and the 25,000 is NOT covered by that sign-off: the note below says so, and the CA must confirm it (CR 0030, open for the CA).
   usp_EsiRate_Get is the 0401 procedure with the new column added last. */
SET XACT_ABORT ON;
GO
IF COL_LENGTH(N'stat.EsiRate', N'DisabledWageCeiling') IS NULL
    ALTER TABLE stat.EsiRate ADD DisabledWageCeiling DECIMAL(12,2) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_EsiRate_DisabledCeiling' AND parent_object_id = OBJECT_ID(N'stat.EsiRate'))
    EXEC (N'ALTER TABLE stat.EsiRate ADD CONSTRAINT CK_EsiRate_DisabledCeiling CHECK (DisabledWageCeiling IS NULL OR DisabledWageCeiling >= WageCeiling)');
GO
DECLARE @Note NVARCHAR(120) = N' Disabled ceiling 25,000 added 0416 (esic.gov.in coverage page): CA to confirm.';
UPDATE stat.EsiRate
SET DisabledWageCeiling = 25000, SourceRef = LEFT(ISNULL(SourceRef, N''), 300 - LEN(@Note)) + @Note
WHERE EffectiveFrom = '2019-07-01' AND DisabledWageCeiling IS NULL;
GO
CREATE OR ALTER PROCEDURE stat.usp_EsiRate_Get @AsOf DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) EffectiveFrom, EmployeePct, EmployerPct, WageCeiling, DailyWageExemption, RoundingID, Verified, SourceRef, DisabledWageCeiling
    FROM stat.EsiRate
    WHERE EffectiveFrom <= @AsOf
    ORDER BY EffectiveFrom DESC;
END
GO
