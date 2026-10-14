/* 0308_pay_loan_procs.sql
   Loan procedures. CREATE OR ALTER keeps it idempotent.
   The company always comes from SESSION_CONTEXT, never from a parameter. No company in the session means no read and no write.
   Error numbers: 50002 no company in session, 50321 loan type not found, 50322 installments do not match the principal,
   50323 loan has recoveries and cannot be cancelled, 50325 loan not found, 50326 run is in the wrong status for this change,
   50327 recovery is more than is outstanding, 50328 recoveries already applied for this run. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_LoanType_List
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT LoanTypeID, Name, InterestMethodID, AnnualInterestPct, MaxInstallments, IsActive
    FROM pay.LoanType WHERE CompanyID = @CompanyID ORDER BY Name;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_LoanType_Save
    @LoanTypeID INT = NULL, @Name NVARCHAR(100), @InterestMethodID TINYINT, @AnnualInterestPct DECIMAL(7,4),
    @MaxInstallments SMALLINT, @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @LoanTypeID IS NULL
    BEGIN
        INSERT pay.LoanType (CompanyID, Name, InterestMethodID, AnnualInterestPct, MaxInstallments, IsActive)
        VALUES (@CompanyID, @Name, @InterestMethodID, @AnnualInterestPct, @MaxInstallments, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS LoanTypeID;
    END
    ELSE
    BEGIN
        UPDATE pay.LoanType
        SET Name = @Name, InterestMethodID = @InterestMethodID, AnnualInterestPct = @AnnualInterestPct, MaxInstallments = @MaxInstallments, IsActive = @IsActive
        WHERE LoanTypeID = @LoanTypeID AND CompanyID = @CompanyID;
        IF @@ROWCOUNT = 0 THROW 50321, N'Loan type not found.', 1;
        SELECT @LoanTypeID AS LoanTypeID;
    END
END
GO
/* @InstallmentsJson: array of {"No":1,"DueMonthKey":202611,"Principal":1000.00,"Interest":100.00,"Amount":1100.00}. The application builds it. */
CREATE OR ALTER PROCEDURE pay.usp_Loan_Create
    @EmployeeID INT, @LoanTypeID INT, @SanctionDate DATE, @Principal DECIMAL(14,2), @InterestMethodID TINYINT,
    @AnnualInterestPct DECIMAL(7,4), @InstallmentsJson NVARCHAR(MAX), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.LoanType WHERE CompanyID = @CompanyID AND LoanTypeID = @LoanTypeID)
        THROW 50321, N'Loan type not found.', 1;
    DECLARE @Inst TABLE (InstallmentNo SMALLINT NOT NULL PRIMARY KEY, DueMonthKey INT NOT NULL, PrincipalPart DECIMAL(14,2) NOT NULL,
                         InterestPart DECIMAL(14,2) NOT NULL, Amount DECIMAL(14,2) NOT NULL);
    BEGIN TRY
        INSERT @Inst
        SELECT InstallmentNo, DueMonthKey, PrincipalPart, InterestPart, Amount
        FROM OPENJSON(@InstallmentsJson) WITH (InstallmentNo SMALLINT '$.No', DueMonthKey INT '$.DueMonthKey',
                                               PrincipalPart DECIMAL(14,2) '$.Principal', InterestPart DECIMAL(14,2) '$.Interest', Amount DECIMAL(14,2) '$.Amount');
    END TRY
    BEGIN CATCH
        THROW 50322, N'The installments do not match the principal.', 1;
    END CATCH
    IF NOT EXISTS (SELECT 1 FROM @Inst)
       OR (SELECT SUM(PrincipalPart) FROM @Inst) <> @Principal
       OR EXISTS (SELECT 1 FROM @Inst WHERE Amount <> PrincipalPart + InterestPart)
       OR (SELECT MIN(InstallmentNo) FROM @Inst) <> 1
       OR (SELECT MAX(InstallmentNo) FROM @Inst) <> (SELECT COUNT(*) FROM @Inst)
        THROW 50322, N'The installments do not match the principal.', 1;
    BEGIN TRANSACTION;
        INSERT pay.Loan (CompanyID, EmployeeID, LoanTypeID, SanctionDate, Principal, InterestMethodID, AnnualInterestPct, InstallmentCount, CreatedByUserID)
        VALUES (@CompanyID, @EmployeeID, @LoanTypeID, @SanctionDate, @Principal, @InterestMethodID, @AnnualInterestPct, (SELECT COUNT(*) FROM @Inst), @UserID);
        DECLARE @LoanID INT = CAST(SCOPE_IDENTITY() AS INT);
        INSERT pay.LoanInstallment (CompanyID, LoanID, InstallmentNo, DueMonthKey, PrincipalPart, InterestPart, Amount)
        SELECT @CompanyID, @LoanID, InstallmentNo, DueMonthKey, PrincipalPart, InterestPart, Amount FROM @Inst;
    COMMIT TRANSACTION;
    SELECT @LoanID AS LoanID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Loan_Cancel @LoanID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM pay.Loan WHERE CompanyID = @CompanyID AND LoanID = @LoanID) THROW 50325, N'Loan not found.', 1;
    IF EXISTS (SELECT 1 FROM pay.LoanRecovery WHERE CompanyID = @CompanyID AND LoanID = @LoanID) THROW 50323, N'The loan has recoveries and cannot be cancelled.', 1;
    UPDATE pay.Loan SET StatusID = 2 WHERE CompanyID = @CompanyID AND LoanID = @LoanID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_Loan_ListByEmployee @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT l.LoanID, l.LoanTypeID, l.SanctionDate, l.Principal, l.InterestMethodID, l.AnnualInterestPct, l.InstallmentCount, l.StatusID,
           (SELECT ISNULL(SUM(i.Amount), 0) FROM pay.LoanInstallment i WHERE i.CompanyID = l.CompanyID AND i.LoanID = l.LoanID)
         - (SELECT ISNULL(SUM(r.Amount), 0) FROM pay.LoanRecovery r WHERE r.CompanyID = l.CompanyID AND r.LoanID = l.LoanID) AS Outstanding
    FROM pay.Loan l WHERE l.CompanyID = @CompanyID AND l.EmployeeID = @EmployeeID
    ORDER BY l.SanctionDate, l.LoanID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_LoanInstallment_List @LoanID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT i.InstallmentNo, i.DueMonthKey, i.PrincipalPart, i.InterestPart, i.Amount,
           ISNULL((SELECT SUM(r.Amount) FROM pay.LoanRecovery r WHERE r.LoanID = i.LoanID AND r.InstallmentNo = i.InstallmentNo), 0) AS Recovered
    FROM pay.LoanInstallment i WHERE i.CompanyID = @CompanyID AND i.LoanID = @LoanID
    ORDER BY i.InstallmentNo;
END
GO
/* Installments of active loans not fully recovered, due in or before @UptoMonthKey. @EmployeeID NULL means every employee of the company. */
CREATE OR ALTER PROCEDURE pay.usp_LoanInstallment_ListOpen @EmployeeID INT = NULL, @UptoMonthKey INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT l.EmployeeID, i.LoanID, i.InstallmentNo, i.DueMonthKey,
           i.Amount - ISNULL(r.Recovered, 0) AS Outstanding
    FROM pay.LoanInstallment i
    JOIN pay.Loan l ON l.CompanyID = i.CompanyID AND l.LoanID = i.LoanID AND l.StatusID = 1
    OUTER APPLY (SELECT SUM(x.Amount) AS Recovered FROM pay.LoanRecovery x WHERE x.LoanID = i.LoanID AND x.InstallmentNo = i.InstallmentNo) r
    WHERE i.CompanyID = @CompanyID AND i.DueMonthKey <= @UptoMonthKey AND (@EmployeeID IS NULL OR l.EmployeeID = @EmployeeID)
      AND i.Amount - ISNULL(r.Recovered, 0) > 0
    ORDER BY l.EmployeeID, i.DueMonthKey, i.LoanID, i.InstallmentNo;
END
GO
/* Applies the recoveries of a Locked run. @RecoveriesJson: array of {"LoanID":1,"InstallmentNo":3,"Amount":1100.00}. */
CREATE OR ALTER PROCEDURE pay.usp_Recovery_Save @RunID INT, @RecoveriesJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Status TINYINT = (SELECT StatusID FROM pay.PayrollRun WITH (UPDLOCK) WHERE CompanyID = @CompanyID AND RunID = @RunID);
    IF @Status IS NULL THROW 50311, N'Run not found.', 1;
    IF @Status <> 5 THROW 50326, N'The run is in the wrong status for this change.', 1;
    IF EXISTS (SELECT 1 FROM pay.LoanRecovery WHERE CompanyID = @CompanyID AND RunID = @RunID) THROW 50328, N'Recoveries are already applied for this run.', 1;
    DECLARE @Rec TABLE (LoanID INT NOT NULL, InstallmentNo SMALLINT NOT NULL, Amount DECIMAL(14,2) NOT NULL, PRIMARY KEY (LoanID, InstallmentNo));
    BEGIN TRY
        INSERT @Rec SELECT LoanID, InstallmentNo, Amount FROM OPENJSON(@RecoveriesJson) WITH (LoanID INT, InstallmentNo SMALLINT, Amount DECIMAL(14,2));
    END TRY
    BEGIN CATCH
        THROW 50327, N'The recovery is more than is outstanding.', 1;
    END CATCH
    IF EXISTS (SELECT 1 FROM @Rec x
               JOIN pay.LoanInstallment i ON i.CompanyID = @CompanyID AND i.LoanID = x.LoanID AND i.InstallmentNo = x.InstallmentNo
               WHERE x.Amount <= 0 OR x.Amount > i.Amount - ISNULL((SELECT SUM(r.Amount) FROM pay.LoanRecovery r WHERE r.LoanID = i.LoanID AND r.InstallmentNo = i.InstallmentNo), 0))
       OR EXISTS (SELECT 1 FROM @Rec x WHERE NOT EXISTS (SELECT 1 FROM pay.LoanInstallment i WHERE i.CompanyID = @CompanyID AND i.LoanID = x.LoanID AND i.InstallmentNo = x.InstallmentNo))
        THROW 50327, N'The recovery is more than is outstanding.', 1;
    INSERT pay.LoanRecovery (CompanyID, LoanID, InstallmentNo, RunID, Amount)
    SELECT @CompanyID, LoanID, InstallmentNo, @RunID, Amount FROM @Rec;
END
GO
/* Removes the recoveries of a run that was reopened to Draft. */
CREATE OR ALTER PROCEDURE pay.usp_Recovery_ReverseRun @RunID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Status TINYINT = (SELECT StatusID FROM pay.PayrollRun WHERE CompanyID = @CompanyID AND RunID = @RunID);
    IF @Status IS NULL THROW 50311, N'Run not found.', 1;
    IF @Status NOT IN (1, 2) THROW 50326, N'The run is in the wrong status for this change.', 1;
    DELETE FROM pay.LoanRecovery WHERE CompanyID = @CompanyID AND RunID = @RunID;
END
GO
