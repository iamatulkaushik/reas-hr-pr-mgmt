/* 0401_stat_procs.sql
   Read procedures for the statutory tables. CREATE OR ALTER keeps it idempotent. The app can only read: rows are added by migrations.
   Each procedure returns the row in force on the date asked (latest EffectiveFrom on or before it) or no row. The service must treat
   no row as an error ("no rate configured"), and must refuse rows with Verified = 0 while cfg.Setting RateVerificationRequired is 1. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE stat.usp_EpfRate_Get @AsOf DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) EffectiveFrom, EmployeePct, EmployerEpfPct, EmployerEpsPct, EdliPct, AdminPct, AdminMinimum, WageCeiling, EdliWageCeiling, Verified, SourceRef
    FROM stat.EpfRate
    WHERE EffectiveFrom <= @AsOf
    ORDER BY EffectiveFrom DESC;
END
GO
CREATE OR ALTER PROCEDURE stat.usp_EsiRate_Get @AsOf DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) EffectiveFrom, EmployeePct, EmployerPct, WageCeiling, DailyWageExemption, RoundingID, Verified, SourceRef
    FROM stat.EsiRate
    WHERE EffectiveFrom <= @AsOf
    ORDER BY EffectiveFrom DESC;
END
GO
CREATE OR ALTER PROCEDURE stat.usp_PtRule_Get @AsOf DATE, @StateID TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) StateID, EffectiveFrom, FrequencyID, SpecialMonth, Verified, SourceRef
    FROM stat.PtRule
    WHERE StateID = @StateID AND EffectiveFrom <= @AsOf
    ORDER BY EffectiveFrom DESC;
END
GO
CREATE OR ALTER PROCEDURE stat.usp_OvertimeLimit_Get @AsOf DATE, @StateID TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) StateID, EffectiveFrom, DailyMaxMinutes, WeeklyMaxMinutes, QuarterlyMaxMinutes, Verified, SourceRef
    FROM stat.OvertimeLimit
    WHERE StateID = @StateID AND EffectiveFrom <= @AsOf
    ORDER BY EffectiveFrom DESC;
END
GO
CREATE OR ALTER PROCEDURE stat.usp_GratuityRule_Get @AsOf DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) EffectiveFrom, MinYearsRegular, MinYearsFixedTerm, DaysPerYear, MonthDivisor, RoundUpAfterMonths, CapAmount, Verified, SourceRef
    FROM stat.GratuityRule
    WHERE EffectiveFrom <= @AsOf
    ORDER BY EffectiveFrom DESC;
END
GO
CREATE OR ALTER PROCEDURE stat.usp_WageRule_Get @AsOf DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) EffectiveFrom, UseCodeWage, WageFloorPct, Notes, Verified, SourceRef
    FROM stat.WageRule
    WHERE EffectiveFrom <= @AsOf
    ORDER BY EffectiveFrom DESC;
END
GO
CREATE OR ALTER PROCEDURE stat.usp_PtSlab_List @StateID TINYINT, @AsOf DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @From DATE = (SELECT TOP (1) EffectiveFrom FROM stat.PtRule WHERE StateID = @StateID AND EffectiveFrom <= @AsOf ORDER BY EffectiveFrom DESC);
    SELECT StateID, EffectiveFrom, GenderID, SlabNo, FromAmount, ToAmount, TaxAmount, SpecialMonthTax
    FROM stat.PtSlab
    WHERE StateID = @StateID AND EffectiveFrom = @From
    ORDER BY GenderID, SlabNo;
END
GO
CREATE OR ALTER PROCEDURE stat.usp_TdsParam_Get @FinancialYear SMALLINT, @RegimeID TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT FinancialYear, RegimeID, StandardDeduction, RebateIncomeLimit, RebateMaximum, CessPct, Verified, SourceRef
    FROM stat.TdsParam
    WHERE FinancialYear = @FinancialYear AND RegimeID = @RegimeID;
END
GO
CREATE OR ALTER PROCEDURE stat.usp_TdsSlab_List @FinancialYear SMALLINT, @RegimeID TINYINT, @AgeBandID TINYINT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SlabNo, FromAmount, ToAmount, RatePct
    FROM stat.TdsSlab
    WHERE FinancialYear = @FinancialYear AND RegimeID = @RegimeID AND AgeBandID = @AgeBandID
    ORDER BY SlabNo;
END
GO
CREATE OR ALTER PROCEDURE stat.usp_TdsSurcharge_List @FinancialYear SMALLINT, @RegimeID TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SlabNo, FromIncome, ToIncome, RatePct
    FROM stat.TdsSurcharge
    WHERE FinancialYear = @FinancialYear AND RegimeID = @RegimeID
    ORDER BY SlabNo;
END
GO
