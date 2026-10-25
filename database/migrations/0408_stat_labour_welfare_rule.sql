/* 0408_stat_labour_welfare_rule.sql
   CR 0012 #5. The labour welfare fund rule of a state in force from a date. Haryana is a percentage with a cap and an employer share of twice
   the employee share, so the rule has: EmployeePct (0 means a flat amount), EmployeeAmount (the flat amount, or the cap when EmployeePct > 0;
   0 = no cap), EmployerMultiple (employer pays this many times the employee amount; 0 means use EmployerAmount), EmployerAmount (flat),
   FrequencyID (1 monthly, 2 half-yearly, 3 yearly), WageCutoff (0 = none) and DueMonths (comma list of month numbers).
   Signed off row by row (Verified, VerifiedOn, SourceRef). No CompanyID, no tenant policy, read procedure only.
   Loaded: Haryana from 2019-04-01, from docs/sources/haryana-lwf.md (Haryana Labour Welfare Board page). Verified = 0 until the CA signs.
   The page gives rates per month; how often the fund is paid and its due date are NOT on the page, so FrequencyID 1 and all months are
   the page's reading and are for the CA to confirm.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'stat.LabourWelfareRule', N'U') IS NULL
BEGIN
    CREATE TABLE stat.LabourWelfareRule
    (
        StateID          TINYINT       NOT NULL CONSTRAINT FK_LabourWelfareRule_State REFERENCES mst.[State] (StateID),
        EffectiveFrom    DATE          NOT NULL,
        EmployeePct      DECIMAL(5,2)  NOT NULL CONSTRAINT CK_LabourWelfareRule_Pct CHECK (EmployeePct BETWEEN 0 AND 100),
        EmployeeAmount   DECIMAL(10,2) NOT NULL CONSTRAINT CK_LabourWelfareRule_EmpAmt CHECK (EmployeeAmount >= 0),
        EmployerMultiple DECIMAL(5,2)  NOT NULL CONSTRAINT CK_LabourWelfareRule_Mult CHECK (EmployerMultiple >= 0),
        EmployerAmount   DECIMAL(10,2) NOT NULL CONSTRAINT CK_LabourWelfareRule_ErAmt CHECK (EmployerAmount >= 0),
        FrequencyID      TINYINT       NOT NULL CONSTRAINT CK_LabourWelfareRule_Freq CHECK (FrequencyID IN (1, 2, 3)),
        WageCutoff       DECIMAL(12,2) NOT NULL CONSTRAINT CK_LabourWelfareRule_Cutoff CHECK (WageCutoff >= 0),
        DueMonths        NVARCHAR(40)  NOT NULL CONSTRAINT CK_LabourWelfareRule_Months CHECK (LEN(DueMonths) > 0 AND DueMonths NOT LIKE N'%[^0-9,]%'),
        Verified         BIT           NOT NULL CONSTRAINT DF_LabourWelfareRule_Verified DEFAULT 0,
        VerifiedOn       DATE          NULL,
        SourceRef        NVARCHAR(300) NULL,
        CONSTRAINT PK_LabourWelfareRule PRIMARY KEY (StateID, EffectiveFrom)
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM stat.LabourWelfareRule WHERE StateID = 6 AND EffectiveFrom = '2019-04-01')
    INSERT stat.LabourWelfareRule (StateID, EffectiveFrom, EmployeePct, EmployeeAmount, EmployerMultiple, EmployerAmount, FrequencyID, WageCutoff, DueMonths, Verified, SourceRef)
    VALUES (6, '2019-04-01', 0.20, 25.00, 2.00, 0.00, 1, 0.00, N'1,2,3,4,5,6,7,8,9,10,11,12', 0,
            N'Haryana Labour Welfare Board page, Punjab Labour Welfare Fund Act 1965 s.9-A, from April 2019 (docs/sources/haryana-lwf.md). Frequency and due date to be confirmed by the CA.');
GO
CREATE OR ALTER PROCEDURE stat.usp_LabourWelfareRule_Get @StateID TINYINT, @AsOf DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) StateID, EffectiveFrom, EmployeePct, EmployeeAmount, EmployerMultiple, EmployerAmount, FrequencyID, WageCutoff, DueMonths,
                   Verified, VerifiedOn, SourceRef
    FROM stat.LabourWelfareRule
    WHERE StateID = @StateID AND EffectiveFrom <= @AsOf
    ORDER BY EffectiveFrom DESC;
END
GO
