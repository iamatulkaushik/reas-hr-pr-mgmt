/* 0415_stat_tds_rule_value.sql
   CR 0018 addendum. Single numbers a tax rule needs that are neither slabs nor rupee caps, by key and from a financial year on:
   HRA_RENT_SHARE_PCT (the share of basic plus DA that rent must exceed for HRA, a percent) and LANDLORD_PAN_RENT (the yearly rent above which the
   landlord's PAN is needed, rupees). A new such number is a new row, never code. FromFinancialYear is the start year (2026 means 2026-27); the row in
   force is the latest FromFinancialYear on or before the year asked (so a value carries forward until a row changes it, and arrears into an earlier year
   read the earlier row). Signed off row by row (Verified, VerifiedOn, SourceRef). No CompanyID, no tenant policy, read procedure only.
   Rows are loaded Verified = 0 with the values of the 1961 Act and its Rules carried forward from 2020; the CA confirms them for the 2025 Act and signs
   them. They cannot be used in a real run until then. Nothing here is a sign-off.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'stat.TdsRuleValue', N'U') IS NULL
BEGIN
    CREATE TABLE stat.TdsRuleValue
    (
        ValueKey          NVARCHAR(40)  NOT NULL CONSTRAINT CK_TdsRuleValue_Key CHECK (LEN(LTRIM(RTRIM(ValueKey))) > 0),
        FromFinancialYear SMALLINT      NOT NULL,
        Amount            DECIMAL(14,4) NOT NULL CONSTRAINT CK_TdsRuleValue_Amount CHECK (Amount >= 0),
        Note              NVARCHAR(300) NULL,
        Verified          BIT           NOT NULL CONSTRAINT DF_TdsRuleValue_Verified DEFAULT 0,
        VerifiedOn        DATE          NULL,
        SourceRef         NVARCHAR(300) NULL,
        CONSTRAINT PK_TdsRuleValue PRIMARY KEY (ValueKey, FromFinancialYear)
    );
END
GO
CREATE OR ALTER PROCEDURE stat.usp_TdsRuleValue_Get @ValueKey NVARCHAR(40), @FinancialYear SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) ValueKey, FromFinancialYear, Amount, Note, Verified, VerifiedOn, SourceRef
    FROM stat.TdsRuleValue
    WHERE ValueKey = @ValueKey AND FromFinancialYear <= @FinancialYear
    ORDER BY FromFinancialYear DESC;
END
GO
DECLARE @Src NVARCHAR(120) = N'Carried from the 1961 Act and Rules, CA to confirm for the 2025 Act (CR 0018 addendum)';

INSERT stat.TdsRuleValue (ValueKey, FromFinancialYear, Amount, Note, Verified, SourceRef)
SELECT v.ValueKey, v.FromFinancialYear, v.Amount, v.Note, 0, @Src
FROM (VALUES
    (N'HRA_RENT_SHARE_PCT', CAST(2020 AS SMALLINT), CAST(10 AS DECIMAL(14,4)),     N'Rent paid minus this percent of basic plus DA, one of the three amounts HRA exemption takes the least of. Percent.'),
    (N'LANDLORD_PAN_RENT',  CAST(2020 AS SMALLINT), CAST(100000 AS DECIMAL(14,4)), N'Yearly rent above which the landlord''s PAN is needed on the declaration. Rupees.')
) AS v (ValueKey, FromFinancialYear, Amount, Note)
WHERE NOT EXISTS (SELECT 1 FROM stat.TdsRuleValue t WHERE t.ValueKey = v.ValueKey AND t.FromFinancialYear = v.FromFinancialYear);
GO
