/* 0334_pay_arrears_head_tables.sql
   Arrears by salary head, and a Paid status for claims (step 7e-2, Owner decisions 2026-11-03: arrears are paid head by head, the off-cycle run takes no tax because tax is
   deducted in the regular run only, and the regular run's tax already counts every locked run of the financial year, so the arrears reach it through the year to date).
   Brave lane 0300-0399. Idempotent. Never edit after merge. Tenant policy is in 0335, procedures in 0336.
   pay.ArrearsClaim gets StatusID 3 Paid (the run that paid it is in AppliedRunID), AppliedRunID and AppliedUtc. A paid claim cannot be cancelled and its months cannot be claimed again.
   pay.ArrearsClaimMonth gets OvertimeDifference and EsiWages, so a run line can be built from the claim alone.
   pay.ArrearsClaimHead: the difference on each salary head for each wage month, signed. The heads and the overtime difference add up to the gross difference of the month.
   Claims saved before this migration have no head rows, and the arrears run refuses them: cancel and make them again. */
SET XACT_ABORT ON;
GO
IF COL_LENGTH(N'pay.ArrearsClaim', N'AppliedRunID') IS NULL
    ALTER TABLE pay.ArrearsClaim ADD AppliedRunID INT NULL, AppliedUtc DATETIME2(0) NULL;
GO
IF OBJECT_ID(N'pay.CK_ArrearsClaim_Status', N'C') IS NOT NULL
    ALTER TABLE pay.ArrearsClaim DROP CONSTRAINT CK_ArrearsClaim_Status;
GO
ALTER TABLE pay.ArrearsClaim ADD CONSTRAINT CK_ArrearsClaim_Status CHECK (StatusID BETWEEN 1 AND 3);
GO
IF COL_LENGTH(N'pay.ArrearsClaimMonth', N'OvertimeDifference') IS NULL
    ALTER TABLE pay.ArrearsClaimMonth ADD OvertimeDifference DECIMAL(14,2) NOT NULL CONSTRAINT DF_ArrearsClaimMonth_Overtime DEFAULT 0,
                                          EsiWages DECIMAL(14,2) NOT NULL CONSTRAINT DF_ArrearsClaimMonth_EsiWages DEFAULT 0;
GO
IF OBJECT_ID(N'pay.ArrearsClaimHead', N'U') IS NULL
BEGIN
    CREATE TABLE pay.ArrearsClaimHead
    (
        CompanyID    INT           NOT NULL,
        ClaimID      INT           NOT NULL,
        WageMonth    DATE          NOT NULL,
        ComponentID  INT           NOT NULL,
        Amount       DECIMAL(14,2) NOT NULL,
        CONSTRAINT PK_ArrearsClaimHead PRIMARY KEY (ClaimID, WageMonth, ComponentID),
        CONSTRAINT FK_ArrearsClaimHead_Month FOREIGN KEY (ClaimID, WageMonth) REFERENCES pay.ArrearsClaimMonth (ClaimID, WageMonth)
    );
END
GO
