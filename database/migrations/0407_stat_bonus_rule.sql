/* 0407_stat_bonus_rule.sql
   CR 0012 #3. The statutory bonus rule in force from a date: the wage ceiling for eligibility, the wage ceiling the bonus is calculated on,
   the minimum and maximum bonus percentage, and the days an employee must have worked in the year. A ceiling of 0 means there is no ceiling.
   Signed off row by row (Verified, VerifiedOn, SourceRef). No CompanyID, no tenant policy, read procedure only.
   Loaded with Verified = 0 and flipped after the CA signs off. No values are seeded here.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'stat.BonusRule', N'U') IS NULL
BEGIN
    CREATE TABLE stat.BonusRule
    (
        EffectiveFrom          DATE          NOT NULL CONSTRAINT PK_BonusRule PRIMARY KEY,
        EligibilityWageCeiling DECIMAL(12,2) NOT NULL CONSTRAINT CK_BonusRule_Eligibility CHECK (EligibilityWageCeiling >= 0),
        CalcWageCeiling        DECIMAL(12,2) NOT NULL CONSTRAINT CK_BonusRule_Calc CHECK (CalcWageCeiling >= 0),
        MinPct                 DECIMAL(5,2)  NOT NULL CONSTRAINT CK_BonusRule_MinPct CHECK (MinPct BETWEEN 0 AND 100),
        MaxPct                 DECIMAL(5,2)  NOT NULL CONSTRAINT CK_BonusRule_MaxPct CHECK (MaxPct BETWEEN 0 AND 100),
        MinDaysWorked          SMALLINT      NOT NULL CONSTRAINT CK_BonusRule_Days CHECK (MinDaysWorked BETWEEN 0 AND 366),
        Verified               BIT           NOT NULL CONSTRAINT DF_BonusRule_Verified DEFAULT 0,
        VerifiedOn             DATE          NULL,
        SourceRef              NVARCHAR(300) NULL,
        CONSTRAINT CK_BonusRule_Range CHECK (MaxPct >= MinPct)
    );
END
GO
CREATE OR ALTER PROCEDURE stat.usp_BonusRule_Get @AsOf DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) EffectiveFrom, EligibilityWageCeiling, CalcWageCeiling, MinPct, MaxPct, MinDaysWorked, Verified, VerifiedOn, SourceRef
    FROM stat.BonusRule
    WHERE EffectiveFrom <= @AsOf
    ORDER BY EffectiveFrom DESC;
END
GO
