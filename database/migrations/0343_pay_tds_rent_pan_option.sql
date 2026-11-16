/* 0343_pay_tds_rent_pan_option.sql
   Change request 0029. Brave lane 0300-0399. Idempotent. Never edit after merge.
   pay.TdsHraRent.PanOption: what the employee chose when no landlord PAN is given (Form 124 needs the PAN when the yearly rent to one landlord is above 1,00,000).
     0 = PanGiven (a PAN is on the row, or the rent does not need one); 1 = NoPanDeclared (landlord has no PAN, written declaration held, exemption still allowed);
     2 = ExemptionNotClaimed (saved without a PAN, no HRA exemption for these months). Default 0, so every row written before this migration stays valid.
   A row with a PAN must have PanOption 0. The rent-above-threshold rule stays in the application (the threshold is a CA-signed rule value, CR 0018), as before.
   The two procedures below are the 0314 ones with the new column: usp_TdsHraRent_List and usp_TdsHraRent_Replace (JSON field PanOption is optional, missing means 0). */
SET XACT_ABORT ON;
GO
IF COL_LENGTH(N'pay.TdsHraRent', N'PanOption') IS NULL
    ALTER TABLE pay.TdsHraRent ADD PanOption TINYINT NOT NULL CONSTRAINT DF_TdsHraRent_PanOption DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_TdsHraRent_PanOption')
    EXEC (N'ALTER TABLE pay.TdsHraRent ADD CONSTRAINT CK_TdsHraRent_PanOption CHECK (PanOption IN (0, 1, 2) AND (PanOption = 0 OR LandlordPanEnc IS NULL))');
GO
CREATE OR ALTER PROCEDURE pay.usp_TdsHraRent_List @DeclarationID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT RentNo, FromMonthKey, ToMonthKey, MonthlyRent, City, LandlordName, LandlordRelationship, LandlordAddress,
           LandlordPanEnc, LandlordPanLast4, IdKeyVersion, PanOption
    FROM pay.TdsHraRent WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID ORDER BY FromMonthKey, RentNo;
END
GO
/* @RentsJson: array of {"No":1,"From":202604,"To":202609,"Rent":25000.00,"City":"Pune","Landlord":"A Kumar","Relationship":"Friend",
   "Address":"...","PanEnc":"<base64>","PanLast4":"1234","KeyVersion":1,"PanOption":0}. The application encrypts the PAN (ADR 0011). Replaces all rows.
   Month keys must lie inside the financial year (April of FinancialYear to March of the next) and may not overlap.
   PanOption is optional (missing means 0); 1 or 2 are refused when a PAN is present. */
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
                          LandlordAddress NVARCHAR(300) NULL, PanEnc VARBINARY(256) NULL, PanLast4 CHAR(4) NULL, KeyVersion TINYINT NOT NULL,
                          PanOption TINYINT NOT NULL);
    BEGIN TRY
        INSERT @Rents
        SELECT RentNo, FromMonthKey, ToMonthKey, MonthlyRent, LTRIM(RTRIM(City)), LTRIM(RTRIM(LandlordName)), LTRIM(RTRIM(LandlordRelationship)),
               LandlordAddress, PanEnc, PanLast4, ISNULL(KeyVersion, 1), ISNULL(PanOption, 0)
        FROM OPENJSON(@RentsJson) WITH (RentNo SMALLINT '$.No', FromMonthKey INT '$.From', ToMonthKey INT '$.To', MonthlyRent DECIMAL(14,2) '$.Rent',
                                        City NVARCHAR(100) '$.City', LandlordName NVARCHAR(150) '$.Landlord', LandlordRelationship NVARCHAR(50) '$.Relationship',
                                        LandlordAddress NVARCHAR(300) '$.Address', PanEnc VARBINARY(256) '$.PanEnc', PanLast4 CHAR(4) '$.PanLast4',
                                        KeyVersion TINYINT '$.KeyVersion', PanOption TINYINT '$.PanOption');
    END TRY
    BEGIN CATCH
        THROW 50344, N'The rent rows are not valid.', 1;
    END CATCH
    IF EXISTS (SELECT 1 FROM @Rents WHERE RentNo < 1 OR MonthlyRent <= 0 OR LEN(City) = 0 OR LEN(LandlordName) = 0 OR LEN(LandlordRelationship) = 0
                  OR (PanEnc IS NULL AND PanLast4 IS NOT NULL) OR (PanEnc IS NOT NULL AND PanLast4 IS NULL)
                  OR PanOption NOT IN (0, 1, 2) OR (PanOption <> 0 AND PanEnc IS NOT NULL))
        THROW 50344, N'The rent rows are not valid.', 1;
    DECLARE @FirstKey INT = @Year * 100 + 4, @LastKey INT = (@Year + 1) * 100 + 3;
    IF EXISTS (SELECT 1 FROM @Rents WHERE FromMonthKey < @FirstKey OR ToMonthKey > @LastKey OR FromMonthKey > ToMonthKey
                  OR FromMonthKey % 100 NOT BETWEEN 1 AND 12 OR ToMonthKey % 100 NOT BETWEEN 1 AND 12)
       OR EXISTS (SELECT 1 FROM @Rents a JOIN @Rents b ON a.RentNo < b.RentNo AND a.FromMonthKey <= b.ToMonthKey AND b.FromMonthKey <= a.ToMonthKey)
        THROW 50343, N'The rent months overlap or are outside the financial year.', 1;
    BEGIN TRANSACTION;
    DELETE pay.TdsHraRent WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID;
    INSERT pay.TdsHraRent (CompanyID, DeclarationID, RentNo, FromMonthKey, ToMonthKey, MonthlyRent, City, LandlordName, LandlordRelationship,
                           LandlordAddress, LandlordPanEnc, LandlordPanLast4, IdKeyVersion, PanOption)
    SELECT @CompanyID, @DeclarationID, RentNo, FromMonthKey, ToMonthKey, MonthlyRent, City, LandlordName, LandlordRelationship,
           LandlordAddress, PanEnc, PanLast4, KeyVersion, PanOption FROM @Rents;
    UPDATE pay.TdsDeclaration SET UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID WHERE CompanyID = @CompanyID AND DeclarationID = @DeclarationID;
    COMMIT TRANSACTION;
END
GO
