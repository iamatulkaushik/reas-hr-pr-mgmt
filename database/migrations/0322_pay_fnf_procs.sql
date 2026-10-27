/* 0322_pay_fnf_procs.sql
   F&F procedures. CREATE OR ALTER keeps it idempotent. The company and the user come from SESSION_CONTEXT, never from a parameter.
   usp_Fnf_Save: stores the lines of a settlement and moves it to Calculated (2). Allowed while Open or Calculated. The maker is the session user.
   @ComponentsJson: array of {"Type":3,"Description":"Bonus","Amount":3499.00}. Totals are worked out here from the lines.
   usp_Fnf_ChangeStatus: Calculated to Approved (the checker is not the maker), Approved to Paid, Approved back to Calculated. The employee's FnFStatusID is kept in step.
   Error numbers: 50002 no company in session, 50330 no user in session, 50354 separation not found, 50355 settlement not found,
   50356 settlement is not open for change, 50357 lines are not valid, 50358 move not allowed, 50359 the maker cannot approve. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_Fnf_Save @SeparationID INT, @ComponentsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @UserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    IF @UserID IS NULL THROW 50330, N'No user in session.', 1;
    DECLARE @EmployeeID INT, @ExitDate DATE;
    SELECT @EmployeeID = EmployeeID, @ExitDate = LastWorkingDay FROM mst.EmployeeSeparation WHERE CompanyID = @CompanyID AND SeparationID = @SeparationID;
    IF @EmployeeID IS NULL THROW 50354, N'Separation not found.', 1;
    DECLARE @Lines TABLE (ComponentNo SMALLINT NOT NULL PRIMARY KEY, ComponentTypeID TINYINT NOT NULL, Description NVARCHAR(200) NOT NULL, Amount DECIMAL(14,2) NOT NULL);
    BEGIN TRY
        INSERT @Lines
        SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)), ComponentTypeID, Description, Amount
        FROM OPENJSON(@ComponentsJson) WITH (ComponentTypeID TINYINT '$.Type', Description NVARCHAR(200) '$.Description', Amount DECIMAL(14,2) '$.Amount');
    END TRY
    BEGIN CATCH
        THROW 50357, N'The lines are not valid.', 1;
    END CATCH
    IF NOT EXISTS (SELECT 1 FROM @Lines) OR EXISTS (SELECT 1 FROM @Lines WHERE Amount <= 0 OR ComponentTypeID NOT BETWEEN 1 AND 11)
        THROW 50357, N'The lines are not valid.', 1;
    DECLARE @Earn DECIMAL(14,2) = ISNULL((SELECT SUM(Amount) FROM @Lines WHERE ComponentTypeID NOT IN (2, 6, 7, 9, 11)), 0);
    DECLARE @Ded  DECIMAL(14,2) = ISNULL((SELECT SUM(Amount) FROM @Lines WHERE ComponentTypeID IN (2, 6, 7, 9, 11)), 0);
    BEGIN TRANSACTION;
        DECLARE @FnfID INT, @Status TINYINT;
        SELECT @FnfID = FnfID, @Status = StatusID FROM pay.FnfSettlement WITH (UPDLOCK) WHERE CompanyID = @CompanyID AND SeparationID = @SeparationID;
        IF @FnfID IS NULL
        BEGIN
            INSERT pay.FnfSettlement (CompanyID, EmployeeID, SeparationID, ExitDate) VALUES (@CompanyID, @EmployeeID, @SeparationID, @ExitDate);
            SET @FnfID = CAST(SCOPE_IDENTITY() AS INT);
            SET @Status = 1;
        END
        IF @Status > 2
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50356, N'The settlement is not open for change.', 1;
        END
        DELETE pay.FnfComponent WHERE CompanyID = @CompanyID AND FnfID = @FnfID;
        INSERT pay.FnfComponent (CompanyID, FnfID, ComponentNo, ComponentTypeID, Description, Amount)
        SELECT @CompanyID, @FnfID, ComponentNo, ComponentTypeID, Description, Amount FROM @Lines;
        UPDATE pay.FnfSettlement
        SET StatusID = 2, TotalEarnings = @Earn, TotalDeductions = @Ded, MakerUserID = @UserID, CalculatedUtc = SYSUTCDATETIME()
        WHERE CompanyID = @CompanyID AND FnfID = @FnfID;
        UPDATE mst.EmployeeSeparation SET FnFStatusID = 2 WHERE CompanyID = @CompanyID AND SeparationID = @SeparationID;
    COMMIT TRANSACTION;
    SELECT @FnfID AS FnfID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Fnf_ChangeStatus @FnfID INT, @NewStatusID TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @UserID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID'));
    IF @UserID IS NULL THROW 50330, N'No user in session.', 1;
    BEGIN TRANSACTION;
        DECLARE @Old TINYINT, @Maker INT, @SeparationID INT;
        SELECT @Old = StatusID, @Maker = MakerUserID, @SeparationID = SeparationID
        FROM pay.FnfSettlement WITH (UPDLOCK) WHERE CompanyID = @CompanyID AND FnfID = @FnfID;
        IF @Old IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50355, N'Settlement not found.', 1;
        END
        IF NOT ((@Old = 2 AND @NewStatusID = 3) OR (@Old = 3 AND @NewStatusID = 4) OR (@Old = 3 AND @NewStatusID = 2))
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50358, N'That move is not allowed.', 1;
        END
        IF @Old = 2 AND @NewStatusID = 3 AND @Maker = @UserID
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50359, N'The person who calculated the settlement cannot approve it.', 1;
        END
        UPDATE pay.FnfSettlement
        SET StatusID = @NewStatusID,
            CheckerUserID = CASE WHEN @NewStatusID = 3 THEN @UserID WHEN @NewStatusID = 2 THEN NULL ELSE CheckerUserID END,
            ApprovedUtc = CASE WHEN @NewStatusID = 3 THEN SYSUTCDATETIME() WHEN @NewStatusID = 2 THEN NULL ELSE ApprovedUtc END,
            PaidUtc = CASE WHEN @NewStatusID = 4 THEN SYSUTCDATETIME() ELSE PaidUtc END
        WHERE CompanyID = @CompanyID AND FnfID = @FnfID;
        UPDATE mst.EmployeeSeparation SET FnFStatusID = @NewStatusID WHERE CompanyID = @CompanyID AND SeparationID = @SeparationID;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Fnf_Get @SeparationID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT FnfID, EmployeeID, SeparationID, ExitDate, StatusID, TotalEarnings, TotalDeductions, NetPayable, MakerUserID, CheckerUserID, CalculatedUtc, ApprovedUtc, PaidUtc
    FROM pay.FnfSettlement WHERE CompanyID = @CompanyID AND SeparationID = @SeparationID;
    SELECT c.ComponentNo, c.ComponentTypeID, c.IsDeduction, c.Description, c.Amount
    FROM pay.FnfComponent c JOIN pay.FnfSettlement s ON s.CompanyID = c.CompanyID AND s.FnfID = c.FnfID
    WHERE s.CompanyID = @CompanyID AND s.SeparationID = @SeparationID ORDER BY c.ComponentNo;
END
GO
