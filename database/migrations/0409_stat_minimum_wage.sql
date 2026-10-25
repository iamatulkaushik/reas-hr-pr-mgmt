/* 0409_stat_minimum_wage.sql
   CR 0012 #6. The statutory minimum wage of a state in force from a date, by zone and skill, as a day or a month amount. The check in payroll is
   a warning only; the service returns the rows and the Payroll module decides. SkillID: 1 unskilled, 2 semi-skilled, 3 skilled, 4 highly skilled.
   UnitID: 1 per day, 2 per month. ZoneNo 1 is used where a state has no zones.
   Loaded: Haryana from 2026-04-01 (Gazette Extraordinary No. 51-2026/Ext., 9 April 2026, Notification 2/25/26-2 Lab; docs/sources/).
   Amounts are the notified basic minimum rates (not to be split into allowances). Verified = 0 until the CA signs.
   Signed off row by row. No CompanyID, no tenant policy, read procedure only. Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'stat.MinimumWage', N'U') IS NULL
BEGIN
    CREATE TABLE stat.MinimumWage
    (
        StateID       TINYINT       NOT NULL CONSTRAINT FK_MinimumWage_State REFERENCES mst.[State] (StateID),
        EffectiveFrom DATE          NOT NULL,
        ZoneNo        TINYINT       NOT NULL CONSTRAINT CK_MinimumWage_Zone CHECK (ZoneNo >= 1),
        SkillID       TINYINT       NOT NULL CONSTRAINT CK_MinimumWage_Skill CHECK (SkillID IN (1, 2, 3, 4)),
        UnitID        TINYINT       NOT NULL CONSTRAINT CK_MinimumWage_Unit CHECK (UnitID IN (1, 2)),
        Amount        DECIMAL(12,2) NOT NULL CONSTRAINT CK_MinimumWage_Amount CHECK (Amount > 0),
        Verified      BIT           NOT NULL CONSTRAINT DF_MinimumWage_Verified DEFAULT 0,
        VerifiedOn    DATE          NULL,
        SourceRef     NVARCHAR(300) NULL,
        CONSTRAINT PK_MinimumWage PRIMARY KEY (StateID, EffectiveFrom, ZoneNo, SkillID, UnitID)
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM stat.MinimumWage WHERE StateID = 6 AND EffectiveFrom = '2026-04-01')
BEGIN
    DECLARE @Src NVARCHAR(300) = N'Haryana Gazette Extraordinary No. 51-2026/Ext., 9 April 2026, Notification 2/25/26-2 Lab (docs/sources/haryana-minimum-wage-2026-04-09.pdf)';
    INSERT stat.MinimumWage (StateID, EffectiveFrom, ZoneNo, SkillID, UnitID, Amount, Verified, SourceRef)
    VALUES (6, '2026-04-01', 1, 1, 2, 15220.71, 0, @Src), (6, '2026-04-01', 1, 1, 1, 585.41, 0, @Src),
           (6, '2026-04-01', 1, 2, 2, 16780.74, 0, @Src), (6, '2026-04-01', 1, 2, 1, 645.41, 0, @Src),
           (6, '2026-04-01', 1, 3, 2, 18500.81, 0, @Src), (6, '2026-04-01', 1, 3, 1, 711.56, 0, @Src),
           (6, '2026-04-01', 1, 4, 2, 19425.85, 0, @Src), (6, '2026-04-01', 1, 4, 1, 747.14, 0, @Src);
END
GO
CREATE OR ALTER PROCEDURE stat.usp_MinimumWage_List @StateID TINYINT, @AsOf DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @From DATE = (SELECT MAX(EffectiveFrom) FROM stat.MinimumWage WHERE StateID = @StateID AND EffectiveFrom <= @AsOf);
    SELECT StateID, EffectiveFrom, ZoneNo, SkillID, UnitID, Amount, Verified, VerifiedOn, SourceRef
    FROM stat.MinimumWage
    WHERE StateID = @StateID AND EffectiveFrom = @From
    ORDER BY ZoneNo, SkillID, UnitID;
END
GO
