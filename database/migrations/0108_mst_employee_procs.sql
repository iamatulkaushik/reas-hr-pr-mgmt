/* 0106_mst_employee_procs.sql
   Stored procedures for employees and their detail tables. CREATE OR ALTER keeps it idempotent.
   The company always comes from SESSION_CONTEXT, never from a parameter (reads included).
   Protected ids arrive already encrypted from the application (ADR 0004). The *_Get procedures return only the last four characters.
   The *_GetEncrypted procedures return ciphertext: the service must hold the Reveal permission (0013) and call aud.usp_Reveal_Insert.
   A set of ids is re-encrypted together: IdKeyVersion is one value per row.
   Error numbers: 50002 no company in session, 51002 employee not found, 51003 nominee shares above 100 percent,
   51005 record not found in this company, 51006 the row was changed by someone else, 51007 invalid exit status.
   Duplicate employee code, PAN, Aadhaar, UAN, ESI IP or biometric id raise SQL error 2601 or 2627. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE mst.usp_Employee_Create
    @EmpCode NVARCHAR(20) = NULL, @FullName NVARCHAR(100), @FatherOrSpouseName NVARCHAR(120) = NULL, @Gender CHAR(1), @DateOfBirth DATE, @JoinDate DATE,
    @EmploymentTypeID TINYINT, @WorkerTypeID TINYINT = 1, @PayTypeID TINYINT, @FactoryID INT, @DepartmentID INT = NULL,
    @SectionID INT = NULL, @DesignationID INT = NULL, @GradeID INT = NULL, @ReportingToEmployeeID INT = NULL,
    @BiometricID INT = NULL, @MaritalStatusID TINYINT = NULL, @Nationality NVARCHAR(30) = N'Indian',
    @Mobile CHAR(10) = NULL, @Email NVARCHAR(200) = NULL, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    /* No code given: take the next number for this company. The counter row is locked, so two users never get the same number. */
    DECLARE @Seq INT, @Prefix NVARCHAR(10);
    BEGIN TRAN;
    IF @EmpCode IS NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM mst.EmployeeCodeSeq WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID)
            INSERT mst.EmployeeCodeSeq (CompanyID, LastSeq) VALUES (@CompanyID, 0);
        UPDATE mst.EmployeeCodeSeq SET @Seq = LastSeq = LastSeq + 1 WHERE CompanyID = @CompanyID;
        SET @Prefix = COALESCE((SELECT EmployeeCodePrefix FROM mst.CompanyProfile WHERE CompanyID = @CompanyID), N'EMP');
        SET @EmpCode = @Prefix + N'-' + CASE WHEN @Seq < 100000 THEN RIGHT(CONCAT(N'00000', @Seq), 5) ELSE CAST(@Seq AS NVARCHAR(10)) END;
    END
    INSERT mst.Employee (CompanyID, EmpCode, FullName, FatherOrSpouseName, Gender, DateOfBirth, JoinDate, EmploymentTypeID, WorkerTypeID,
                         PayTypeID, FactoryID, DepartmentID, SectionID, DesignationID, GradeID, ReportingToEmployeeID, BiometricID,
                         MaritalStatusID, Nationality, Mobile, Email, CreatedByUserID)
    VALUES (@CompanyID, @EmpCode, @FullName, @FatherOrSpouseName, @Gender, @DateOfBirth, @JoinDate, @EmploymentTypeID, @WorkerTypeID,
            @PayTypeID, @FactoryID, @DepartmentID, @SectionID, @DesignationID, @GradeID, @ReportingToEmployeeID, @BiometricID,
            @MaritalStatusID, @Nationality, @Mobile, @Email, @UserID);
    DECLARE @NewID INT = SCOPE_IDENTITY();
    COMMIT;
    SELECT @NewID AS EmployeeID, @EmpCode AS EmpCode;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Employee_Update
    @EmployeeID INT, @RowVer BINARY(8) = NULL, @FullName NVARCHAR(100), @FatherOrSpouseName NVARCHAR(120) = NULL, @Gender CHAR(1), @DateOfBirth DATE, @JoinDate DATE,
    @EmploymentTypeID TINYINT, @WorkerTypeID TINYINT = 1, @PayTypeID TINYINT, @FactoryID INT, @DepartmentID INT = NULL,
    @SectionID INT = NULL, @DesignationID INT = NULL, @GradeID INT = NULL, @ReportingToEmployeeID INT = NULL,
    @BiometricID INT = NULL, @MaritalStatusID TINYINT = NULL, @Nationality NVARCHAR(30) = N'Indian',
    @Mobile CHAR(10) = NULL, @Email NVARCHAR(200) = NULL, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    UPDATE mst.Employee
    SET FullName = @FullName, FatherOrSpouseName = @FatherOrSpouseName, Gender = @Gender, DateOfBirth = @DateOfBirth, JoinDate = @JoinDate,
        EmploymentTypeID = @EmploymentTypeID, WorkerTypeID = @WorkerTypeID, PayTypeID = @PayTypeID, FactoryID = @FactoryID,
        DepartmentID = @DepartmentID, SectionID = @SectionID, DesignationID = @DesignationID, GradeID = @GradeID,
        ReportingToEmployeeID = @ReportingToEmployeeID, BiometricID = @BiometricID, MaritalStatusID = @MaritalStatusID,
        Nationality = @Nationality, Mobile = @Mobile, Email = @Email, UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID AND (@RowVer IS NULL OR RowVer = @RowVer);
    IF @@ROWCOUNT = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM mst.Employee WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID)
            THROW 51006, N'The employee was changed by someone else. Reload and try again.', 1;
        THROW 51002, N'Employee not found.', 1;
    END
    SELECT @EmployeeID AS EmployeeID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Employee_GetById
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT e.EmployeeID, e.EmpCode, e.FullName, e.FatherOrSpouseName, e.Gender, e.DateOfBirth, e.JoinDate, e.ExitDate,
           e.EmploymentTypeID, e.WorkerTypeID, e.PayTypeID, e.FactoryID, f.FactoryName, e.DepartmentID, d.DepartmentName, e.SectionID,
           e.DesignationID, g.DesignationName, e.GradeID, e.ReportingToEmployeeID, e.BiometricID, e.MaritalStatusID, e.Nationality,
           e.Mobile, e.Email, e.StatusID, e.IsActive, e.IsAdolescent, e.RowVer
    FROM mst.Employee e
    JOIN mst.Factory f ON f.FactoryID = e.FactoryID AND f.CompanyID = e.CompanyID
    LEFT JOIN mst.Department d ON d.DepartmentID = e.DepartmentID AND d.CompanyID = e.CompanyID
    LEFT JOIN mst.Designation g ON g.DesignationID = e.DesignationID AND g.CompanyID = e.CompanyID
    WHERE e.EmployeeID = @EmployeeID AND e.CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Employee_List
    @Search NVARCHAR(100) = NULL, @FactoryID INT = NULL, @ActiveOnly BIT = 1, @Top INT = 500
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    /* CHARINDEX, not LIKE: a % or _ typed in the search box is just a character. */
    SELECT TOP (@Top) e.EmployeeID, e.EmpCode, e.FullName, e.FactoryID, f.FactoryName, d.DepartmentName, g.DesignationName,
           e.JoinDate, e.StatusID, e.IsActive
    FROM mst.Employee e
    JOIN mst.Factory f ON f.FactoryID = e.FactoryID AND f.CompanyID = e.CompanyID
    LEFT JOIN mst.Department d ON d.DepartmentID = e.DepartmentID AND d.CompanyID = e.CompanyID
    LEFT JOIN mst.Designation g ON g.DesignationID = e.DesignationID AND g.CompanyID = e.CompanyID
    WHERE e.CompanyID = @CompanyID
      AND (@FactoryID IS NULL OR e.FactoryID = @FactoryID)
      AND (@ActiveOnly = 0 OR e.IsActive = 1)
      AND (@Search IS NULL OR CHARINDEX(@Search, e.FullName) > 0 OR CHARINDEX(@Search, e.EmpCode) > 0)
    ORDER BY e.EmpCode;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Employee_Exit
    @EmployeeID INT, @ExitDate DATE, @StatusID TINYINT, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @StatusID NOT IN (3, 4, 5, 6) THROW 51007, N'Exit status must be Left, Retired, Deceased or Absconded.', 1;
    UPDATE mst.Employee
    SET ExitDate = @ExitDate, StatusID = @StatusID, IsActive = 0, UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
    IF @@ROWCOUNT = 0 THROW 51002, N'Employee not found.', 1;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_Employee_Reactivate
    @EmployeeID INT, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    UPDATE mst.Employee
    SET ExitDate = NULL, StatusID = 1, IsActive = 1, UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
    IF @@ROWCOUNT = 0 THROW 51002, N'Employee not found.', 1;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeStatutory_Get
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT EmployeeID, IsEpfMember, PfNumber, EpfJoinDate, IsPensionMember, HigherWageOption, IsEsiMember, EsiDispensary, PtApplicable,
           LwfApplicable, TaxRegimeID, IsInternationalWorker, UanLast4, EsiIpLast4, PanLast4, AadhaarLast4, IdKeyVersion
    FROM mst.EmployeeStatutory
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeStatutory_GetEncrypted
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT EmployeeID, IdKeyVersion, UanEnc, EsiIpEnc, PanEnc, AadhaarEnc
    FROM mst.EmployeeStatutory
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeStatutory_SaveFlags
    @EmployeeID INT, @IsEpfMember BIT, @PfNumber NVARCHAR(30) = NULL, @EpfJoinDate DATE = NULL, @IsPensionMember BIT = 1,
    @HigherWageOption BIT = 0, @IsEsiMember BIT, @EsiDispensary NVARCHAR(100) = NULL, @PtApplicable BIT = 1, @LwfApplicable BIT = 1,
    @TaxRegimeID TINYINT = 2, @IsInternationalWorker BIT = 0, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID)
        THROW 51002, N'Employee not found.', 1;
    UPDATE mst.EmployeeStatutory
    SET IsEpfMember = @IsEpfMember, PfNumber = @PfNumber, EpfJoinDate = @EpfJoinDate, IsPensionMember = @IsPensionMember,
        HigherWageOption = @HigherWageOption, IsEsiMember = @IsEsiMember, EsiDispensary = @EsiDispensary, PtApplicable = @PtApplicable,
        LwfApplicable = @LwfApplicable, TaxRegimeID = @TaxRegimeID, IsInternationalWorker = @IsInternationalWorker,
        UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
    IF @@ROWCOUNT = 0
        INSERT mst.EmployeeStatutory (CompanyID, EmployeeID, IsEpfMember, PfNumber, EpfJoinDate, IsPensionMember, HigherWageOption, IsEsiMember,
                                      EsiDispensary, PtApplicable, LwfApplicable, TaxRegimeID, IsInternationalWorker, UpdatedByUserID)
        VALUES (@CompanyID, @EmployeeID, @IsEpfMember, @PfNumber, @EpfJoinDate, @IsPensionMember, @HigherWageOption, @IsEsiMember,
                @EsiDispensary, @PtApplicable, @LwfApplicable, @TaxRegimeID, @IsInternationalWorker, @UserID);
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeStatutory_SetIdentifiers
    @EmployeeID INT, @IdKeyVersion TINYINT,
    @SetUan BIT = 0, @UanEnc VARBINARY(256) = NULL, @UanHash BINARY(32) = NULL, @UanLast4 CHAR(4) = NULL,
    @SetEsiIp BIT = 0, @EsiIpEnc VARBINARY(256) = NULL, @EsiIpHash BINARY(32) = NULL, @EsiIpLast4 CHAR(4) = NULL,
    @SetPan BIT = 0, @PanEnc VARBINARY(256) = NULL, @PanHash BINARY(32) = NULL, @PanLast4 CHAR(4) = NULL,
    @SetAadhaar BIT = 0, @AadhaarEnc VARBINARY(256) = NULL, @AadhaarHash BINARY(32) = NULL, @AadhaarLast4 CHAR(4) = NULL,
    @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID)
        THROW 51002, N'Employee not found.', 1;
    /* A Set flag of 1 replaces that id (NULLs clear it). A flag of 0 leaves it as it is. */
    IF NOT EXISTS (SELECT 1 FROM mst.EmployeeStatutory WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID)
        INSERT mst.EmployeeStatutory (CompanyID, EmployeeID) VALUES (@CompanyID, @EmployeeID);
    UPDATE mst.EmployeeStatutory
    SET UanEnc = CASE WHEN @SetUan = 1 THEN @UanEnc ELSE UanEnc END,
        UanHash = CASE WHEN @SetUan = 1 THEN @UanHash ELSE UanHash END,
        UanLast4 = CASE WHEN @SetUan = 1 THEN @UanLast4 ELSE UanLast4 END,
        EsiIpEnc = CASE WHEN @SetEsiIp = 1 THEN @EsiIpEnc ELSE EsiIpEnc END,
        EsiIpHash = CASE WHEN @SetEsiIp = 1 THEN @EsiIpHash ELSE EsiIpHash END,
        EsiIpLast4 = CASE WHEN @SetEsiIp = 1 THEN @EsiIpLast4 ELSE EsiIpLast4 END,
        PanEnc = CASE WHEN @SetPan = 1 THEN @PanEnc ELSE PanEnc END,
        PanHash = CASE WHEN @SetPan = 1 THEN @PanHash ELSE PanHash END,
        PanLast4 = CASE WHEN @SetPan = 1 THEN @PanLast4 ELSE PanLast4 END,
        AadhaarEnc = CASE WHEN @SetAadhaar = 1 THEN @AadhaarEnc ELSE AadhaarEnc END,
        AadhaarHash = CASE WHEN @SetAadhaar = 1 THEN @AadhaarHash ELSE AadhaarHash END,
        AadhaarLast4 = CASE WHEN @SetAadhaar = 1 THEN @AadhaarLast4 ELSE AadhaarLast4 END,
        IdKeyVersion = CASE WHEN @SetUan = 1 OR @SetEsiIp = 1 OR @SetPan = 1 OR @SetAadhaar = 1 THEN @IdKeyVersion ELSE IdKeyVersion END,
        UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeBank_Get
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT EmployeeID, PaymentModeID, BankID, Ifsc, AccountLast4, IdKeyVersion
    FROM mst.EmployeeBank
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeBank_GetEncrypted
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT EmployeeID, IdKeyVersion, AccountEnc
    FROM mst.EmployeeBank
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeBank_Save
    @EmployeeID INT, @PaymentModeID TINYINT = 1, @BankID INT = NULL, @Ifsc CHAR(11) = NULL, @IdKeyVersion TINYINT = 1,
    @SetAccount BIT = 0, @AccountEnc VARBINARY(256) = NULL, @AccountLast4 CHAR(4) = NULL, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID)
        THROW 51002, N'Employee not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.EmployeeBank WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID)
    BEGIN
        INSERT mst.EmployeeBank (CompanyID, EmployeeID, PaymentModeID, BankID, Ifsc, AccountEnc, AccountLast4, IdKeyVersion, UpdatedByUserID)
        VALUES (@CompanyID, @EmployeeID, @PaymentModeID, @BankID, @Ifsc, @AccountEnc, @AccountLast4, @IdKeyVersion, @UserID);
        RETURN;
    END
    UPDATE mst.EmployeeBank
    SET PaymentModeID = @PaymentModeID, BankID = @BankID, Ifsc = @Ifsc,
        AccountEnc = CASE WHEN @SetAccount = 1 THEN @AccountEnc ELSE AccountEnc END,
        AccountLast4 = CASE WHEN @SetAccount = 1 THEN @AccountLast4 ELSE AccountLast4 END,
        IdKeyVersion = CASE WHEN @SetAccount = 1 THEN @IdKeyVersion ELSE IdKeyVersion END,
        UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeAddress_List
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT AddressTypeID, Line1, Line2, City, StateID, Pincode
    FROM mst.EmployeeAddress
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID
    ORDER BY AddressTypeID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeAddress_Save
    @EmployeeID INT, @AddressTypeID TINYINT, @Line1 NVARCHAR(200) = NULL, @Line2 NVARCHAR(200) = NULL,
    @City NVARCHAR(100) = NULL, @StateID TINYINT = NULL, @Pincode CHAR(6) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID)
        THROW 51002, N'Employee not found.', 1;
    UPDATE mst.EmployeeAddress
    SET Line1 = @Line1, Line2 = @Line2, City = @City, StateID = @StateID, Pincode = @Pincode
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID AND AddressTypeID = @AddressTypeID;
    IF @@ROWCOUNT = 0
        INSERT mst.EmployeeAddress (CompanyID, EmployeeID, AddressTypeID, Line1, Line2, City, StateID, Pincode)
        VALUES (@CompanyID, @EmployeeID, @AddressTypeID, @Line1, @Line2, @City, @StateID, @Pincode);
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeFamily_List
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT FamilyID, MemberName, Relation, DateOfBirth, IsDependent, IsResiding
    FROM mst.EmployeeFamily
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID
    ORDER BY FamilyID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeFamily_Save
    @FamilyID INT = NULL, @EmployeeID INT, @MemberName NVARCHAR(120), @Relation NVARCHAR(30), @DateOfBirth DATE = NULL,
    @IsDependent BIT = 0, @IsResiding BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID)
        THROW 51002, N'Employee not found.', 1;
    IF @FamilyID IS NULL
    BEGIN
        INSERT mst.EmployeeFamily (CompanyID, EmployeeID, MemberName, Relation, DateOfBirth, IsDependent, IsResiding)
        VALUES (@CompanyID, @EmployeeID, @MemberName, @Relation, @DateOfBirth, @IsDependent, @IsResiding);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS FamilyID;
    END
    ELSE
    BEGIN
        UPDATE mst.EmployeeFamily
        SET MemberName = @MemberName, Relation = @Relation, DateOfBirth = @DateOfBirth, IsDependent = @IsDependent, IsResiding = @IsResiding
        WHERE FamilyID = @FamilyID AND CompanyID = @CompanyID AND EmployeeID = @EmployeeID;
        IF @@ROWCOUNT = 0 THROW 51005, N'Family member not found.', 1;
        SELECT @FamilyID AS FamilyID;
    END
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeFamily_Delete
    @FamilyID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    /* A member who is still a nominee cannot be deleted: the foreign key refuses it. Remove the nominee first. */
    DELETE FROM mst.EmployeeFamily WHERE FamilyID = @FamilyID AND CompanyID = @CompanyID;
    IF @@ROWCOUNT = 0 THROW 51005, N'Family member not found.', 1;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeNominee_List
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT n.NomineeID, n.SchemeID, n.FamilyID, f.MemberName, f.Relation, n.SharePercent
    FROM mst.EmployeeNominee n
    JOIN mst.EmployeeFamily f ON f.FamilyID = n.FamilyID AND f.CompanyID = n.CompanyID
    WHERE n.EmployeeID = @EmployeeID AND n.CompanyID = @CompanyID
    ORDER BY n.SchemeID, n.NomineeID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeNominee_Save
    @EmployeeID INT, @SchemeID TINYINT, @FamilyID INT, @SharePercent DECIMAL(5,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID)
        THROW 51002, N'Employee not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.EmployeeFamily WHERE FamilyID = @FamilyID AND CompanyID = @CompanyID AND EmployeeID = @EmployeeID)
        THROW 51005, N'Family member not found for this employee.', 1;
    BEGIN TRAN;
    IF ISNULL((SELECT SUM(SharePercent) FROM mst.EmployeeNominee WITH (UPDLOCK, HOLDLOCK)
               WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND SchemeID = @SchemeID AND FamilyID <> @FamilyID), 0) + @SharePercent > 100
        THROW 51003, N'Nominee shares for this scheme cannot add up to more than 100 percent.', 1;
    UPDATE mst.EmployeeNominee SET SharePercent = @SharePercent
    WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND SchemeID = @SchemeID AND FamilyID = @FamilyID;
    IF @@ROWCOUNT = 0
        INSERT mst.EmployeeNominee (CompanyID, EmployeeID, SchemeID, FamilyID, SharePercent)
        VALUES (@CompanyID, @EmployeeID, @SchemeID, @FamilyID, @SharePercent);
    COMMIT;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeNominee_Delete
    @NomineeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DELETE FROM mst.EmployeeNominee WHERE NomineeID = @NomineeID AND CompanyID = @CompanyID;
    IF @@ROWCOUNT = 0 THROW 51005, N'Nominee not found.', 1;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeDocument_List
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT d.DocumentID, d.DocumentTypeID, t.DocumentName, d.DocNoLast4, d.ValidTo, d.FilePath, d.VerifiedByUserID, d.VerifiedOn
    FROM mst.EmployeeDocument d
    JOIN mst.DocumentType t ON t.DocumentTypeID = d.DocumentTypeID
    WHERE d.EmployeeID = @EmployeeID AND d.CompanyID = @CompanyID
    ORDER BY t.DocumentName;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeDocument_GetEncrypted
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT DocumentID, DocumentTypeID, IdKeyVersion, DocNoEnc
    FROM mst.EmployeeDocument
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID AND DocNoEnc IS NOT NULL;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeDocument_Save
    @EmployeeID INT, @DocumentTypeID INT, @DocNoEnc VARBINARY(256) = NULL, @DocNoLast4 CHAR(4) = NULL, @IdKeyVersion TINYINT = 1,
    @ValidTo DATE = NULL, @FilePath NVARCHAR(400) = NULL, @VerifiedByUserID INT = NULL, @VerifiedOn DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID)
        THROW 51002, N'Employee not found.', 1;
    UPDATE mst.EmployeeDocument
    SET DocNoEnc = @DocNoEnc, DocNoLast4 = @DocNoLast4, IdKeyVersion = @IdKeyVersion, ValidTo = @ValidTo,
        FilePath = COALESCE(@FilePath, FilePath), VerifiedByUserID = @VerifiedByUserID, VerifiedOn = @VerifiedOn
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID AND DocumentTypeID = @DocumentTypeID;
    IF @@ROWCOUNT = 0
        INSERT mst.EmployeeDocument (CompanyID, EmployeeID, DocumentTypeID, DocNoEnc, DocNoLast4, IdKeyVersion, ValidTo, FilePath, VerifiedByUserID, VerifiedOn)
        VALUES (@CompanyID, @EmployeeID, @DocumentTypeID, @DocNoEnc, @DocNoLast4, @IdKeyVersion, @ValidTo, @FilePath, @VerifiedByUserID, @VerifiedOn);
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeSeparation_List
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT SeparationID, ReasonID, NoticeDate, LastWorkingDay, NoticePeriodDays, NoticeServedDays, FnFStatusID, ExitInterviewDone
    FROM mst.EmployeeSeparation
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID
    ORDER BY SeparationID DESC;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeSeparation_Save
    @SeparationID INT = NULL, @EmployeeID INT, @ReasonID TINYINT, @NoticeDate DATE = NULL, @LastWorkingDay DATE,
    @NoticePeriodDays SMALLINT = 30, @NoticeServedDays SMALLINT = 0, @FnFStatusID TINYINT = 1, @ExitInterviewDone BIT = 0, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID)
        THROW 51002, N'Employee not found.', 1;
    IF @SeparationID IS NULL
    BEGIN
        INSERT mst.EmployeeSeparation (CompanyID, EmployeeID, ReasonID, NoticeDate, LastWorkingDay, NoticePeriodDays, NoticeServedDays,
                                       FnFStatusID, ExitInterviewDone, CreatedByUserID)
        VALUES (@CompanyID, @EmployeeID, @ReasonID, @NoticeDate, @LastWorkingDay, @NoticePeriodDays, @NoticeServedDays,
                @FnFStatusID, @ExitInterviewDone, @UserID);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS SeparationID;
    END
    ELSE
    BEGIN
        UPDATE mst.EmployeeSeparation
        SET ReasonID = @ReasonID, NoticeDate = @NoticeDate, LastWorkingDay = @LastWorkingDay, NoticePeriodDays = @NoticePeriodDays,
            NoticeServedDays = @NoticeServedDays, FnFStatusID = @FnFStatusID, ExitInterviewDone = @ExitInterviewDone
        WHERE SeparationID = @SeparationID AND CompanyID = @CompanyID AND EmployeeID = @EmployeeID;
        IF @@ROWCOUNT = 0 THROW 51005, N'Separation record not found.', 1;
        SELECT @SeparationID AS SeparationID;
    END
END
GO
