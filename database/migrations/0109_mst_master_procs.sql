/* 0105_mst_master_procs.sql
   Stored procedures for reference lists and company masters. CREATE OR ALTER keeps it idempotent.
   The company always comes from SESSION_CONTEXT, never from a parameter. No company in the session means no read and no write.
   Error numbers: 50002 no company in session, 51005 record not found in this company. Duplicate codes raise SQL error 2627. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE mst.usp_State_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT StateID, StateName FROM mst.[State] WHERE IsActive = 1 ORDER BY StateName;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_District_List @StateID TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DistrictID, StateID, DistrictName FROM mst.District WHERE StateID = @StateID AND IsActive = 1 ORDER BY DistrictName;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Bank_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT BankID, BankName, IfscPrefix FROM mst.Bank WHERE IsActive = 1 ORDER BY BankName;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_DocumentType_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DocumentTypeID, DocumentName, IsMandatory, HasExpiry FROM mst.DocumentType ORDER BY DocumentName;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_CompanyProfile_Get
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT CompanyID, LegalName, AddressLine1, AddressLine2, City, StateID, Pincode, Pan, Tan, Gstin,
           EpfEstablishmentCode, EsiEmployerCode, PtRegistrationNo, EmployeeCodePrefix
    FROM mst.CompanyProfile
    WHERE CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_CompanyProfile_Save
    @LegalName NVARCHAR(200), @AddressLine1 NVARCHAR(150) = NULL, @AddressLine2 NVARCHAR(150) = NULL, @City NVARCHAR(100) = NULL,
    @StateID TINYINT = NULL, @Pincode CHAR(6) = NULL, @Pan CHAR(10) = NULL, @Tan CHAR(10) = NULL, @Gstin CHAR(15) = NULL,
    @EpfEstablishmentCode NVARCHAR(30) = NULL, @EsiEmployerCode CHAR(17) = NULL, @PtRegistrationNo NVARCHAR(30) = NULL,
    @EmployeeCodePrefix NVARCHAR(10) = N'EMP'
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    UPDATE mst.CompanyProfile
    SET LegalName = @LegalName, AddressLine1 = @AddressLine1, AddressLine2 = @AddressLine2, City = @City, StateID = @StateID,
        Pincode = @Pincode, Pan = @Pan, Tan = @Tan, Gstin = @Gstin, EpfEstablishmentCode = @EpfEstablishmentCode,
        EsiEmployerCode = @EsiEmployerCode, PtRegistrationNo = @PtRegistrationNo,
        EmployeeCodePrefix = UPPER(LTRIM(RTRIM(@EmployeeCodePrefix))), UpdatedUtc = SYSUTCDATETIME()
    WHERE CompanyID = @CompanyID;
    IF @@ROWCOUNT = 0
        INSERT mst.CompanyProfile (CompanyID, LegalName, AddressLine1, AddressLine2, City, StateID, Pincode, Pan, Tan, Gstin,
                                   EpfEstablishmentCode, EsiEmployerCode, PtRegistrationNo, EmployeeCodePrefix)
        VALUES (@CompanyID, @LegalName, @AddressLine1, @AddressLine2, @City, @StateID, @Pincode, @Pan, @Tan, @Gstin,
                @EpfEstablishmentCode, @EsiEmployerCode, @PtRegistrationNo, UPPER(LTRIM(RTRIM(@EmployeeCodePrefix))));
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Factory_List @ActiveOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT FactoryID, FactoryCode, FactoryName, AddressLine1, AddressLine2, City, StateID, FactoryLicenceNo, LicenceValidTo,
           MaxWorkers, ManagerName, EpfSubCode, EsiSubCode, IsActive
    FROM mst.Factory
    WHERE CompanyID = @CompanyID AND (@ActiveOnly = 0 OR IsActive = 1)
    ORDER BY FactoryName;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Factory_Save
    @FactoryID INT = NULL, @FactoryCode NVARCHAR(10), @FactoryName NVARCHAR(150), @AddressLine1 NVARCHAR(150) = NULL,
    @AddressLine2 NVARCHAR(150) = NULL, @City NVARCHAR(100) = NULL, @StateID TINYINT, @FactoryLicenceNo NVARCHAR(50) = NULL,
    @LicenceValidTo DATE = NULL, @MaxWorkers INT = NULL, @ManagerName NVARCHAR(120) = NULL, @EpfSubCode NVARCHAR(20) = NULL,
    @EsiSubCode NVARCHAR(17) = NULL, @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @FactoryID IS NULL
    BEGIN
        INSERT mst.Factory (CompanyID, FactoryCode, FactoryName, AddressLine1, AddressLine2, City, StateID, FactoryLicenceNo,
                            LicenceValidTo, MaxWorkers, ManagerName, EpfSubCode, EsiSubCode, IsActive)
        VALUES (@CompanyID, @FactoryCode, @FactoryName, @AddressLine1, @AddressLine2, @City, @StateID, @FactoryLicenceNo,
                @LicenceValidTo, @MaxWorkers, @ManagerName, @EpfSubCode, @EsiSubCode, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS FactoryID;
    END
    ELSE
    BEGIN
        UPDATE mst.Factory
        SET FactoryCode = @FactoryCode, FactoryName = @FactoryName, AddressLine1 = @AddressLine1, AddressLine2 = @AddressLine2,
            City = @City, StateID = @StateID, FactoryLicenceNo = @FactoryLicenceNo, LicenceValidTo = @LicenceValidTo,
            MaxWorkers = @MaxWorkers, ManagerName = @ManagerName, EpfSubCode = @EpfSubCode, EsiSubCode = @EsiSubCode, IsActive = @IsActive
        WHERE FactoryID = @FactoryID AND CompanyID = @CompanyID;
        IF @@ROWCOUNT = 0 THROW 51005, N'Factory not found.', 1;
        SELECT @FactoryID AS FactoryID;
    END
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Department_List @FactoryID INT = NULL, @ActiveOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT DepartmentID, FactoryID, DepartmentCode, DepartmentName, IsHazardous, IsActive
    FROM mst.Department
    WHERE CompanyID = @CompanyID AND (@FactoryID IS NULL OR FactoryID = @FactoryID) AND (@ActiveOnly = 0 OR IsActive = 1)
    ORDER BY DepartmentName;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Department_Save
    @DepartmentID INT = NULL, @FactoryID INT, @DepartmentCode NVARCHAR(10), @DepartmentName NVARCHAR(100),
    @IsHazardous BIT = 0, @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @DepartmentID IS NULL
    BEGIN
        INSERT mst.Department (CompanyID, FactoryID, DepartmentCode, DepartmentName, IsHazardous, IsActive)
        VALUES (@CompanyID, @FactoryID, @DepartmentCode, @DepartmentName, @IsHazardous, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS DepartmentID;
    END
    ELSE
    BEGIN
        /* The factory of an existing department is fixed: employees were placed under it. */
        UPDATE mst.Department
        SET DepartmentCode = @DepartmentCode, DepartmentName = @DepartmentName, IsHazardous = @IsHazardous, IsActive = @IsActive
        WHERE DepartmentID = @DepartmentID AND CompanyID = @CompanyID;
        IF @@ROWCOUNT = 0 THROW 51005, N'Department not found.', 1;
        SELECT @DepartmentID AS DepartmentID;
    END
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Section_List @DepartmentID INT = NULL, @ActiveOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT SectionID, DepartmentID, SectionCode, SectionName, IsActive
    FROM mst.Section
    WHERE CompanyID = @CompanyID AND (@DepartmentID IS NULL OR DepartmentID = @DepartmentID) AND (@ActiveOnly = 0 OR IsActive = 1)
    ORDER BY SectionName;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Section_Save
    @SectionID INT = NULL, @DepartmentID INT, @SectionCode NVARCHAR(10), @SectionName NVARCHAR(100), @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @SectionID IS NULL
    BEGIN
        INSERT mst.Section (CompanyID, DepartmentID, SectionCode, SectionName, IsActive)
        VALUES (@CompanyID, @DepartmentID, @SectionCode, @SectionName, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS SectionID;
    END
    ELSE
    BEGIN
        UPDATE mst.Section SET SectionCode = @SectionCode, SectionName = @SectionName, IsActive = @IsActive
        WHERE SectionID = @SectionID AND CompanyID = @CompanyID;
        IF @@ROWCOUNT = 0 THROW 51005, N'Section not found.', 1;
        SELECT @SectionID AS SectionID;
    END
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Grade_List @ActiveOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT GradeID, GradeCode, GradeName, GradeLevel, MinGross, MaxGross, IsActive
    FROM mst.Grade
    WHERE CompanyID = @CompanyID AND (@ActiveOnly = 0 OR IsActive = 1)
    ORDER BY GradeLevel, GradeCode;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Grade_Save
    @GradeID INT = NULL, @GradeCode NVARCHAR(10), @GradeName NVARCHAR(60), @GradeLevel INT,
    @MinGross DECIMAL(14,2) = NULL, @MaxGross DECIMAL(14,2) = NULL, @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @GradeID IS NULL
    BEGIN
        INSERT mst.Grade (CompanyID, GradeCode, GradeName, GradeLevel, MinGross, MaxGross, IsActive)
        VALUES (@CompanyID, @GradeCode, @GradeName, @GradeLevel, @MinGross, @MaxGross, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS GradeID;
    END
    ELSE
    BEGIN
        UPDATE mst.Grade
        SET GradeCode = @GradeCode, GradeName = @GradeName, GradeLevel = @GradeLevel, MinGross = @MinGross, MaxGross = @MaxGross, IsActive = @IsActive
        WHERE GradeID = @GradeID AND CompanyID = @CompanyID;
        IF @@ROWCOUNT = 0 THROW 51005, N'Grade not found.', 1;
        SELECT @GradeID AS GradeID;
    END
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Designation_List @ActiveOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT DesignationID, DesignationCode, DesignationName, GradeID, CategoryID, IsActive
    FROM mst.Designation
    WHERE CompanyID = @CompanyID AND (@ActiveOnly = 0 OR IsActive = 1)
    ORDER BY DesignationName;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Designation_Save
    @DesignationID INT = NULL, @DesignationCode NVARCHAR(10), @DesignationName NVARCHAR(100), @GradeID INT = NULL,
    @CategoryID TINYINT, @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @DesignationID IS NULL
    BEGIN
        INSERT mst.Designation (CompanyID, DesignationCode, DesignationName, GradeID, CategoryID, IsActive)
        VALUES (@CompanyID, @DesignationCode, @DesignationName, @GradeID, @CategoryID, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS DesignationID;
    END
    ELSE
    BEGIN
        UPDATE mst.Designation
        SET DesignationCode = @DesignationCode, DesignationName = @DesignationName, GradeID = @GradeID, CategoryID = @CategoryID, IsActive = @IsActive
        WHERE DesignationID = @DesignationID AND CompanyID = @CompanyID;
        IF @@ROWCOUNT = 0 THROW 51005, N'Designation not found.', 1;
        SELECT @DesignationID AS DesignationID;
    END
END
GO
