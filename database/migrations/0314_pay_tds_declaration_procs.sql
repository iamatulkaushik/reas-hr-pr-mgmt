/* 0314_pay_tds_declaration_procs.sql
   TDS declaration procedures (Form 124). CREATE OR ALTER keeps it idempotent.
   The company always comes from SESSION_CONTEXT, never from a parameter. No company in the session means no read and no write.
   Error numbers: 50002 no company in session, 50340 declaration is locked, 50341 status change not allowed, 50342 declaration not found,
   50343 rent months overlap or fall outside the financial year, 50344 items, rent or previous employer data not valid, 50345 employee not found.
   Status: 1 Draft, 2 Submitted, 3 Locked. Allowed changes: 1 to 2, 2 to 3, 2 to 1, 3 to 2. Only Locked refuses edits.
   Lists and items are replaced as a whole inside one transaction, so a failed save leaves the old data. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_TdsDeclaration_Get @EmployeeID INT, @FinancialYear SMALLINT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT DeclarationID, EmployeeID, FinancialYear, RegimeID, StatusID, UpdatedUtc
    FROM pay.TdsDeclaration
    WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND FinancialYear = @FinancialYear;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_TdsDeclaration_Save @EmployeeID INT, @FinancialYear SMALLINT, @RegimeID TINYINT, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID)
        THROW 50345, N'Employee not found.', 1;
    DECLARE @Id INT, @Status TINYINT;
    SELECT @Id = DeclarationID, @Status = StatusID FROM pay.TdsDeclaration
    WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND FinancialYear = @FinancialYear;
    IF @Id IS NULL
    BEGIN
        INSERT pay.TdsDeclaration (CompanyID, EmployeeID, FinancialYear, RegimeID, CreatedByUserID)
        VALUES (@CompanyID, @EmployeeID, @FinancialYear, @RegimeID, @UserID);
        SET @Id = CAST(SCOPE_IDENTITY() AS INT);
    END
    ELSE
    BEGIN
        IF @Status = 3 THROW 50340, N'The declaration is locked.', 1;
        UPDATE pay.TdsDeclaration SET RegimeID = @RegimeID, UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
        WHERE CompanyID = @CompanyID AND DeclarationID = @Id;
    END
    SELECT @Id AS DeclarationID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_TdsDeclaration_SetStatus @DeclarationID INT, @StatusID TINYINT, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Status TINYINT = (SELECT StatusID FROM pay.TdsDeclaration WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID);
    IF @Status IS NULL THROW 50342, N'Declaration not found.', 1;
    IF NOT ((@Status = 1 AND @StatusID = 2) OR (@Status = 2 AND @StatusID IN (1, 3)) OR (@Status = 3 AND @StatusID = 2))
        THROW 50341, N'That status change is not allowed.', 1;
    UPDATE pay.TdsDeclaration SET StatusID = @StatusID, UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_TdsDeclarationItem_List @DeclarationID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT ItemNo, SectionCode, SubLimitKey, Description, DeclaredAmount, ProofRef, VerifiedAmount
    FROM pay.TdsDeclarationItem WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID ORDER BY ItemNo;
END
GO
/* @ItemsJson: array of {"No":1,"Section":"80C","SubKey":"","Description":"LIC","Declared":50000.00,"Proof":"R-12","Verified":null}. Replaces all items. */
CREATE OR ALTER PROCEDURE pay.usp_TdsDeclarationItem_Replace @DeclarationID INT, @ItemsJson NVARCHAR(MAX), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Status TINYINT = (SELECT StatusID FROM pay.TdsDeclaration WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID);
    IF @Status IS NULL THROW 50342, N'Declaration not found.', 1;
    IF @Status = 3 THROW 50340, N'The declaration is locked.', 1;
    DECLARE @Items TABLE (ItemNo SMALLINT NOT NULL PRIMARY KEY, SectionCode NVARCHAR(20) NOT NULL, SubLimitKey NVARCHAR(40) NOT NULL,
                          Description NVARCHAR(100) NULL, DeclaredAmount DECIMAL(14,2) NOT NULL, ProofRef NVARCHAR(200) NULL, VerifiedAmount DECIMAL(14,2) NULL);
    BEGIN TRY
        INSERT @Items
        SELECT ItemNo, LTRIM(RTRIM(SectionCode)), LTRIM(RTRIM(ISNULL(SubLimitKey, N''))), Description, DeclaredAmount, ProofRef, VerifiedAmount
        FROM OPENJSON(@ItemsJson) WITH (ItemNo SMALLINT '$.No', SectionCode NVARCHAR(20) '$.Section', SubLimitKey NVARCHAR(40) '$.SubKey',
                                        Description NVARCHAR(100) '$.Description', DeclaredAmount DECIMAL(14,2) '$.Declared',
                                        ProofRef NVARCHAR(200) '$.Proof', VerifiedAmount DECIMAL(14,2) '$.Verified');
    END TRY
    BEGIN CATCH
        THROW 50344, N'The deduction items are not valid.', 1;
    END CATCH
    IF EXISTS (SELECT 1 FROM @Items WHERE ItemNo < 1 OR LEN(SectionCode) = 0 OR DeclaredAmount < 0 OR VerifiedAmount < 0)
        THROW 50344, N'The deduction items are not valid.', 1;
    BEGIN TRANSACTION;
    DELETE pay.TdsDeclarationItem WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID;
    INSERT pay.TdsDeclarationItem (CompanyID, DeclarationID, ItemNo, SectionCode, SubLimitKey, Description, DeclaredAmount, ProofRef, VerifiedAmount)
    SELECT @CompanyID, @DeclarationID, ItemNo, SectionCode, SubLimitKey, Description, DeclaredAmount, ProofRef, VerifiedAmount FROM @Items;
    UPDATE pay.TdsDeclaration SET UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_TdsHraRent_List @DeclarationID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT RentNo, FromMonthKey, ToMonthKey, MonthlyRent, City, LandlordName, LandlordRelationship, LandlordAddress,
           LandlordPanEnc, LandlordPanLast4, IdKeyVersion
    FROM pay.TdsHraRent WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID ORDER BY FromMonthKey, RentNo;
END
GO
/* @RentsJson: array of {"No":1,"From":202604,"To":202609,"Rent":25000.00,"City":"Pune","Landlord":"A Kumar","Relationship":"Friend",
   "Address":"...","PanEnc":"<base64>","PanLast4":"1234","KeyVersion":1}. The application encrypts the PAN (ADR 0011). Replaces all rows.
   Month keys must lie inside the financial year (April of FinancialYear to March of the next) and may not overlap. */
CREATE OR ALTER PROCEDURE pay.usp_TdsHraRent_Replace @DeclarationID INT, @RentsJson NVARCHAR(MAX), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Status TINYINT, @Year SMALLINT;
    SELECT @Status = StatusID, @Year = FinancialYear FROM pay.TdsDeclaration WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID;
    IF @Status IS NULL THROW 50342, N'Declaration not found.', 1;
    IF @Status = 3 THROW 50340, N'The declaration is locked.', 1;
    DECLARE @Rents TABLE (RentNo SMALLINT NOT NULL PRIMARY KEY, FromMonthKey INT NOT NULL, ToMonthKey INT NOT NULL, MonthlyRent DECIMAL(14,2) NOT NULL,
                          City NVARCHAR(100) NOT NULL, LandlordName NVARCHAR(150) NOT NULL, LandlordRelationship NVARCHAR(50) NOT NULL,
                          LandlordAddress NVARCHAR(300) NULL, PanEnc VARBINARY(256) NULL, PanLast4 CHAR(4) NULL, KeyVersion TINYINT NOT NULL);
    BEGIN TRY
        INSERT @Rents
        SELECT RentNo, FromMonthKey, ToMonthKey, MonthlyRent, LTRIM(RTRIM(City)), LTRIM(RTRIM(LandlordName)), LTRIM(RTRIM(LandlordRelationship)),
               LandlordAddress, PanEnc, PanLast4, ISNULL(KeyVersion, 1)
        FROM OPENJSON(@RentsJson) WITH (RentNo SMALLINT '$.No', FromMonthKey INT '$.From', ToMonthKey INT '$.To', MonthlyRent DECIMAL(14,2) '$.Rent',
                                        City NVARCHAR(100) '$.City', LandlordName NVARCHAR(150) '$.Landlord', LandlordRelationship NVARCHAR(50) '$.Relationship',
                                        LandlordAddress NVARCHAR(300) '$.Address', PanEnc VARBINARY(256) '$.PanEnc', PanLast4 CHAR(4) '$.PanLast4',
                                        KeyVersion TINYINT '$.KeyVersion');
    END TRY
    BEGIN CATCH
        THROW 50344, N'The rent rows are not valid.', 1;
    END CATCH
    IF EXISTS (SELECT 1 FROM @Rents WHERE RentNo < 1 OR MonthlyRent <= 0 OR LEN(City) = 0 OR LEN(LandlordName) = 0 OR LEN(LandlordRelationship) = 0
                  OR (PanEnc IS NULL AND PanLast4 IS NOT NULL) OR (PanEnc IS NOT NULL AND PanLast4 IS NULL))
        THROW 50344, N'The rent rows are not valid.', 1;
    DECLARE @FirstKey INT = @Year * 100 + 4, @LastKey INT = (@Year + 1) * 100 + 3;
    IF EXISTS (SELECT 1 FROM @Rents WHERE FromMonthKey < @FirstKey OR ToMonthKey > @LastKey OR FromMonthKey > ToMonthKey
                  OR FromMonthKey % 100 NOT BETWEEN 1 AND 12 OR ToMonthKey % 100 NOT BETWEEN 1 AND 12)
       OR EXISTS (SELECT 1 FROM @Rents a JOIN @Rents b ON a.RentNo < b.RentNo AND a.FromMonthKey <= b.ToMonthKey AND b.FromMonthKey <= a.ToMonthKey)
        THROW 50343, N'The rent months overlap or are outside the financial year.', 1;
    BEGIN TRANSACTION;
    DELETE pay.TdsHraRent WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID;
    INSERT pay.TdsHraRent (CompanyID, DeclarationID, RentNo, FromMonthKey, ToMonthKey, MonthlyRent, City, LandlordName, LandlordRelationship,
                           LandlordAddress, LandlordPanEnc, LandlordPanLast4, IdKeyVersion)
    SELECT @CompanyID, @DeclarationID, RentNo, FromMonthKey, ToMonthKey, MonthlyRent, City, LandlordName, LandlordRelationship,
           LandlordAddress, PanEnc, PanLast4, KeyVersion FROM @Rents;
    UPDATE pay.TdsDeclaration SET UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_TdsPrevEmployer_Get @DeclarationID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT EmployerName, EmployerTan, SalaryIncome, Perquisites, ProvidentFund, ProfessionalTax, TaxDeducted
    FROM pay.TdsPrevEmployer WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_TdsPrevEmployer_Save
    @DeclarationID INT, @EmployerName NVARCHAR(150), @EmployerTan CHAR(10) = NULL, @SalaryIncome DECIMAL(14,2),
    @Perquisites DECIMAL(14,2) = 0, @ProvidentFund DECIMAL(14,2) = 0, @ProfessionalTax DECIMAL(14,2) = 0, @TaxDeducted DECIMAL(14,2), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Status TINYINT = (SELECT StatusID FROM pay.TdsDeclaration WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID);
    IF @Status IS NULL THROW 50342, N'Declaration not found.', 1;
    IF @Status = 3 THROW 50340, N'The declaration is locked.', 1;
    IF LEN(LTRIM(RTRIM(ISNULL(@EmployerName, N'')))) = 0 OR @SalaryIncome < 0 OR @Perquisites < 0 OR @ProvidentFund < 0
       OR @ProfessionalTax < 0 OR @TaxDeducted < 0
        THROW 50344, N'The previous employer data is not valid.', 1;
    BEGIN TRANSACTION;
    DELETE pay.TdsPrevEmployer WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID;
    INSERT pay.TdsPrevEmployer (CompanyID, DeclarationID, EmployerName, EmployerTan, SalaryIncome, Perquisites, ProvidentFund, ProfessionalTax, TaxDeducted)
    VALUES (@CompanyID, @DeclarationID, LTRIM(RTRIM(@EmployerName)), @EmployerTan, @SalaryIncome, @Perquisites, @ProvidentFund, @ProfessionalTax, @TaxDeducted);
    UPDATE pay.TdsDeclaration SET UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE pay.usp_TdsPrevEmployer_Clear @DeclarationID INT, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Status TINYINT = (SELECT StatusID FROM pay.TdsDeclaration WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID);
    IF @Status IS NULL THROW 50342, N'Declaration not found.', 1;
    IF @Status = 3 THROW 50340, N'The declaration is locked.', 1;
    DELETE pay.TdsPrevEmployer WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID;
    UPDATE pay.TdsDeclaration SET UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID;
END
GO
