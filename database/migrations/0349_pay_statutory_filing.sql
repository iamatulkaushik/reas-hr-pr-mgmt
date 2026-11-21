/* 0349_pay_statutory_filing.sql
   ADR 0027 point 4, work order docs/phase7b-plan-sigma.md. Brave lane 0300-0399. Idempotent. Never edit after merge. Policy is in 0350.
   pay.StatutoryFiling: what was generated, filed and paid for a company and month. One row per (CompanyID, PeriodStart, Kind, StateID); Kind is ComplianceKind
   (1 EPF, 2 ESIC, 3 professional tax, 4 TDS, 5 labour welfare); StateID is NULL for the national items. Status: 3 Generated, 4 Filed, 5 Paid.
   AmountPayable is the amount when the line was generated, so a later payroll change shows as Changed. A closed or reopened payroll never deletes a row.
   No plain statutory id is stored here: only amounts, counts, dates and the reference number the Associate typed (acknowledgement or challan).
   Errors for the Monthly Pack: 53650 value out of range, 53651 never generated, 53652 date in the future, 53653 date missing. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'pay.StatutoryFiling', N'U') IS NULL
BEGIN
    CREATE TABLE pay.StatutoryFiling
    (
        CompanyID         INT           NOT NULL CONSTRAINT FK_StatutoryFiling_Company REFERENCES mst.Company (CompanyID),
        PeriodStart       DATE          NOT NULL CONSTRAINT CK_StatutoryFiling_Period CHECK (DAY(PeriodStart) = 1),
        Kind              TINYINT       NOT NULL CONSTRAINT CK_StatutoryFiling_Kind CHECK (Kind BETWEEN 1 AND 5),
        StateKey          TINYINT       NOT NULL CONSTRAINT DF_StatutoryFiling_StateKey DEFAULT 0,
        StateID           TINYINT       NULL CONSTRAINT FK_StatutoryFiling_State REFERENCES mst.[State] (StateID),
        Status            TINYINT       NOT NULL CONSTRAINT CK_StatutoryFiling_Status CHECK (Status IN (3, 4, 5)),
        AmountPayable     DECIMAL(14,2) NOT NULL CONSTRAINT CK_StatutoryFiling_Amount CHECK (AmountPayable >= 0),
        EmployeeCount     INT           NOT NULL CONSTRAINT CK_StatutoryFiling_Count CHECK (EmployeeCount >= 0),
        GeneratedUtc      DATETIME2(0)  NOT NULL CONSTRAINT DF_StatutoryFiling_GeneratedUtc DEFAULT SYSUTCDATETIME(),
        GeneratedByUserID INT           NULL,
        FiledOn           DATE          NULL,
        PaidOn            DATE          NULL,
        ReferenceNo       NVARCHAR(40)  NULL,
        Remarks           NVARCHAR(200) NULL,
        UpdatedUtc        DATETIME2(0)  NOT NULL CONSTRAINT DF_StatutoryFiling_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_StatutoryFiling PRIMARY KEY (CompanyID, PeriodStart, Kind, StateKey),
        CONSTRAINT CK_StatutoryFiling_StateKey CHECK (StateKey = ISNULL(StateID, 0))
    );
END
GO
/* The rows of a month, newest kinds last. */
CREATE OR ALTER PROCEDURE pay.usp_Filing_List @PeriodStart DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @PeriodStart IS NULL OR DAY(@PeriodStart) <> 1 THROW 53650, N'The month must start on the first day.', 1;
    SELECT f.Kind, f.StateID, f.Status, f.AmountPayable, f.EmployeeCount, f.GeneratedUtc, f.FiledOn, f.PaidOn, f.ReferenceNo, f.Remarks
    FROM pay.StatutoryFiling f
    WHERE f.CompanyID = @CompanyID AND f.PeriodStart = @PeriodStart
    ORDER BY f.Kind, f.StateKey;
END
GO
/* Upsert one line. Status 3 (Generated) stores the amount and the count, stamps who and when, and clears any earlier filing or payment (a line made again must be filed again).
   Status 4 (Filed) needs FiledOn; Status 5 (Paid) needs PaidOn; both need an existing row (53651), a date that is not in the future (53652) and keep the generated amount.
   ReferenceNo and Remarks are stored as given. */
CREATE OR ALTER PROCEDURE pay.usp_Filing_Record
    @PeriodStart DATE, @Kind TINYINT, @StateID TINYINT = NULL, @Status TINYINT,
    @AmountPayable DECIMAL(14,2) = NULL, @EmployeeCount INT = NULL,
    @FiledOn DATE = NULL, @PaidOn DATE = NULL, @ReferenceNo NVARCHAR(40) = NULL, @Remarks NVARCHAR(200) = NULL, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @PeriodStart IS NULL OR DAY(@PeriodStart) <> 1 OR @Kind NOT BETWEEN 1 AND 5 OR @Status NOT IN (3, 4, 5)
        THROW 53650, N'The month, the item or the status is not valid.', 1;
    DECLARE @Key TINYINT = ISNULL(@StateID, 0);
    DECLARE @Today DATE = CAST(SYSDATETIME() AS DATE);
    IF @Status = 3
    BEGIN
        IF @AmountPayable IS NULL OR @AmountPayable < 0 OR @EmployeeCount IS NULL OR @EmployeeCount < 0
            THROW 53650, N'The amount and the number of employees are needed when a line is generated.', 1;
        UPDATE pay.StatutoryFiling
        SET Status = 3, AmountPayable = @AmountPayable, EmployeeCount = @EmployeeCount, GeneratedUtc = SYSUTCDATETIME(), GeneratedByUserID = @UserID,
            FiledOn = NULL, PaidOn = NULL, ReferenceNo = NULL, Remarks = NULL, UpdatedUtc = SYSUTCDATETIME()
        WHERE CompanyID = @CompanyID AND PeriodStart = @PeriodStart AND Kind = @Kind AND StateKey = @Key;
        IF @@ROWCOUNT = 0
            INSERT pay.StatutoryFiling (CompanyID, PeriodStart, Kind, StateKey, StateID, Status, AmountPayable, EmployeeCount, GeneratedByUserID)
            VALUES (@CompanyID, @PeriodStart, @Kind, @Key, @StateID, 3, @AmountPayable, @EmployeeCount, @UserID);
        RETURN;
    END
    IF NOT EXISTS (SELECT 1 FROM pay.StatutoryFiling WHERE CompanyID = @CompanyID AND PeriodStart = @PeriodStart AND Kind = @Kind AND StateKey = @Key)
        THROW 53651, N'This item was never generated for the month.', 1;
    IF @Status = 4
    BEGIN
        IF @FiledOn IS NULL THROW 53653, N'Give the date it was filed.', 1;
        IF @FiledOn > @Today THROW 53652, N'The date cannot be in the future.', 1;
        UPDATE pay.StatutoryFiling
        SET Status = 4, FiledOn = @FiledOn, ReferenceNo = @ReferenceNo, Remarks = @Remarks, UpdatedUtc = SYSUTCDATETIME()
        WHERE CompanyID = @CompanyID AND PeriodStart = @PeriodStart AND Kind = @Kind AND StateKey = @Key;
    END
    ELSE
    BEGIN
        IF @PaidOn IS NULL THROW 53653, N'Give the date it was paid.', 1;
        IF @PaidOn > @Today THROW 53652, N'The date cannot be in the future.', 1;
        UPDATE pay.StatutoryFiling
        SET Status = 5, PaidOn = @PaidOn, ReferenceNo = COALESCE(@ReferenceNo, ReferenceNo), Remarks = COALESCE(@Remarks, Remarks), UpdatedUtc = SYSUTCDATETIME()
        WHERE CompanyID = @CompanyID AND PeriodStart = @PeriodStart AND Kind = @Kind AND StateKey = @Key;
    END
END
GO
