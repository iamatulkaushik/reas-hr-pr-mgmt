/* 0339_pay_bonus_tables.sql
   Annual bonus run (step 7f, Bonuses screen). Brave lane 0300-0399 (taken over by Sigma, 2026-11-04). Idempotent. Never edit after merge. Tenant policy is in 0340, procedures in 0341.
   pay.BonusRun marks an off-cycle run (RunNo 2 and up) as the bonus run of one accounting year (April to March, YearFrom is the 1st of April) and keeps the percentage the Owner chose inside the
   rule's range (stat.BonusRule). The bonus itself is the gross of the run's lines; what an employee was already paid for the year is the sum of those lines plus any bonus paid in full and final,
   so a second run for the same year only pays the difference. The run goes through approve, lock and pay like any other. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'pay.BonusRun', N'U') IS NULL
BEGIN
    CREATE TABLE pay.BonusRun
    (
        RunID           INT           NOT NULL CONSTRAINT PK_BonusRun PRIMARY KEY,
        CompanyID       INT           NOT NULL,
        YearFrom        DATE          NOT NULL CONSTRAINT CK_BonusRun_Year CHECK (DAY(YearFrom) = 1 AND MONTH(YearFrom) = 4),
        Pct             DECIMAL(5,2)  NOT NULL CONSTRAINT CK_BonusRun_Pct CHECK (Pct > 0 AND Pct <= 100),
        CreatedUtc      DATETIME2(0)  NOT NULL CONSTRAINT DF_BonusRun_Created DEFAULT SYSUTCDATETIME(),
        CreatedByUserID INT           NULL,
        CONSTRAINT FK_BonusRun_Run FOREIGN KEY (CompanyID, RunID) REFERENCES pay.PayrollRun (CompanyID, RunID)
    );
    CREATE INDEX IX_BonusRun_Year ON pay.BonusRun (CompanyID, YearFrom);
END
GO
