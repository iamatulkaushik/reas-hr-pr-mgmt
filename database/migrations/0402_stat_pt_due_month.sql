/* 0402_stat_pt_due_month.sql
   CR 0011. The months in which professional tax is due under a half-yearly or yearly state rule. A monthly rule needs no rows
   (every month is due). The slab TaxAmount is the amount per due month. Rows belong to a stat.PtRule row and are signed off with it.
   Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'stat.PtDueMonth', N'U') IS NULL
BEGIN
    CREATE TABLE stat.PtDueMonth
    (
        StateID       TINYINT NOT NULL,
        EffectiveFrom DATE    NOT NULL,
        MonthNo       TINYINT NOT NULL CONSTRAINT CK_PtDueMonth_Month CHECK (MonthNo BETWEEN 1 AND 12),
        CONSTRAINT PK_PtDueMonth PRIMARY KEY (StateID, EffectiveFrom, MonthNo),
        CONSTRAINT FK_PtDueMonth_Rule FOREIGN KEY (StateID, EffectiveFrom) REFERENCES stat.PtRule (StateID, EffectiveFrom)
    );
END
GO
CREATE OR ALTER PROCEDURE stat.usp_PtDueMonth_List @StateID TINYINT, @AsOf DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.MonthNo
    FROM stat.PtDueMonth d
    WHERE d.StateID = @StateID
      AND d.EffectiveFrom = (SELECT MAX(r.EffectiveFrom) FROM stat.PtRule r WHERE r.StateID = @StateID AND r.EffectiveFrom <= @AsOf)
    ORDER BY d.MonthNo;
END
GO
