/* 0302_pay_salary_procs.sql
   Payroll salary procedures. CREATE OR ALTER keeps it idempotent.
   The company always comes from SESSION_CONTEXT, never from a parameter. No company in the session means no read and no write.
   Error numbers: 50002 no company in session, 50301 component not found, 50302 salary for this date already exists,
   50303 salary has no lines, 50304 lines name a component twice or an unknown one. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_Policy_Get
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.CompanyPolicy WHERE CompanyID = @CompanyID)
        INSERT pay.CompanyPolicy (CompanyID) VALUES (@CompanyID);
    SELECT PayCycleStartDay, OtMultiplier, OtInEpfWage, OtInEsiWage, DailyWageDivisor, EmiCapPct
    FROM pay.CompanyPolicy WHERE CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Policy_Save
    @PayCycleStartDay TINYINT, @OtMultiplier DECIMAL(7,4), @OtInEpfWage BIT, @OtInEsiWage BIT,
    @DailyWageDivisor DECIMAL(7,2), @EmiCapPct DECIMAL(7,4), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    UPDATE pay.CompanyPolicy
    SET PayCycleStartDay = @PayCycleStartDay, OtMultiplier = @OtMultiplier, OtInEpfWage = @OtInEpfWage, OtInEsiWage = @OtInEsiWage,
        DailyWageDivisor = @DailyWageDivisor, EmiCapPct = @EmiCapPct, UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE CompanyID = @CompanyID;
    IF @@ROWCOUNT = 0
        INSERT pay.CompanyPolicy (CompanyID, PayCycleStartDay, OtMultiplier, OtInEpfWage, OtInEsiWage, DailyWageDivisor, EmiCapPct, UpdatedByUserID)
        VALUES (@CompanyID, @PayCycleStartDay, @OtMultiplier, @OtInEpfWage, @OtInEsiWage, @DailyWageDivisor, @EmiCapPct, @UserID);
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Component_List
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT ComponentID, Code, Name, IsBasic, IsDa, IsOtBase, SortOrder, IsActive
    FROM pay.SalaryComponent WHERE CompanyID = @CompanyID
    ORDER BY SortOrder, Code;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Component_Save
    @ComponentID INT = NULL, @Code NVARCHAR(20), @Name NVARCHAR(100), @IsBasic BIT, @IsDa BIT,
    @IsOtBase BIT, @SortOrder SMALLINT, @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @ComponentID IS NULL
    BEGIN
        INSERT pay.SalaryComponent (CompanyID, Code, Name, IsBasic, IsDa, IsOtBase, SortOrder, IsActive)
        VALUES (@CompanyID, @Code, @Name, @IsBasic, @IsDa, @IsOtBase, @SortOrder, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS ComponentID;
    END
    ELSE
    BEGIN
        UPDATE pay.SalaryComponent
        SET Code = @Code, Name = @Name, IsBasic = @IsBasic, IsDa = @IsDa, IsOtBase = @IsOtBase, SortOrder = @SortOrder, IsActive = @IsActive
        WHERE ComponentID = @ComponentID AND CompanyID = @CompanyID;
        IF @@ROWCOUNT = 0 THROW 50301, N'Component not found.', 1;
        SELECT @ComponentID AS ComponentID;
    END
END
GO
/* Salary in force on a date: the header with the latest EffectiveFrom on or before @AsOf, and its lines. Two result sets. */
CREATE OR ALTER PROCEDURE pay.usp_Salary_GetAt @EmployeeID INT, @AsOf DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @SalaryID INT = (SELECT TOP (1) SalaryID FROM pay.EmployeeSalary
                             WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND EffectiveFrom <= @AsOf
                             ORDER BY EffectiveFrom DESC);
    SELECT SalaryID, EffectiveFrom FROM pay.EmployeeSalary WHERE SalaryID = @SalaryID AND CompanyID = @CompanyID;
    SELECT l.ComponentID, c.Code, c.Name, c.IsBasic, c.IsDa, c.IsOtBase, l.Amount
    FROM pay.EmployeeSalaryLine l
    JOIN pay.SalaryComponent c ON c.CompanyID = l.CompanyID AND c.ComponentID = l.ComponentID
    WHERE l.CompanyID = @CompanyID AND l.SalaryID = @SalaryID
    ORDER BY c.SortOrder, c.Code;
END
GO
/* @LinesJson is an array of {"ComponentID":1,"Amount":12000.00}. */
CREATE OR ALTER PROCEDURE pay.usp_Salary_Save
    @EmployeeID INT, @EffectiveFrom DATE, @Reason NVARCHAR(200) = NULL, @LinesJson NVARCHAR(MAX), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Lines TABLE (ComponentID INT NOT NULL PRIMARY KEY, Amount DECIMAL(14,2) NOT NULL);
    BEGIN TRY
        INSERT @Lines (ComponentID, Amount)
        SELECT ComponentID, Amount FROM OPENJSON(@LinesJson) WITH (ComponentID INT '$.ComponentID', Amount DECIMAL(14,2) '$.Amount');
    END TRY
    BEGIN CATCH
        THROW 50304, N'Salary lines name a component twice or are not valid.', 1;
    END CATCH
    IF NOT EXISTS (SELECT 1 FROM @Lines) THROW 50303, N'Salary has no lines.', 1;
    IF EXISTS (SELECT 1 FROM @Lines x WHERE NOT EXISTS (SELECT 1 FROM pay.SalaryComponent c WHERE c.CompanyID = @CompanyID AND c.ComponentID = x.ComponentID))
        THROW 50301, N'Component not found.', 1;
    IF EXISTS (SELECT 1 FROM pay.EmployeeSalary WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND EffectiveFrom = @EffectiveFrom)
        THROW 50302, N'A salary for this date already exists.', 1;
    BEGIN TRANSACTION;
        INSERT pay.EmployeeSalary (CompanyID, EmployeeID, EffectiveFrom, Reason, CreatedByUserID)
        VALUES (@CompanyID, @EmployeeID, @EffectiveFrom, @Reason, @UserID);
        DECLARE @SalaryID INT = CAST(SCOPE_IDENTITY() AS INT);
        INSERT pay.EmployeeSalaryLine (CompanyID, SalaryID, ComponentID, Amount)
        SELECT @CompanyID, @SalaryID, ComponentID, Amount FROM @Lines;
    COMMIT TRANSACTION;
    SELECT @SalaryID AS SalaryID;
END
GO
