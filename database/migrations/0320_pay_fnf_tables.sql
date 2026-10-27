/* 0320_pay_fnf_tables.sql
   Full and final settlement. Brave lane 0300-0399. Idempotent. Never edit after merge.
   pay.FnfSettlement: one per separation. StatusID follows mst.EmployeeSeparation.FnFStatusID: 1 Open, 2 Calculated, 3 Approved, 4 Paid.
   The maker is who calculated it and the checker is who approved it; they must be different people (checked in pay.usp_Fnf_ChangeStatus).
   pay.FnfComponent: the lines, built by the application from the calculators. Amount is always positive; IsDeduction says which side it is on.
   ComponentTypeID: 1 Last salary, 2 Notice pay recovery, 3 Bonus, 4 Leave encashment, 5 Gratuity, 6 Loan closure, 7 TDS shortfall, 8 TDS refund,
   9 Arrears recovery (Owner option), 10 Other earning, 11 Other deduction. Types 2, 6, 7, 9 and 11 are deductions. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'pay.FnfSettlement', N'U') IS NULL
BEGIN
    CREATE TABLE pay.FnfSettlement
    (
        FnfID            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FnfSettlement PRIMARY KEY,
        CompanyID        INT           NOT NULL CONSTRAINT FK_FnfSettlement_Company REFERENCES mst.Company (CompanyID),
        EmployeeID       INT           NOT NULL,
        SeparationID     INT           NOT NULL CONSTRAINT FK_FnfSettlement_Separation REFERENCES mst.EmployeeSeparation (SeparationID),
        ExitDate         DATE          NOT NULL,
        StatusID         TINYINT       NOT NULL CONSTRAINT DF_FnfSettlement_Status DEFAULT 1 CONSTRAINT CK_FnfSettlement_Status CHECK (StatusID BETWEEN 1 AND 4),
        TotalEarnings    DECIMAL(14,2) NOT NULL CONSTRAINT DF_FnfSettlement_Earn DEFAULT 0 CONSTRAINT CK_FnfSettlement_Earn CHECK (TotalEarnings >= 0),
        TotalDeductions  DECIMAL(14,2) NOT NULL CONSTRAINT DF_FnfSettlement_Ded DEFAULT 0 CONSTRAINT CK_FnfSettlement_Ded CHECK (TotalDeductions >= 0),
        NetPayable       AS (TotalEarnings - TotalDeductions) PERSISTED,
        MakerUserID      INT           NULL,
        CheckerUserID    INT           NULL,
        CalculatedUtc    DATETIME2(0)  NULL,
        ApprovedUtc      DATETIME2(0)  NULL,
        PaidUtc          DATETIME2(0)  NULL,
        CONSTRAINT UQ_FnfSettlement_Separation UNIQUE (SeparationID),
        CONSTRAINT UQ_FnfSettlement_Company UNIQUE (CompanyID, FnfID),
        CONSTRAINT FK_FnfSettlement_Employee FOREIGN KEY (CompanyID, EmployeeID) REFERENCES mst.Employee (CompanyID, EmployeeID),
        CONSTRAINT CK_FnfSettlement_Checker CHECK (CheckerUserID IS NULL OR MakerUserID IS NULL OR CheckerUserID <> MakerUserID)
    );
END
GO
IF OBJECT_ID(N'pay.FnfComponent', N'U') IS NULL
BEGIN
    CREATE TABLE pay.FnfComponent
    (
        CompanyID       INT           NOT NULL,
        FnfID           INT           NOT NULL,
        ComponentNo          SMALLINT      NOT NULL CONSTRAINT CK_FnfComponent_No CHECK (ComponentNo >= 1),
        ComponentTypeID TINYINT       NOT NULL CONSTRAINT CK_FnfComponent_Type CHECK (ComponentTypeID BETWEEN 1 AND 11),
        IsDeduction     AS (CASE WHEN ComponentTypeID IN (2, 6, 7, 9, 11) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END) PERSISTED,
        Description     NVARCHAR(200) NOT NULL,
        Amount          DECIMAL(14,2) NOT NULL CONSTRAINT CK_FnfComponent_Amount CHECK (Amount > 0),
        CONSTRAINT PK_FnfComponent PRIMARY KEY (FnfID, ComponentNo),
        CONSTRAINT FK_FnfComponent_Settlement FOREIGN KEY (CompanyID, FnfID) REFERENCES pay.FnfSettlement (CompanyID, FnfID)
    );
END
GO
