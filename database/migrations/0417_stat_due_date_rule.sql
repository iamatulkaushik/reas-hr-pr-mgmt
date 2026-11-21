/* 0417_stat_due_date_rule.sql
   ADR 0027 point 6, work order docs/phase7b-plan-sigma.md. Statutory lane 0400-0499. Idempotent. Never edit after merge: add a new number.
   Due dates are data. stat.DueDateRule gives the date a monthly item is due: Kind (ComplianceKind: 1 EPF, 2 ESIC, 3 professional tax, 4 TDS, 5 labour welfare),
   StateID (NULL = national; a state rule beats the national one), Frequency (M monthly; Q, H and Y are kept for the state tables and give no date here),
   DueDay and MonthsAfter (1 = the month after the period), an optional special month (tax deducted from salary for March is due on 30 April: SpecialMonth 3,
   SpecialDueDay 30, SpecialMonthsAfter 1), EffectiveFrom and EffectiveTo, IsVerified and SourceNote. A rule with IsVerified = 0 never produces a date.
   A day later than the last day of the month falls on the last day. No CompanyID, no tenant policy, read procedure only (like the other stat tables).
   Loaded as proposed, all unverified: EPF and ESIC by the 15th of the next month, tax deducted from salary by the 7th of the next month and 30 April for March.
   Professional tax and labour welfare are not loaded here: they keep their dates in stat.PtDueMonth and stat.LabourWelfareRule. For the CA: docs/ca-signoff/due-date-rules-2026-11-20.md. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'stat.DueDateRule', N'U') IS NULL
BEGIN
    CREATE TABLE stat.DueDateRule
    (
        RuleID             INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_DueDateRule PRIMARY KEY,
        Kind               TINYINT       NOT NULL CONSTRAINT CK_DueDateRule_Kind CHECK (Kind BETWEEN 1 AND 5),
        StateID            TINYINT       NULL CONSTRAINT FK_DueDateRule_State REFERENCES mst.[State] (StateID),
        Frequency          CHAR(1)       NOT NULL CONSTRAINT CK_DueDateRule_Frequency CHECK (Frequency IN ('M', 'Q', 'H', 'Y')),
        DueDay             TINYINT       NOT NULL CONSTRAINT CK_DueDateRule_DueDay CHECK (DueDay BETWEEN 1 AND 31),
        MonthsAfter        TINYINT       NOT NULL CONSTRAINT CK_DueDateRule_MonthsAfter CHECK (MonthsAfter BETWEEN 0 AND 12),
        SpecialMonth       TINYINT       NULL CONSTRAINT CK_DueDateRule_SpecialMonth CHECK (SpecialMonth BETWEEN 1 AND 12),
        SpecialDueDay      TINYINT       NULL CONSTRAINT CK_DueDateRule_SpecialDay CHECK (SpecialDueDay BETWEEN 1 AND 31),
        SpecialMonthsAfter TINYINT       NULL CONSTRAINT CK_DueDateRule_SpecialAfter CHECK (SpecialMonthsAfter BETWEEN 0 AND 12),
        EffectiveFrom      DATE          NOT NULL,
        EffectiveTo        DATE          NULL,
        IsVerified         BIT           NOT NULL CONSTRAINT DF_DueDateRule_IsVerified DEFAULT 0,
        SourceNote         NVARCHAR(200) NULL,
        StateKey           AS CAST(ISNULL(StateID, 0) AS TINYINT) PERSISTED,
        CONSTRAINT CK_DueDateRule_Special CHECK ((SpecialMonth IS NULL AND SpecialDueDay IS NULL AND SpecialMonthsAfter IS NULL)
                                              OR (SpecialMonth IS NOT NULL AND SpecialDueDay IS NOT NULL AND SpecialMonthsAfter IS NOT NULL)),
        CONSTRAINT CK_DueDateRule_Dates CHECK (EffectiveTo IS NULL OR EffectiveTo >= EffectiveFrom),
        CONSTRAINT UQ_DueDateRule UNIQUE (Kind, StateKey, EffectiveFrom)
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM stat.DueDateRule WHERE Kind = 1 AND StateID IS NULL)
    INSERT stat.DueDateRule (Kind, StateID, Frequency, DueDay, MonthsAfter, EffectiveFrom, IsVerified, SourceNote)
    VALUES (1, NULL, 'M', 15, 1, '2020-01-01', 0, N'Proposed: EPF contribution by the 15th of the next month. CA to verify (ADR 0027).');
IF NOT EXISTS (SELECT 1 FROM stat.DueDateRule WHERE Kind = 2 AND StateID IS NULL)
    INSERT stat.DueDateRule (Kind, StateID, Frequency, DueDay, MonthsAfter, EffectiveFrom, IsVerified, SourceNote)
    VALUES (2, NULL, 'M', 15, 1, '2020-01-01', 0, N'Proposed: ESIC contribution by the 15th of the next month. CA to verify (ADR 0027).');
IF NOT EXISTS (SELECT 1 FROM stat.DueDateRule WHERE Kind = 4 AND StateID IS NULL)
    INSERT stat.DueDateRule (Kind, StateID, Frequency, DueDay, MonthsAfter, SpecialMonth, SpecialDueDay, SpecialMonthsAfter, EffectiveFrom, IsVerified, SourceNote)
    VALUES (4, NULL, 'M', 7, 1, 3, 30, 1, '2020-01-01', 0, N'Proposed: tax deducted from salary by the 7th of the next month; 30 April for March. CA to verify (ADR 0027).');
GO
/* The due date of a monthly item for a period (the first of the month). One row always:
   DueDate (NULL when there is no rule, the rule is not verified, or its frequency is not monthly), HasRule (a rule was found), IsVerified, SourceNote.
   A state rule beats the national rule; of the rules in force on the period, the latest EffectiveFrom wins. */
CREATE OR ALTER PROCEDURE stat.usp_DueDate_Resolve @Kind TINYINT, @StateID TINYINT = NULL, @PeriodStart DATE
AS
BEGIN
    SET NOCOUNT ON;
    IF @PeriodStart IS NULL OR DAY(@PeriodStart) <> 1 THROW 53650, N'The month must start on the first day.', 1;
    DECLARE @Frequency CHAR(1), @DueDay TINYINT, @After TINYINT, @SpecialMonth TINYINT, @SpecialDay TINYINT, @SpecialAfter TINYINT,
            @Verified BIT, @Note NVARCHAR(200), @Found BIT = 0;
    SELECT TOP (1) @Found = 1, @Frequency = r.Frequency, @DueDay = r.DueDay, @After = r.MonthsAfter, @SpecialMonth = r.SpecialMonth,
           @SpecialDay = r.SpecialDueDay, @SpecialAfter = r.SpecialMonthsAfter, @Verified = r.IsVerified, @Note = r.SourceNote
    FROM stat.DueDateRule r
    WHERE r.Kind = @Kind AND (r.StateID = @StateID OR r.StateID IS NULL)
      AND r.EffectiveFrom <= @PeriodStart AND (r.EffectiveTo IS NULL OR r.EffectiveTo >= @PeriodStart)
    ORDER BY CASE WHEN r.StateID IS NULL THEN 1 ELSE 0 END, r.EffectiveFrom DESC;
    DECLARE @Due DATE = NULL;
    IF @Found = 1 AND @Verified = 1 AND @Frequency = 'M'
    BEGIN
        DECLARE @Special BIT = CASE WHEN @SpecialMonth IS NOT NULL AND MONTH(@PeriodStart) = @SpecialMonth THEN 1 ELSE 0 END;
        DECLARE @Target DATE = DATEADD(MONTH, CASE WHEN @Special = 1 THEN @SpecialAfter ELSE @After END, @PeriodStart);
        DECLARE @Day INT = CASE WHEN @Special = 1 THEN @SpecialDay ELSE @DueDay END;
        DECLARE @Last INT = DAY(EOMONTH(@Target));
        SET @Due = DATEFROMPARTS(YEAR(@Target), MONTH(@Target), CASE WHEN @Day > @Last THEN @Last ELSE @Day END);
    END
    SELECT @Due AS DueDate, @Found AS HasRule, ISNULL(@Verified, CAST(0 AS BIT)) AS IsVerified, @Note AS SourceNote;
END
GO
