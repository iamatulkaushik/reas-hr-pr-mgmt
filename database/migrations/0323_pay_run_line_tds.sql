/* 0323_pay_run_line_tds.sql
   Change request 0019, accepted by Sigma 2026-10-28. Brave lane 0300-0399. Idempotent. Never edit after merge.
   pay.PayrollRunLine.TdsAmount: income tax deducted from this pay. Default 0, so every line written before this migration stays valid, and a company without the Tds plan feature
   keeps writing 0. TotalDeductions already includes it (gross less net).
   pay.SalaryComponent.IsHra: marks the house rent allowance head, so the HRA received in a month can be read from a line. A head is never HRA and Basic or DA at once.
   The procedures below are the 0305 and 0302 ones with the new column: usp_RunLine_List, usp_Run_ReplaceLines, usp_Component_List and usp_Component_Save.
   usp_Run_ReplaceLines reads TdsAmount as an optional field (missing means 0); usp_Component_Save takes @IsHra as the last parameter, and NULL keeps the stored value (new row: 0),
   so a caller that does not pass it still works. */
SET XACT_ABORT ON;
GO
IF COL_LENGTH(N'pay.PayrollRunLine', N'TdsAmount') IS NULL
    ALTER TABLE pay.PayrollRunLine ADD TdsAmount DECIMAL(14,2) NOT NULL CONSTRAINT DF_PayrollRunLine_Tds DEFAULT 0;
GO
IF COL_LENGTH(N'pay.SalaryComponent', N'IsHra') IS NULL
    ALTER TABLE pay.SalaryComponent ADD IsHra BIT NOT NULL CONSTRAINT DF_SalaryComponent_Hra DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_SalaryComponent_Hra')
    EXEC (N'ALTER TABLE pay.SalaryComponent ADD CONSTRAINT CK_SalaryComponent_Hra CHECK (NOT (IsHra = 1 AND (IsBasic = 1 OR IsDa = 1)))');
GO
CREATE OR ALTER PROCEDURE pay.usp_RunLine_List @RunID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT EmployeeID, FactoryID, PaidDays, OvertimeHours, OvertimeAmount, GrossEarnings, EpfWage, EsiWage, EpfEmployee, EpfEmployerEpf, EpfEmployerEps,
           Edli, EpfAdmin, EsiEmployee, EsiEmployer, ProfessionalTax, TdsAmount, LoanEmi, LoanEmiDeferred, OtherDeductions,
           CarryForwardIn, CarryForwardOut, TotalDeductions, NetPay
    FROM pay.PayrollRunLine WHERE CompanyID = @CompanyID AND RunID = @RunID
    ORDER BY EmployeeID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Component_List
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT ComponentID, Code, Name, IsBasic, IsDa, IsHra, IsOtBase, SortOrder, IsActive
    FROM pay.SalaryComponent WHERE CompanyID = @CompanyID
    ORDER BY SortOrder, Code;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Component_Save
    @ComponentID INT = NULL, @Code NVARCHAR(20), @Name NVARCHAR(100), @IsBasic BIT, @IsDa BIT,
    @IsOtBase BIT, @SortOrder SMALLINT, @IsActive BIT = 1, @IsHra BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @ComponentID IS NULL
    BEGIN
        INSERT pay.SalaryComponent (CompanyID, Code, Name, IsBasic, IsDa, IsHra, IsOtBase, SortOrder, IsActive)
        VALUES (@CompanyID, @Code, @Name, @IsBasic, @IsDa, ISNULL(@IsHra, 0), @IsOtBase, @SortOrder, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS ComponentID;
    END
    ELSE
    BEGIN
        UPDATE pay.SalaryComponent
        SET Code = @Code, Name = @Name, IsBasic = @IsBasic, IsDa = @IsDa, IsHra = ISNULL(@IsHra, IsHra), IsOtBase = @IsOtBase, SortOrder = @SortOrder, IsActive = @IsActive
        WHERE ComponentID = @ComponentID AND CompanyID = @CompanyID;
        IF @@ROWCOUNT = 0 THROW 50301, N'Component not found.', 1;
        SELECT @ComponentID AS ComponentID;
    END
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Run_ReplaceLines @RunID INT, @LinesJson NVARCHAR(MAX), @EarningsJson NVARCHAR(MAX), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Status TINYINT, @From DATE, @To DATE;
    SELECT @Status = StatusID, @From = PeriodFrom, @To = PeriodTo FROM pay.PayrollRun WITH (UPDLOCK) WHERE CompanyID = @CompanyID AND RunID = @RunID;
    IF @Status IS NULL THROW 50311, N'Run not found.', 1;
    IF @Status NOT IN (1, 2) THROW 50312, N'The run cannot be edited in this status.', 1;
    IF EXISTS (SELECT 1 FROM pay.LoanRecovery WHERE CompanyID = @CompanyID AND RunID = @RunID)
        THROW 50324, N'Loan recoveries of this run are still applied.', 1;
    IF EXISTS (SELECT 1 FROM (SELECT YEAR(d) * 100 + MONTH(d) AS MonthKey
                              FROM (SELECT DATEADD(MONTH, n.n, DATEFROMPARTS(YEAR(@From), MONTH(@From), 1)) AS d
                                    FROM (VALUES (0), (1)) n (n)) m
                              WHERE d <= @To) k
               WHERE NOT EXISTS (SELECT 1 FROM att.MonthLock l WHERE l.CompanyID = @CompanyID AND l.MonthKey = k.MonthKey AND l.IsClosed = 1))
        THROW 50318, N'An attendance month in the period is not closed.', 1;
    DECLARE @Lines TABLE
    (
        EmployeeID INT NOT NULL PRIMARY KEY, FactoryID INT NOT NULL, PaidDays DECIMAL(7,2) NOT NULL, OvertimeHours DECIMAL(7,2) NOT NULL, OvertimeAmount DECIMAL(14,2) NOT NULL,
        GrossEarnings DECIMAL(14,2) NOT NULL, EpfWage DECIMAL(14,2) NOT NULL, EsiWage DECIMAL(14,2) NOT NULL, EpfEmployee DECIMAL(14,2) NOT NULL,
        EpfEmployerEpf DECIMAL(14,2) NOT NULL, EpfEmployerEps DECIMAL(14,2) NOT NULL, Edli DECIMAL(14,2) NOT NULL, EpfAdmin DECIMAL(14,2) NOT NULL,
        EsiEmployee DECIMAL(14,2) NOT NULL, EsiEmployer DECIMAL(14,2) NOT NULL, ProfessionalTax DECIMAL(14,2) NOT NULL, TdsAmount DECIMAL(14,2) NOT NULL, LoanEmi DECIMAL(14,2) NOT NULL,
        LoanEmiDeferred DECIMAL(14,2) NOT NULL, OtherDeductions DECIMAL(14,2) NOT NULL, CarryForwardIn DECIMAL(14,2) NOT NULL,
        CarryForwardOut DECIMAL(14,2) NOT NULL, TotalDeductions DECIMAL(14,2) NOT NULL, NetPay DECIMAL(14,2) NOT NULL
    );
    DECLARE @Earn TABLE (EmployeeID INT NOT NULL, ComponentID INT NOT NULL, Amount DECIMAL(14,2) NOT NULL, PRIMARY KEY (EmployeeID, ComponentID));
    BEGIN TRY
        INSERT @Lines
        SELECT EmployeeID, FactoryID, PaidDays, OvertimeHours, OvertimeAmount, GrossEarnings, EpfWage, EsiWage, EpfEmployee, EpfEmployerEpf, EpfEmployerEps, Edli, EpfAdmin,
               EsiEmployee, EsiEmployer, ProfessionalTax, ISNULL(TdsAmount, 0), LoanEmi, LoanEmiDeferred, OtherDeductions, CarryForwardIn, CarryForwardOut, TotalDeductions, NetPay
        FROM OPENJSON(@LinesJson) WITH (
            EmployeeID INT, FactoryID INT, PaidDays DECIMAL(7,2), OvertimeHours DECIMAL(7,2), OvertimeAmount DECIMAL(14,2), GrossEarnings DECIMAL(14,2), EpfWage DECIMAL(14,2),
            EsiWage DECIMAL(14,2), EpfEmployee DECIMAL(14,2), EpfEmployerEpf DECIMAL(14,2), EpfEmployerEps DECIMAL(14,2), Edli DECIMAL(14,2),
            EpfAdmin DECIMAL(14,2), EsiEmployee DECIMAL(14,2), EsiEmployer DECIMAL(14,2), ProfessionalTax DECIMAL(14,2), TdsAmount DECIMAL(14,2), LoanEmi DECIMAL(14,2),
            LoanEmiDeferred DECIMAL(14,2), OtherDeductions DECIMAL(14,2), CarryForwardIn DECIMAL(14,2), CarryForwardOut DECIMAL(14,2),
            TotalDeductions DECIMAL(14,2), NetPay DECIMAL(14,2));
        INSERT @Earn
        SELECT EmployeeID, ComponentID, Amount
        FROM OPENJSON(@EarningsJson) WITH (EmployeeID INT, ComponentID INT, Amount DECIMAL(14,2));
    END TRY
    BEGIN CATCH
        THROW 50317, N'The run lines are not valid.', 1;
    END CATCH
    IF EXISTS (SELECT 1 FROM @Earn e WHERE NOT EXISTS (SELECT 1 FROM @Lines l WHERE l.EmployeeID = e.EmployeeID))
        THROW 50317, N'The run lines are not valid.', 1;
    BEGIN TRANSACTION;
        DELETE FROM pay.PayrollRunEarning WHERE CompanyID = @CompanyID AND RunID = @RunID;
        DELETE FROM pay.PayrollRunLine WHERE CompanyID = @CompanyID AND RunID = @RunID;
        INSERT pay.PayrollRunLine (CompanyID, RunID, EmployeeID, FactoryID, PaidDays, OvertimeHours, OvertimeAmount, GrossEarnings, EpfWage, EsiWage, EpfEmployee,
               EpfEmployerEpf, EpfEmployerEps, Edli, EpfAdmin, EsiEmployee, EsiEmployer, ProfessionalTax, TdsAmount, LoanEmi, LoanEmiDeferred, OtherDeductions,
               CarryForwardIn, CarryForwardOut, TotalDeductions, NetPay)
        SELECT @CompanyID, @RunID, EmployeeID, FactoryID, PaidDays, OvertimeHours, OvertimeAmount, GrossEarnings, EpfWage, EsiWage, EpfEmployee,
               EpfEmployerEpf, EpfEmployerEps, Edli, EpfAdmin, EsiEmployee, EsiEmployer, ProfessionalTax, TdsAmount, LoanEmi, LoanEmiDeferred, OtherDeductions,
               CarryForwardIn, CarryForwardOut, TotalDeductions, NetPay FROM @Lines;
        INSERT pay.PayrollRunEarning (CompanyID, RunID, EmployeeID, ComponentID, Amount)
        SELECT @CompanyID, @RunID, EmployeeID, ComponentID, Amount FROM @Earn;
        UPDATE pay.PayrollRun
        SET StatusID = 2, TotalsChecksum = NULL,
            EmployeeCount = (SELECT COUNT(*) FROM @Lines),
            TotalGross = (SELECT ISNULL(SUM(GrossEarnings), 0) FROM @Lines),
            TotalDeductions = (SELECT ISNULL(SUM(TotalDeductions), 0) FROM @Lines),
            TotalNet = (SELECT ISNULL(SUM(NetPay), 0) FROM @Lines)
        WHERE CompanyID = @CompanyID AND RunID = @RunID;
        INSERT pay.PayrollRunEvent (CompanyID, RunID, FromStatusID, ToStatusID, Action, UserID) VALUES (@CompanyID, @RunID, @Status, 2, N'Process', @UserID);
    COMMIT TRANSACTION;
END
GO
