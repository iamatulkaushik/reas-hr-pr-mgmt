/* 0405_stat_tds_deduction_limit.sql
   CR 0012 #1. Deduction limits for TDS, one row per financial year, regime, section and sub-limit (for example 80C, or 80D with SubLimitKey 'self'
   and 'senior parent'). A new section is a new row, never code. Like stat.TdsParam the rows belong to a financial year and regime, so they have no
   EffectiveFrom and are signed off row by row (Verified, VerifiedOn, SourceRef). No CompanyID, no tenant policy, read procedure only.
   MaxAmount is the rupee cap. A section with no row is not allowed: the calculator treats it as a deduction of zero, never as unlimited.
   Rows are loaded with Verified = 0 and flipped after the CA signs off. No amounts are seeded here.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'stat.TdsDeductionLimit', N'U') IS NULL
BEGIN
    CREATE TABLE stat.TdsDeductionLimit
    (
        FinancialYear SMALLINT      NOT NULL,
        RegimeID      TINYINT       NOT NULL,
        SectionCode   NVARCHAR(20)  NOT NULL CONSTRAINT CK_TdsDeductionLimit_Section CHECK (LEN(LTRIM(RTRIM(SectionCode))) > 0),
        SubLimitKey   NVARCHAR(40)  NOT NULL CONSTRAINT DF_TdsDeductionLimit_SubKey DEFAULT N'',
        MaxAmount     DECIMAL(14,2) NOT NULL CONSTRAINT CK_TdsDeductionLimit_Max CHECK (MaxAmount >= 0),
        Note          NVARCHAR(300) NULL,
        Verified      BIT           NOT NULL CONSTRAINT DF_TdsDeductionLimit_Verified DEFAULT 0,
        VerifiedOn    DATE          NULL,
        SourceRef     NVARCHAR(300) NULL,
        CONSTRAINT PK_TdsDeductionLimit PRIMARY KEY (FinancialYear, RegimeID, SectionCode, SubLimitKey),
        CONSTRAINT FK_TdsDeductionLimit_Param FOREIGN KEY (FinancialYear, RegimeID) REFERENCES stat.TdsParam (FinancialYear, RegimeID)
    );
END
GO
CREATE OR ALTER PROCEDURE stat.usp_TdsDeductionLimit_List @FinancialYear SMALLINT, @RegimeID TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT FinancialYear, RegimeID, SectionCode, SubLimitKey, MaxAmount, Note, Verified, VerifiedOn, SourceRef
    FROM stat.TdsDeductionLimit
    WHERE FinancialYear = @FinancialYear AND RegimeID = @RegimeID
    ORDER BY SectionCode, SubLimitKey;
END
GO
