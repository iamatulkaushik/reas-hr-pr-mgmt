/* 0110_mst_employee_esi_higher_ceiling.sql
   Change request 0030. Employees lane 0100-0199. Idempotent. Never edit after merge.
   mst.EmployeeStatutory.EsiHigherCeiling: the ESI wage ceiling for a person with a disability applies to this employee. A flag only (default 0): no certificate or
   disability detail is stored, because that is sensitive personal data and the ceiling needs nothing more. Allowed only for an ESI member.
   usp_EmployeeStatutory_Get returns it and usp_EmployeeStatutory_SaveFlags takes it (@EsiHigherCeiling, default 0, so older callers keep working); both are the 0109 versions
   with that one column added. Error 50044: the higher ceiling needs ESI membership. */
SET XACT_ABORT ON;
GO
IF COL_LENGTH(N'mst.EmployeeStatutory', N'EsiHigherCeiling') IS NULL
    ALTER TABLE mst.EmployeeStatutory ADD EsiHigherCeiling BIT NOT NULL CONSTRAINT DF_EmpStat_EsiHigherCeiling DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_EmpStat_EsiHigherNeedsEsi' AND parent_object_id = OBJECT_ID(N'mst.EmployeeStatutory'))
    EXEC (N'ALTER TABLE mst.EmployeeStatutory ADD CONSTRAINT CK_EmpStat_EsiHigherNeedsEsi CHECK (EsiHigherCeiling = 0 OR IsEsiMember = 1)');
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeStatutory_Get
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT EmployeeID, IsEpfMember, PfNumber, EpfJoinDate, IsPensionMember, HigherWageOption, IsEsiMember, EsiDispensary, PtApplicable,
           LwfApplicable, TaxRegimeID, IsInternationalWorker, UanLast4, EsiIpLast4, PanLast4, AadhaarLast4, IdKeyVersion,
           EpfOnHigherWage, EpsOnHigherWage, EsiHigherCeiling
    FROM mst.EmployeeStatutory
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeStatutory_SaveFlags
    @EmployeeID INT, @IsEpfMember BIT, @PfNumber NVARCHAR(30) = NULL, @EpfJoinDate DATE = NULL, @IsPensionMember BIT = 1,
    @HigherWageOption BIT = 0, @IsEsiMember BIT, @EsiDispensary NVARCHAR(100) = NULL, @PtApplicable BIT = 1, @LwfApplicable BIT = 1,
    @TaxRegimeID TINYINT = 2, @IsInternationalWorker BIT = 0, @UserID INT = NULL,
    @EpfOnHigherWage BIT = NULL, @EpsOnHigherWage BIT = NULL, @EsiHigherCeiling BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID)
        THROW 51002, N'Employee not found.', 1;
    DECLARE @Epf BIT = COALESCE(@EpfOnHigherWage, @HigherWageOption);
    DECLARE @Eps BIT = COALESCE(@EpsOnHigherWage, 0);
    IF @Eps = 1 AND @Epf = 0 THROW 50042, N'EPS on higher wage needs EPF on higher wage.', 1;
    IF @Epf = 1 AND @IsEpfMember = 0 THROW 50043, N'EPF on higher wage needs EPF membership.', 1;
    IF @EsiHigherCeiling = 1 AND @IsEsiMember = 0 THROW 50044, N'The higher ESI wage ceiling needs ESI membership.', 1;
    UPDATE mst.EmployeeStatutory
    SET IsEpfMember = @IsEpfMember, PfNumber = @PfNumber, EpfJoinDate = @EpfJoinDate, IsPensionMember = @IsPensionMember,
        HigherWageOption = @Epf, EpfOnHigherWage = @Epf, EpsOnHigherWage = @Eps,
        IsEsiMember = @IsEsiMember, EsiDispensary = @EsiDispensary, EsiHigherCeiling = @EsiHigherCeiling, PtApplicable = @PtApplicable,
        LwfApplicable = @LwfApplicable, TaxRegimeID = @TaxRegimeID, IsInternationalWorker = @IsInternationalWorker,
        UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
    IF @@ROWCOUNT = 0
        INSERT mst.EmployeeStatutory (CompanyID, EmployeeID, IsEpfMember, PfNumber, EpfJoinDate, IsPensionMember, HigherWageOption, EpfOnHigherWage,
                                      EpsOnHigherWage, IsEsiMember, EsiDispensary, EsiHigherCeiling, PtApplicable, LwfApplicable, TaxRegimeID, IsInternationalWorker, UpdatedByUserID)
        VALUES (@CompanyID, @EmployeeID, @IsEpfMember, @PfNumber, @EpfJoinDate, @IsPensionMember, @Epf, @Epf,
                @Eps, @IsEsiMember, @EsiDispensary, @EsiHigherCeiling, @PtApplicable, @LwfApplicable, @TaxRegimeID, @IsInternationalWorker, @UserID);
END
GO
