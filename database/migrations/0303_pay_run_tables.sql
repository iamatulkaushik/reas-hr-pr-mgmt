/* 0303_pay_run_tables.sql
   Payroll run tables. Brave lane 0300-0399. Idempotent. Never edit after merge.
   pay.PayrollRun: one row per run. One regular run (RunNo 1) per company per period start. Off-cycle runs (bonus, arrears) use higher RunNo later.
   StatusID 1 Draft, 2 Processed, 3 Submitted, 4 Approved, 5 Locked, 6 Paid. PeriodFrom and PeriodTo follow the company pay cycle (26th to 25th or calendar month).
   TotalsChecksum is set when the run is submitted and compared when it is approved (ADR 0012: an approval does not freeze data).
   pay.PayrollRunLine: one row per employee. pay.PayrollRunEarning: amount per salary head, for the payslip.
   pay.PayrollRunEvent: append-only status history, kept on every plan like att.MonthLockEvent. ToStatusID 0 means a draft was deleted.
   No foreign key from the event table to the run, so history outlives a deleted draft. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'pay.PayrollRun', N'U') IS NULL
BEGIN
    CREATE TABLE pay.PayrollRun
    (
        RunID           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PayrollRun PRIMARY KEY,
        CompanyID       INT           NOT NULL CONSTRAINT FK_PayrollRun_Company REFERENCES mst.Company (CompanyID),
        PeriodFrom      DATE          NOT NULL,
        PeriodTo        DATE          NOT NULL,
        RunNo           TINYINT       NOT NULL CONSTRAINT DF_PayrollRun_RunNo DEFAULT 1 CONSTRAINT CK_PayrollRun_RunNo CHECK (RunNo >= 1),
        StatusID        TINYINT       NOT NULL CONSTRAINT DF_PayrollRun_Status DEFAULT 1 CONSTRAINT CK_PayrollRun_Status CHECK (StatusID BETWEEN 1 AND 6),
        EmployeeCount   INT           NOT NULL CONSTRAINT DF_PayrollRun_Count DEFAULT 0,
        TotalGross      DECIMAL(14,2) NOT NULL CONSTRAINT DF_PayrollRun_Gross DEFAULT 0,
        TotalDeductions DECIMAL(14,2) NOT NULL CONSTRAINT DF_PayrollRun_Ded DEFAULT 0,
        TotalNet        DECIMAL(14,2) NOT NULL CONSTRAINT DF_PayrollRun_Net DEFAULT 0,
        TotalsChecksum  BINARY(32)    NULL,
        CreatedUtc      DATETIME2(0)  NOT NULL CONSTRAINT DF_PayrollRun_Created DEFAULT SYSUTCDATETIME(),
        CreatedByUserID INT           NULL,
        CONSTRAINT CK_PayrollRun_Period CHECK (PeriodTo >= PeriodFrom),
        CONSTRAINT UQ_PayrollRun_Period UNIQUE (CompanyID, PeriodFrom, RunNo),
        CONSTRAINT UQ_PayrollRun_Company UNIQUE (CompanyID, RunID)
    );
END
GO
IF OBJECT_ID(N'pay.PayrollRunLine', N'U') IS NULL
BEGIN
    CREATE TABLE pay.PayrollRunLine
    (
        CompanyID        INT           NOT NULL,
        RunID            INT           NOT NULL,
        EmployeeID       INT           NOT NULL,
        FactoryID        INT           NOT NULL,
        PaidDays         DECIMAL(7,2)  NOT NULL,
        OvertimeHours    DECIMAL(7,2)  NOT NULL,
        OvertimeAmount   DECIMAL(14,2) NOT NULL,
        GrossEarnings    DECIMAL(14,2) NOT NULL,
        EpfWage          DECIMAL(14,2) NOT NULL,
        EsiWage          DECIMAL(14,2) NOT NULL,
        EpfEmployee      DECIMAL(14,2) NOT NULL,
        EpfEmployerEpf   DECIMAL(14,2) NOT NULL,
        EpfEmployerEps   DECIMAL(14,2) NOT NULL,
        Edli             DECIMAL(14,2) NOT NULL,
        EpfAdmin         DECIMAL(14,2) NOT NULL,
        EsiEmployee      DECIMAL(14,2) NOT NULL,
        EsiEmployer      DECIMAL(14,2) NOT NULL,
        ProfessionalTax  DECIMAL(14,2) NOT NULL,
        LoanEmi          DECIMAL(14,2) NOT NULL,
        LoanEmiDeferred  DECIMAL(14,2) NOT NULL,
        OtherDeductions  DECIMAL(14,2) NOT NULL,
        CarryForwardIn   DECIMAL(14,2) NOT NULL,
        CarryForwardOut  DECIMAL(14,2) NOT NULL,
        TotalDeductions  DECIMAL(14,2) NOT NULL,
        NetPay           DECIMAL(14,2) NOT NULL,
        CONSTRAINT PK_PayrollRunLine PRIMARY KEY (RunID, EmployeeID),
        CONSTRAINT FK_PayrollRunLine_Run FOREIGN KEY (CompanyID, RunID) REFERENCES pay.PayrollRun (CompanyID, RunID),
        CONSTRAINT FK_PayrollRunLine_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID),
        CONSTRAINT CK_PayrollRunLine_Net CHECK (NetPay >= 0)
    );
END
GO
IF OBJECT_ID(N'pay.PayrollRunEarning', N'U') IS NULL
BEGIN
    CREATE TABLE pay.PayrollRunEarning
    (
        CompanyID    INT           NOT NULL,
        RunID        INT           NOT NULL,
        EmployeeID   INT           NOT NULL,
        ComponentID  INT           NOT NULL,
        Amount       DECIMAL(14,2) NOT NULL CONSTRAINT CK_PayrollRunEarning_Amount CHECK (Amount >= 0),
        CONSTRAINT PK_PayrollRunEarning PRIMARY KEY (RunID, EmployeeID, ComponentID),
        CONSTRAINT FK_PayrollRunEarning_Line FOREIGN KEY (RunID, EmployeeID) REFERENCES pay.PayrollRunLine (RunID, EmployeeID),
        CONSTRAINT FK_PayrollRunEarning_Component FOREIGN KEY (CompanyID, ComponentID) REFERENCES pay.SalaryComponent (CompanyID, ComponentID)
    );
END
GO
IF OBJECT_ID(N'pay.PayrollRunEvent', N'U') IS NULL
BEGIN
    CREATE TABLE pay.PayrollRunEvent
    (
        EventID      BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PayrollRunEvent PRIMARY KEY,
        CompanyID    INT           NOT NULL CONSTRAINT FK_PayrollRunEvent_Company REFERENCES mst.Company (CompanyID),
        RunID        INT           NOT NULL,
        FromStatusID TINYINT       NOT NULL CONSTRAINT CK_PayrollRunEvent_From CHECK (FromStatusID BETWEEN 0 AND 6),
        ToStatusID   TINYINT       NOT NULL CONSTRAINT CK_PayrollRunEvent_To CHECK (ToStatusID BETWEEN 0 AND 6),
        Action       NVARCHAR(30)  NOT NULL,
        UserID       INT           NULL,
        Reason       NVARCHAR(500) NULL,
        EventUtc     DATETIME2(0)  NOT NULL CONSTRAINT DF_PayrollRunEvent_EventUtc DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_PayrollRunEvent_Run ON pay.PayrollRunEvent (CompanyID, RunID, EventID);
END
GO
