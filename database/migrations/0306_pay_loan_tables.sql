/* 0306_pay_loan_tables.sql
   Loans and EMI. Brave lane 0300-0399. Idempotent. Never edit after merge.
   pay.LoanType: interest method (1 flat, 2 reducing balance) and yearly rate per type (Owner). Rate 0 means no interest.
   pay.Loan: copies method and rate at sanction, so a later change to the type does not touch a running loan. StatusID 1 Active, 2 Cancelled.
   A loan is finished when every installment is fully recovered: that is worked out from recoveries, not stored.
   pay.LoanInstallment: the schedule, built by the application and checked by pay.usp_Loan_Create. DueMonthKey is YYYYMM.
   pay.LoanRecovery: money taken from pay. Written when a run is Locked, removed when it is reopened. An installment not fully recovered stays due and is
   scheduled again in the next run (deferred EMI). */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'pay.LoanType', N'U') IS NULL
BEGIN
    CREATE TABLE pay.LoanType
    (
        LoanTypeID        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LoanType PRIMARY KEY,
        CompanyID         INT           NOT NULL CONSTRAINT FK_LoanType_Company REFERENCES mst.Company (CompanyID),
        Name              NVARCHAR(100) NOT NULL,
        InterestMethodID  TINYINT       NOT NULL CONSTRAINT CK_LoanType_Method CHECK (InterestMethodID IN (1, 2)),
        AnnualInterestPct DECIMAL(7,4)  NOT NULL CONSTRAINT DF_LoanType_Rate DEFAULT 0 CONSTRAINT CK_LoanType_Rate CHECK (AnnualInterestPct BETWEEN 0 AND 100),
        MaxInstallments   SMALLINT      NOT NULL CONSTRAINT DF_LoanType_Max DEFAULT 60 CONSTRAINT CK_LoanType_Max CHECK (MaxInstallments BETWEEN 1 AND 360),
        IsActive          BIT           NOT NULL CONSTRAINT DF_LoanType_Active DEFAULT 1,
        CONSTRAINT UQ_LoanType_Name UNIQUE (CompanyID, Name),
        CONSTRAINT UQ_LoanType_Company UNIQUE (CompanyID, LoanTypeID)
    );
END
GO
IF OBJECT_ID(N'pay.Loan', N'U') IS NULL
BEGIN
    CREATE TABLE pay.Loan
    (
        LoanID            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Loan PRIMARY KEY,
        CompanyID         INT           NOT NULL CONSTRAINT FK_Loan_Company REFERENCES mst.Company (CompanyID),
        EmployeeID        INT           NOT NULL,
        LoanTypeID        INT           NOT NULL,
        SanctionDate      DATE          NOT NULL,
        Principal         DECIMAL(14,2) NOT NULL CONSTRAINT CK_Loan_Principal CHECK (Principal > 0),
        InterestMethodID  TINYINT       NOT NULL CONSTRAINT CK_Loan_Method CHECK (InterestMethodID IN (1, 2)),
        AnnualInterestPct DECIMAL(7,4)  NOT NULL CONSTRAINT CK_Loan_Rate CHECK (AnnualInterestPct BETWEEN 0 AND 100),
        InstallmentCount  SMALLINT      NOT NULL CONSTRAINT CK_Loan_Count CHECK (InstallmentCount BETWEEN 1 AND 360),
        StatusID          TINYINT       NOT NULL CONSTRAINT DF_Loan_Status DEFAULT 1 CONSTRAINT CK_Loan_Status CHECK (StatusID IN (1, 2)),
        CreatedUtc        DATETIME2(0)  NOT NULL CONSTRAINT DF_Loan_Created DEFAULT SYSUTCDATETIME(),
        CreatedByUserID   INT           NULL,
        CONSTRAINT UQ_Loan_Company UNIQUE (CompanyID, LoanID),
        CONSTRAINT FK_Loan_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID),
        CONSTRAINT FK_Loan_Type FOREIGN KEY (CompanyID, LoanTypeID) REFERENCES pay.LoanType (CompanyID, LoanTypeID)
    );
END
GO
IF OBJECT_ID(N'pay.LoanInstallment', N'U') IS NULL
BEGIN
    CREATE TABLE pay.LoanInstallment
    (
        CompanyID       INT           NOT NULL,
        LoanID          INT           NOT NULL,
        InstallmentNo   SMALLINT      NOT NULL CONSTRAINT CK_LoanInstallment_No CHECK (InstallmentNo >= 1),
        DueMonthKey     INT           NOT NULL CONSTRAINT CK_LoanInstallment_Key CHECK (DueMonthKey BETWEEN 190001 AND 999912 AND DueMonthKey % 100 BETWEEN 1 AND 12),
        PrincipalPart   DECIMAL(14,2) NOT NULL CONSTRAINT CK_LoanInstallment_Principal CHECK (PrincipalPart >= 0),
        InterestPart    DECIMAL(14,2) NOT NULL CONSTRAINT CK_LoanInstallment_Interest CHECK (InterestPart >= 0),
        Amount          DECIMAL(14,2) NOT NULL,
        CONSTRAINT PK_LoanInstallment PRIMARY KEY (LoanID, InstallmentNo),
        CONSTRAINT FK_LoanInstallment_Loan FOREIGN KEY (CompanyID, LoanID) REFERENCES pay.Loan (CompanyID, LoanID),
        CONSTRAINT CK_LoanInstallment_Amount CHECK (Amount = PrincipalPart + InterestPart AND Amount > 0)
    );
END
GO
IF OBJECT_ID(N'pay.LoanRecovery', N'U') IS NULL
BEGIN
    CREATE TABLE pay.LoanRecovery
    (
        RecoveryID     BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LoanRecovery PRIMARY KEY,
        CompanyID      INT           NOT NULL,
        LoanID         INT           NOT NULL,
        InstallmentNo  SMALLINT      NOT NULL,
        RunID          INT           NOT NULL,
        Amount         DECIMAL(14,2) NOT NULL CONSTRAINT CK_LoanRecovery_Amount CHECK (Amount > 0),
        CONSTRAINT FK_LoanRecovery_Installment FOREIGN KEY (LoanID, InstallmentNo) REFERENCES pay.LoanInstallment (LoanID, InstallmentNo),
        CONSTRAINT FK_LoanRecovery_Run FOREIGN KEY (CompanyID, RunID) REFERENCES pay.PayrollRun (CompanyID, RunID),
        CONSTRAINT UQ_LoanRecovery_Part UNIQUE (RunID, LoanID, InstallmentNo)
    );
END
GO
