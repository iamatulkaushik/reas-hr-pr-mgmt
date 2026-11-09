/* 0315_pay_policy_procs_after_salary_procs.sql
   ADR 0024. Idempotent. Never edit after merge: add a new number.
   0302 defines usp_Policy_Get and usp_Policy_Save without AbsenceDeductionDivisor; on a new database it runs after 0028, so it would put the old procedures back.
   0300 now creates the column; this script runs after 0302 and sets the final procedures from 0028 (the column is added here too if a database lacks it). */
SET XACT_ABORT ON;
GO
IF COL_LENGTH(N'pay.CompanyPolicy', N'AbsenceDeductionDivisor') IS NULL
BEGIN
    ALTER TABLE pay.CompanyPolicy ADD AbsenceDeductionDivisor DECIMAL(7,2) NOT NULL
        CONSTRAINT DF_CompanyPolicy_AbsenceDiv DEFAULT 30
        CONSTRAINT CK_CompanyPolicy_AbsenceDiv CHECK (AbsenceDeductionDivisor > 0);
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Policy_Get
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.CompanyPolicy WHERE CompanyID = @CompanyID)
        INSERT pay.CompanyPolicy (CompanyID) VALUES (@CompanyID);
    SELECT PayCycleStartDay, OtMultiplier, OtInEpfWage, OtInEsiWage, DailyWageDivisor, EmiCapPct, AbsenceDeductionDivisor
    FROM pay.CompanyPolicy WHERE CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Policy_Save
    @PayCycleStartDay TINYINT, @OtMultiplier DECIMAL(7,4), @OtInEpfWage BIT, @OtInEsiWage BIT,
    @DailyWageDivisor DECIMAL(7,2), @EmiCapPct DECIMAL(7,4), @UserID INT = NULL, @AbsenceDeductionDivisor DECIMAL(7,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    UPDATE pay.CompanyPolicy
    SET PayCycleStartDay = @PayCycleStartDay, OtMultiplier = @OtMultiplier, OtInEpfWage = @OtInEpfWage, OtInEsiWage = @OtInEsiWage,
        DailyWageDivisor = @DailyWageDivisor, EmiCapPct = @EmiCapPct,
        AbsenceDeductionDivisor = COALESCE(@AbsenceDeductionDivisor, AbsenceDeductionDivisor),
        UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE CompanyID = @CompanyID;
    IF @@ROWCOUNT = 0
        INSERT pay.CompanyPolicy (CompanyID, PayCycleStartDay, OtMultiplier, OtInEpfWage, OtInEsiWage, DailyWageDivisor, EmiCapPct, AbsenceDeductionDivisor, UpdatedByUserID)
        VALUES (@CompanyID, @PayCycleStartDay, @OtMultiplier, @OtInEpfWage, @OtInEsiWage, @DailyWageDivisor, @EmiCapPct, COALESCE(@AbsenceDeductionDivisor, 30), @UserID);
END
GO
