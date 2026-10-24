/* 0404_stat_gratuity_proc.sql
   CR 0013: read procedure for stat.GratuityRule (table from 0400). Returns the row in force on @AsOf, or no row.
   The service fails on no row and on an unverified row; this procedure never supplies a default.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE stat.usp_GratuityRule_Get @AsOf DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) EffectiveFrom, MinYearsRegular, MinYearsFixedTerm, DaysPerYear, MonthDivisor,
           RoundUpAfterMonths, CapAmount, Verified, VerifiedOn, SourceRef
    FROM stat.GratuityRule
    WHERE EffectiveFrom <= @AsOf
    ORDER BY EffectiveFrom DESC;
END
GO
