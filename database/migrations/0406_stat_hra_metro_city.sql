/* 0406_stat_hra_metro_city.sql
   CR 0012 #2. The cities whose HRA exemption is 50 percent (metro) and the percentage for every other city, as a list in force from a date.
   A list is all the rows that share the latest EffectiveFrom on or before the date asked for. CityName is matched exactly, ignoring case and
   surrounding spaces, against the City of the unit an employee works in (units and employees keep the city as free text).
   The row with CityName '*' is the percentage for every city not listed; the service fails when a list has no '*' row, so no percentage is
   ever assumed in code. Rows are loaded with Verified = 0 and flipped after the CA signs off. No rows are seeded here.
   No CompanyID, no tenant policy, read procedure only.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'stat.HraMetroCity', N'U') IS NULL
BEGIN
    CREATE TABLE stat.HraMetroCity
    (
        EffectiveFrom DATE          NOT NULL,
        CityName      NVARCHAR(100) NOT NULL CONSTRAINT CK_HraMetroCity_Name CHECK (LEN(LTRIM(RTRIM(CityName))) > 0),
        IsMetro       BIT           NOT NULL,
        ExemptPct     DECIMAL(5,2)  NOT NULL CONSTRAINT CK_HraMetroCity_Pct CHECK (ExemptPct BETWEEN 0 AND 100),
        Verified      BIT           NOT NULL CONSTRAINT DF_HraMetroCity_Verified DEFAULT 0,
        VerifiedOn    DATE          NULL,
        SourceRef     NVARCHAR(300) NULL,
        CONSTRAINT PK_HraMetroCity PRIMARY KEY (EffectiveFrom, CityName)
    );
END
GO
CREATE OR ALTER PROCEDURE stat.usp_HraMetroCity_List @AsOf DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @From DATE = (SELECT MAX(EffectiveFrom) FROM stat.HraMetroCity WHERE EffectiveFrom <= @AsOf);
    SELECT EffectiveFrom, LTRIM(RTRIM(CityName)) AS CityName, IsMetro, ExemptPct, Verified, VerifiedOn, SourceRef
    FROM stat.HraMetroCity
    WHERE EffectiveFrom = @From
    ORDER BY CityName;
END
GO
