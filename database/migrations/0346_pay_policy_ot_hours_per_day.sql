/* 0346_pay_policy_ot_hours_per_day.sql
   ADR 0026 point 8, work order docs/phase4b-plan-sigma.md. Brave lane 0300-0399. Idempotent. Never edit after merge.
   pay.CompanyPolicy.OtHoursPerDay: the hours of a normal day that monthly overtime hours are converted with when an employee has no shift (monthly attendance has no shifts).
   Default 8. usp_Policy_Get and usp_Policy_Save are re-created from 0315 (their last definition) with the new column; @OtHoursPerDay is optional so older callers keep working. */
SET XACT_ABORT ON;
GO
IF COL_LENGTH(N'pay.CompanyPolicy', N'OtHoursPerDay') IS NULL
BEGIN
    ALTER TABLE pay.CompanyPolicy ADD OtHoursPerDay DECIMAL(4,2) NOT NULL
        CONSTRAINT DF_CompanyPolicy_OtHoursPerDay DEFAULT 8
        CONSTRAINT CK_CompanyPolicy_OtHoursPerDay CHECK (OtHoursPerDay > 0 AND OtHoursPerDay <= 24);
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
    SELECT PayCycleStartDay, OtMultiplier, OtInEpfWage, OtInEsiWage, DailyWageDivisor, EmiCapPct, AbsenceDeductionDivisor, OtHoursPerDay
    FROM pay.CompanyPolicy WHERE CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Policy_Save
    @PayCycleStartDay TINYINT, @OtMultiplier DECIMAL(7,4), @OtInEpfWage BIT, @OtInEsiWage BIT,
    @DailyWageDivisor DECIMAL(7,2), @EmiCapPct DECIMAL(7,4), @UserID INT = NULL, @AbsenceDeductionDivisor DECIMAL(7,2) = NULL,
    @OtHoursPerDay DECIMAL(4,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    UPDATE pay.CompanyPolicy
    SET PayCycleStartDay = @PayCycleStartDay, OtMultiplier = @OtMultiplier, OtInEpfWage = @OtInEpfWage, OtInEsiWage = @OtInEsiWage,
        DailyWageDivisor = @DailyWageDivisor, EmiCapPct = @EmiCapPct,
        AbsenceDeductionDivisor = COALESCE(@AbsenceDeductionDivisor, AbsenceDeductionDivisor),
        OtHoursPerDay = COALESCE(@OtHoursPerDay, OtHoursPerDay),
        UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE CompanyID = @CompanyID;
    IF @@ROWCOUNT = 0
        INSERT pay.CompanyPolicy (CompanyID, PayCycleStartDay, OtMultiplier, OtInEpfWage, OtInEsiWage, DailyWageDivisor, EmiCapPct, AbsenceDeductionDivisor, OtHoursPerDay, UpdatedByUserID)
        VALUES (@CompanyID, @PayCycleStartDay, @OtMultiplier, @OtInEpfWage, @OtInEsiWage, @DailyWageDivisor, @EmiCapPct, COALESCE(@AbsenceDeductionDivisor, 30), COALESCE(@OtHoursPerDay, 8), @UserID);
END
GO
