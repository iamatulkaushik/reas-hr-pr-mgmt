/* 0412_stat_tax_form_and_section_ref.sql
   CR 0018. The number of the form and the section reference the law uses for a purpose or topic, from a financial year on, so no screen, report or
   file name types "Form 16" or "192". Tax year 2026-27 uses the Income-tax Act 2025 names (Form 124, 130, 138, section 392); financial years before
   keep the 1961 Act names. FromFinancialYear is the start year (2026 means 2026-27); the row in force is the latest FromFinancialYear on or before the
   year asked. Signed off row by row (Verified, VerifiedOn, SourceRef). No CompanyID, no tenant policy, read procedures only.
   Rows are loaded with Verified = 0 from docs/rates/income-tax-2026-forms-and-rules.md, which rests on secondary sources: they cannot be used in a real
   run until the CA signs them. The CA settles 392 against 393 for salary TDS and confirms Form 122 for the previous employer.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'stat.TaxForm', N'U') IS NULL
BEGIN
    CREATE TABLE stat.TaxForm
    (
        PurposeKey        NVARCHAR(30)  NOT NULL CONSTRAINT CK_TaxForm_Purpose CHECK (LEN(LTRIM(RTRIM(PurposeKey))) > 0),
        FromFinancialYear SMALLINT      NOT NULL,
        FormNo            NVARCHAR(10)  NOT NULL CONSTRAINT CK_TaxForm_FormNo CHECK (LEN(LTRIM(RTRIM(FormNo))) > 0),
        Title             NVARCHAR(100) NOT NULL,
        Verified          BIT           NOT NULL CONSTRAINT DF_TaxForm_Verified DEFAULT 0,
        VerifiedOn        DATE          NULL,
        SourceRef         NVARCHAR(300) NULL,
        CONSTRAINT PK_TaxForm PRIMARY KEY (PurposeKey, FromFinancialYear)
    );
END
GO
IF OBJECT_ID(N'stat.TaxSectionRef', N'U') IS NULL
BEGIN
    CREATE TABLE stat.TaxSectionRef
    (
        TopicKey          NVARCHAR(30)  NOT NULL CONSTRAINT CK_TaxSectionRef_Topic CHECK (LEN(LTRIM(RTRIM(TopicKey))) > 0),
        FromFinancialYear SMALLINT      NOT NULL,
        SectionRef        NVARCHAR(40)  NOT NULL CONSTRAINT CK_TaxSectionRef_Ref CHECK (LEN(LTRIM(RTRIM(SectionRef))) > 0),
        Verified          BIT           NOT NULL CONSTRAINT DF_TaxSectionRef_Verified DEFAULT 0,
        VerifiedOn        DATE          NULL,
        SourceRef         NVARCHAR(300) NULL,
        CONSTRAINT PK_TaxSectionRef PRIMARY KEY (TopicKey, FromFinancialYear)
    );
END
GO
CREATE OR ALTER PROCEDURE stat.usp_TaxForm_Get @PurposeKey NVARCHAR(30), @FinancialYear SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) PurposeKey, FromFinancialYear, FormNo, Title, Verified, VerifiedOn, SourceRef
    FROM stat.TaxForm
    WHERE PurposeKey = @PurposeKey AND FromFinancialYear <= @FinancialYear
    ORDER BY FromFinancialYear DESC;
END
GO
CREATE OR ALTER PROCEDURE stat.usp_TaxSectionRef_Get @TopicKey NVARCHAR(30), @FinancialYear SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) TopicKey, FromFinancialYear, SectionRef, Verified, VerifiedOn, SourceRef
    FROM stat.TaxSectionRef
    WHERE TopicKey = @TopicKey AND FromFinancialYear <= @FinancialYear
    ORDER BY FromFinancialYear DESC;
END
GO
DECLARE @Src NVARCHAR(120) = N'docs/rates/income-tax-2026-forms-and-rules.md (secondary sources, CA pending)';

INSERT stat.TaxForm (PurposeKey, FromFinancialYear, FormNo, Title, Verified, SourceRef)
SELECT v.PurposeKey, v.FromFinancialYear, v.FormNo, v.Title, 0, v.Src
FROM (VALUES
    (N'DECLARATION',   2020, N'12BB', N'Employee declaration of deductions and HRA',   @Src),
    (N'DECLARATION',   2026, N'124',  N'Employee declaration of deductions and HRA',   @Src),
    (N'SALARY_CERT',   2020, N'16',   N'Annual salary TDS certificate',                @Src),
    (N'SALARY_CERT',   2026, N'130',  N'Annual salary TDS certificate',                @Src),
    (N'SALARY_RETURN', 2020, N'24Q',  N'Quarterly salary TDS return',                  @Src),
    (N'SALARY_RETURN', 2026, N'138',  N'Quarterly salary TDS return',                  @Src),
    (N'PREV_EMPLOYER', 2020, N'12B',  N'Previous employer salary and TDS statement',   @Src),
    (N'PREV_EMPLOYER', 2026, N'122',  N'Previous employer salary and TDS statement',   @Src + N'; one source only')
) AS v (PurposeKey, FromFinancialYear, FormNo, Title, Src)
WHERE NOT EXISTS (SELECT 1 FROM stat.TaxForm t WHERE t.PurposeKey = v.PurposeKey AND t.FromFinancialYear = v.FromFinancialYear);

INSERT stat.TaxSectionRef (TopicKey, FromFinancialYear, SectionRef, Verified, SourceRef)
SELECT v.TopicKey, v.FromFinancialYear, v.SectionRef, 0, v.Src
FROM (VALUES
    (N'SALARY_TDS',       2020, N'192',          @Src),
    (N'SALARY_TDS',       2026, N'392',          @Src + N'; 393 also reported, CA to settle'),
    (N'DEDUCTION_INVEST', 2020, N'80C',          @Src),
    (N'DEDUCTION_INVEST', 2026, N'123',          @Src),
    (N'NPS',              2020, N'80CCD',        @Src),
    (N'NPS',              2026, N'124',          @Src),
    (N'HEALTH_INSURANCE', 2020, N'80D',          @Src),
    (N'HEALTH_INSURANCE', 2026, N'126',          @Src),
    (N'SAVINGS_INTEREST', 2020, N'80TTA/80TTB',  @Src),
    (N'SAVINGS_INTEREST', 2026, N'153',          @Src),
    (N'NEW_REGIME',       2020, N'115BAC',       @Src),
    (N'NEW_REGIME',       2026, N'202',          @Src),
    (N'REBATE',           2020, N'87A',          @Src),
    (N'REBATE',           2026, N'156',          @Src)
) AS v (TopicKey, FromFinancialYear, SectionRef, Src)
WHERE NOT EXISTS (SELECT 1 FROM stat.TaxSectionRef t WHERE t.TopicKey = v.TopicKey AND t.FromFinancialYear = v.FromFinancialYear);
GO
