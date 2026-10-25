/* 0414_stat_tds_employer_nps.sql
   CR 0018. The employer's NPS contribution exempt from TDS under section 80CCD(2), as a percent of basic plus DA, per financial year and regime
   (FY 2026: 14 percent new regime, 10 percent old regime). It is a percent, not a rupee cap, so it is not in stat.TdsDeductionLimit. It is a table of
   its own, not a column on stat.TdsParam, because the TdsParam rows are already signed off by the CA (0411): a new column on a signed row would look
   signed when its value is not. Signed off row by row (Verified, VerifiedOn, SourceRef). No CompanyID, no tenant policy, read procedure only.
   Rows are loaded Verified = 0 from docs/rates/income-tax-2026-forms-and-rules.md and CR 0018 (secondary sources): they cannot be used in a real run
   until the CA signs them. A row is inserted only when its stat.TdsParam row exists.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'stat.TdsEmployerNps', N'U') IS NULL
BEGIN
    CREATE TABLE stat.TdsEmployerNps
    (
        FinancialYear SMALLINT      NOT NULL,
        RegimeID      TINYINT       NOT NULL,
        PctOfBasicDa  DECIMAL(5,2)  NOT NULL CONSTRAINT CK_TdsEmployerNps_Pct CHECK (PctOfBasicDa BETWEEN 0 AND 100),
        Note          NVARCHAR(300) NULL,
        Verified      BIT           NOT NULL CONSTRAINT DF_TdsEmployerNps_Verified DEFAULT 0,
        VerifiedOn    DATE          NULL,
        SourceRef     NVARCHAR(300) NULL,
        CONSTRAINT PK_TdsEmployerNps PRIMARY KEY (FinancialYear, RegimeID),
        CONSTRAINT FK_TdsEmployerNps_Param FOREIGN KEY (FinancialYear, RegimeID) REFERENCES stat.TdsParam (FinancialYear, RegimeID)
    );
END
GO
CREATE OR ALTER PROCEDURE stat.usp_TdsEmployerNps_Get @FinancialYear SMALLINT, @RegimeID TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT FinancialYear, RegimeID, PctOfBasicDa, Note, Verified, VerifiedOn, SourceRef
    FROM stat.TdsEmployerNps
    WHERE FinancialYear = @FinancialYear AND RegimeID = @RegimeID;
END
GO
DECLARE @Src NVARCHAR(120) = N'docs/rates/income-tax-2026-forms-and-rules.md (secondary sources, CA pending)';

INSERT stat.TdsEmployerNps (FinancialYear, RegimeID, PctOfBasicDa, Note, Verified, SourceRef)
SELECT 2026, v.RegimeID, v.PctOfBasicDa, N'Employer NPS, section 80CCD(2), percent of basic plus DA.', 0, @Src
FROM (VALUES
    (CAST(2 AS TINYINT), CAST(14 AS DECIMAL(5,2))),
    (CAST(1 AS TINYINT), CAST(10 AS DECIMAL(5,2)))
) AS v (RegimeID, PctOfBasicDa)
WHERE EXISTS (SELECT 1 FROM stat.TdsParam p WHERE p.FinancialYear = 2026 AND p.RegimeID = v.RegimeID)
  AND NOT EXISTS (SELECT 1 FROM stat.TdsEmployerNps t WHERE t.FinancialYear = 2026 AND t.RegimeID = v.RegimeID);
GO
