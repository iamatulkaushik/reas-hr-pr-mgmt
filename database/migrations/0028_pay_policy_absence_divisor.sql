/* 0028_pay_policy_absence_divisor.sql
   CR 0017 (Haryana gazette, Owner: "go as per gazette"). The gazette divides the monthly wage by 26 for the per-day wage (pay.CompanyPolicy.DailyWageDivisor,
   default 26) and by 30 for a deduction such as absence. This adds the second divisor, AbsenceDeductionDivisor, default 30, to pay.CompanyPolicy.
   Existing companies get 30. usp_Policy_Get returns the new column last, so the current reader keeps working. usp_Policy_Save gets the new parameter
   as optional: NULL keeps the stored value (30 for a new row), so the current Brave repository keeps working until it sends the value.
   Idempotent. Never edit after merge: add a new number. */
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
