/* 0327_pay_arrears_tables.sql
   Arrears claims (Brave, step 7c, Owner decisions 2026-11-01). Brave lane 0300-0399. Idempotent. Never edit after merge.
   pay.ArrearsClaim: one row per claim, for one employee over a run of wage months (FromMonth to ToMonth, both the first of a month).
   StatusID 1 Open, 2 Cancelled. A claim is never deleted: a wrong one is cancelled with a reason, and a month that is under a cancelled claim can be claimed again.
   The totals are the sums of the month rows, kept on the header so the list does not add them up. Amounts are signed: a negative amount is an overpayment to recover.
   Tax is not here: section 89 relief is applied by the run that pays the arrears.
   pay.ArrearsClaimMonth: one row per wage month, the due less paid earnings and the EPF and ESI on the difference, at the rates of that wage month.
   Applying a claim to an off-cycle run (RunNo 2 and up) comes later and adds its own columns. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'pay.ArrearsClaim', N'U') IS NULL
BEGIN
    CREATE TABLE pay.ArrearsClaim
    (
        ClaimID              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ArrearsClaim PRIMARY KEY,
        CompanyID            INT           NOT NULL CONSTRAINT FK_ArrearsClaim_Company REFERENCES mst.Company (CompanyID),
        EmployeeID           INT           NOT NULL,
        FromMonth            DATE          NOT NULL,
        ToMonth              DATE          NOT NULL,
        Reason               NVARCHAR(200) NOT NULL,
        StatusID             TINYINT       NOT NULL CONSTRAINT DF_ArrearsClaim_Status DEFAULT 1 CONSTRAINT CK_ArrearsClaim_Status CHECK (StatusID BETWEEN 1 AND 2),
        GrossTotal           DECIMAL(14,2) NOT NULL,
        EmployeeDeductions   DECIMAL(14,2) NOT NULL,
        EmployerContributions DECIMAL(14,2) NOT NULL,
        NetBeforeTax         DECIMAL(14,2) NOT NULL,
        CreatedUtc           DATETIME2(0)  NOT NULL CONSTRAINT DF_ArrearsClaim_Created DEFAULT SYSUTCDATETIME(),
        CreatedByUserID      INT           NULL,
        CancelledUtc         DATETIME2(0)  NULL,
        CancelledByUserID    INT           NULL,
        CancelReason         NVARCHAR(200) NULL,
        CONSTRAINT CK_ArrearsClaim_Months CHECK (ToMonth >= FromMonth AND DAY(FromMonth) = 1 AND DAY(ToMonth) = 1),
        CONSTRAINT UQ_ArrearsClaim_Company UNIQUE (CompanyID, ClaimID),
        CONSTRAINT FK_ArrearsClaim_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID)
    );
    CREATE INDEX IX_ArrearsClaim_Employee ON pay.ArrearsClaim (CompanyID, EmployeeID, FromMonth);
END
GO
IF OBJECT_ID(N'pay.ArrearsClaimMonth', N'U') IS NULL
BEGIN
    CREATE TABLE pay.ArrearsClaimMonth
    (
        CompanyID       INT           NOT NULL,
        ClaimID         INT           NOT NULL,
        WageMonth       DATE          NOT NULL CONSTRAINT CK_ArrearsClaimMonth_Month CHECK (DAY(WageMonth) = 1),
        PaidGross       DECIMAL(14,2) NOT NULL,
        DueGross        DECIMAL(14,2) NOT NULL,
        GrossDifference DECIMAL(14,2) NOT NULL,
        EpfWages        DECIMAL(14,2) NOT NULL,
        EpsWages        DECIMAL(14,2) NOT NULL,
        EdliWages       DECIMAL(14,2) NOT NULL,
        EmployeeEpf     DECIMAL(14,2) NOT NULL,
        EmployerEpf     DECIMAL(14,2) NOT NULL,
        EmployerEps     DECIMAL(14,2) NOT NULL,
        Edli            DECIMAL(14,2) NOT NULL,
        Admin           DECIMAL(14,2) NOT NULL,
        EsiEmployee     DECIMAL(14,2) NOT NULL,
        EsiEmployer     DECIMAL(14,2) NOT NULL,
        CONSTRAINT PK_ArrearsClaimMonth PRIMARY KEY (ClaimID, WageMonth),
        CONSTRAINT FK_ArrearsClaimMonth_Claim FOREIGN KEY (CompanyID, ClaimID) REFERENCES pay.ArrearsClaim (CompanyID, ClaimID)
    );
END
GO
