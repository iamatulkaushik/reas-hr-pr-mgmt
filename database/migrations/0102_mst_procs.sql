/* 0102_mst_procs.sql
   Master and employee procedures. CREATE OR ALTER keeps it idempotent.
   The company always comes from SESSION_CONTEXT, never from a parameter.
   Error numbers: 50002 no company in session, 50010 nominee shares do not add up to 100, 50011 employee not found.
   Duplicate employee code, Aadhaar or PAN raise SQL error 2627 or 2601 from the unique keys. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE mst.usp_Department_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DepartmentID, DepartmentName, IsActive FROM mst.Department ORDER BY DepartmentName;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Department_Save @DepartmentID INT = NULL, @DepartmentName NVARCHAR(100), @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @DepartmentID IS NULL
    BEGIN
        INSERT mst.Department (CompanyID, DepartmentName, IsActive) VALUES (@CompanyID, @DepartmentName, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS DepartmentID;
    END
    ELSE
    BEGIN
        UPDATE mst.Department SET DepartmentName = @DepartmentName, IsActive = @IsActive
        WHERE DepartmentID = @DepartmentID AND CompanyID = @CompanyID;
        SELECT @DepartmentID AS DepartmentID;
    END
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Designation_List
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DesignationID, DesignationName, IsActive FROM mst.Designation ORDER BY DesignationName;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Designation_Save @DesignationID INT = NULL, @DesignationName NVARCHAR(100), @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @DesignationID IS NULL
    BEGIN
        INSERT mst.Designation (CompanyID, DesignationName, IsActive) VALUES (@CompanyID, @DesignationName, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS DesignationID;
    END
    ELSE
    BEGIN
        UPDATE mst.Designation SET DesignationName = @DesignationName, IsActive = @IsActive
        WHERE DesignationID = @DesignationID AND CompanyID = @CompanyID;
        SELECT @DesignationID AS DesignationID;
    END
END
GO
/* Next free number for a code prefix, for example EMP. The unique key on (CompanyID, EmpCode) is the final guard against two users racing. */
CREATE OR ALTER PROCEDURE mst.usp_Employee_NextSequence @Prefix NVARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Head NVARCHAR(12) = UPPER(@Prefix) + N'-';
    SELECT ISNULL(MAX(TRY_CONVERT(INT, SUBSTRING(EmpCode, LEN(@Head) + 1, 10))), 0) + 1 AS NextSequence
    FROM mst.Employee
    WHERE EmpCode LIKE @Head + N'%';
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Employee_List @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT EmployeeID, EmpCode, FullName, JoinDate, LeaveDate, PayTypeID, Mobile, FactoryID, DepartmentID, DesignationID, IsActive
    FROM mst.Employee
    WHERE @IncludeInactive = 1 OR IsActive = 1
    ORDER BY EmpCode;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Employee_Get @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT EmployeeID, EmpCode, FullName, DateOfBirth, JoinDate, LeaveDate, PayTypeID, Mobile, FactoryID, DepartmentID,
           DesignationID, BankID, Ifsc, AadhaarEnc, PanEnc, UanEnc, EsiIpEnc, BankAccountEnc, IsActive
    FROM mst.Employee WHERE EmployeeID = @EmployeeID;
    SELECT NomineeID, NomineeName, Relation, DateOfBirth, SharePercent
    FROM mst.Nominee WHERE EmployeeID = @EmployeeID ORDER BY NomineeID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Employee_Save
    @EmployeeID INT = NULL, @EmpCode NVARCHAR(20), @FullName NVARCHAR(150), @DateOfBirth DATE, @JoinDate DATE,
    @PayTypeID TINYINT, @Mobile NVARCHAR(10) = NULL, @FactoryID INT = NULL, @DepartmentID INT = NULL,
    @DesignationID INT = NULL, @BankID INT = NULL, @Ifsc NVARCHAR(11) = NULL,
    @AadhaarEnc VARBINARY(256) = NULL, @PanEnc VARBINARY(256) = NULL, @UanEnc VARBINARY(256) = NULL,
    @EsiIpEnc VARBINARY(256) = NULL, @BankAccountEnc VARBINARY(256) = NULL,
    @AadhaarIndex VARBINARY(32) = NULL, @PanIndex VARBINARY(32) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @EmployeeID IS NULL
    BEGIN
        INSERT mst.Employee (CompanyID, EmpCode, FullName, DateOfBirth, JoinDate, PayTypeID, Mobile, FactoryID, DepartmentID,
                             DesignationID, BankID, Ifsc, AadhaarEnc, PanEnc, UanEnc, EsiIpEnc, BankAccountEnc, AadhaarIndex, PanIndex)
        VALUES (@CompanyID, @EmpCode, @FullName, @DateOfBirth, @JoinDate, @PayTypeID, @Mobile, @FactoryID, @DepartmentID,
                @DesignationID, @BankID, @Ifsc, @AadhaarEnc, @PanEnc, @UanEnc, @EsiIpEnc, @BankAccountEnc, @AadhaarIndex, @PanIndex);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS EmployeeID;
    END
    ELSE
    BEGIN
        UPDATE mst.Employee
        SET EmpCode = @EmpCode, FullName = @FullName, DateOfBirth = @DateOfBirth, JoinDate = @JoinDate, PayTypeID = @PayTypeID,
            Mobile = @Mobile, FactoryID = @FactoryID, DepartmentID = @DepartmentID, DesignationID = @DesignationID,
            BankID = @BankID, Ifsc = @Ifsc, AadhaarEnc = @AadhaarEnc, PanEnc = @PanEnc, UanEnc = @UanEnc,
            EsiIpEnc = @EsiIpEnc, BankAccountEnc = @BankAccountEnc, AadhaarIndex = @AadhaarIndex, PanIndex = @PanIndex
        WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
        IF @@ROWCOUNT = 0 THROW 50011, N'Employee not found.', 1;
        SELECT @EmployeeID AS EmployeeID;
    END
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Employee_SetStatus @EmployeeID INT, @IsActive BIT, @LeaveDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    UPDATE mst.Employee SET IsActive = @IsActive, LeaveDate = CASE WHEN @IsActive = 1 THEN NULL ELSE @LeaveDate END
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
    IF @@ROWCOUNT = 0 THROW 50011, N'Employee not found.', 1;
END
GO
/* Replaces all nominees of an employee. @NomineesJson: [{"name":"..","relation":"..","dob":"yyyy-MM-dd","share":50.00}].
   An empty list clears them. A non-empty list must add up to exactly 100. */
CREATE OR ALTER PROCEDURE mst.usp_Nominee_Replace @EmployeeID INT, @NomineesJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID) THROW 50011, N'Employee not found.', 1;
    DECLARE @Rows TABLE (NomineeName NVARCHAR(150), Relation NVARCHAR(50), DateOfBirth DATE, SharePercent DECIMAL(5,2));
    INSERT @Rows
    SELECT name, relation, dob, share
    FROM OPENJSON(ISNULL(@NomineesJson, N'[]'))
         WITH (name NVARCHAR(150) '$.name', relation NVARCHAR(50) '$.relation', dob DATE '$.dob', share DECIMAL(5,2) '$.share');
    IF EXISTS (SELECT 1 FROM @Rows) AND (SELECT SUM(SharePercent) FROM @Rows) <> 100
        THROW 50010, N'Nominee shares must add up to 100.', 1;
    BEGIN TRAN;
    DELETE mst.Nominee WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
    INSERT mst.Nominee (CompanyID, EmployeeID, NomineeName, Relation, DateOfBirth, SharePercent)
    SELECT @CompanyID, @EmployeeID, NomineeName, Relation, DateOfBirth, SharePercent FROM @Rows;
    COMMIT;
END
GO
