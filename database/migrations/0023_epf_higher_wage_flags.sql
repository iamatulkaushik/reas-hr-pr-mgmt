/* 0023_epf_higher_wage_flags.sql
   Change request 0015. Two separate per-employee choices: EPF on higher wage and EPS (pension) on higher wage. EPS on higher wage needs
   EPF on higher wage (check constraint); EPF on higher wage needs EPF membership (checked when saving). Idempotent. Never edit after merge.
   HigherWageOption stays for now and is kept equal to EpfOnHigherWage. The first time this runs, EpfOnHigherWage starts from HigherWageOption
   and EpsOnHigherWage starts off. The backfill sets IsDeveloper for this connection only (tenant filter), then clears it.
   usp_EmployeeStatutory_SaveFlags gets two optional parameters. A caller that does not pass them still works: EPF on higher wage then follows
   @HigherWageOption and EPS on higher wage is saved as off (the old screen cannot express it).
   Errors: 50042 EPS needs EPF on higher wage, 50043 EPF on higher wage needs EPF membership. */
SET XACT_ABORT ON;
GO
IF COL_LENGTH(N'mst.EmployeeStatutory', N'EpfOnHigherWage') IS NULL
BEGIN
    ALTER TABLE mst.EmployeeStatutory ADD
        EpfOnHigherWage BIT NOT NULL CONSTRAINT DF_EmpStat_EpfOnHigher DEFAULT 0,
        EpsOnHigherWage BIT NOT NULL CONSTRAINT DF_EmpStat_EpsOnHigher DEFAULT 0;
    EXEC sp_set_session_context @key = N'IsDeveloper', @value = 1;
    EXEC (N'UPDATE mst.EmployeeStatutory SET EpfOnHigherWage = HigherWageOption');
    EXEC sp_set_session_context @key = N'IsDeveloper', @value = NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_EmpStat_EpsNeedsEpf' AND parent_object_id = OBJECT_ID(N'mst.EmployeeStatutory'))
    ALTER TABLE mst.EmployeeStatutory ADD CONSTRAINT CK_EmpStat_EpsNeedsEpf CHECK (EpsOnHigherWage = 0 OR EpfOnHigherWage = 1);
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
           EpfOnHigherWage, EpsOnHigherWage
    FROM mst.EmployeeStatutory
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE mst.usp_EmployeeStatutory_SaveFlags
    @EmployeeID INT, @IsEpfMember BIT, @PfNumber NVARCHAR(30) = NULL, @EpfJoinDate DATE = NULL, @IsPensionMember BIT = 1,
    @HigherWageOption BIT = 0, @IsEsiMember BIT, @EsiDispensary NVARCHAR(100) = NULL, @PtApplicable BIT = 1, @LwfApplicable BIT = 1,
    @TaxRegimeID TINYINT = 2, @IsInternationalWorker BIT = 0, @UserID INT = NULL,
    @EpfOnHigherWage BIT = NULL, @EpsOnHigherWage BIT = NULL
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
    UPDATE mst.EmployeeStatutory
    SET IsEpfMember = @IsEpfMember, PfNumber = @PfNumber, EpfJoinDate = @EpfJoinDate, IsPensionMember = @IsPensionMember,
        HigherWageOption = @Epf, EpfOnHigherWage = @Epf, EpsOnHigherWage = @Eps,
        IsEsiMember = @IsEsiMember, EsiDispensary = @EsiDispensary, PtApplicable = @PtApplicable,
        LwfApplicable = @LwfApplicable, TaxRegimeID = @TaxRegimeID, IsInternationalWorker = @IsInternationalWorker,
        UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
    WHERE EmployeeID = @EmployeeID AND CompanyID = @CompanyID;
    IF @@ROWCOUNT = 0
        INSERT mst.EmployeeStatutory (CompanyID, EmployeeID, IsEpfMember, PfNumber, EpfJoinDate, IsPensionMember, HigherWageOption, EpfOnHigherWage,
                                      EpsOnHigherWage, IsEsiMember, EsiDispensary, PtApplicable, LwfApplicable, TaxRegimeID, IsInternationalWorker, UpdatedByUserID)
        VALUES (@CompanyID, @EmployeeID, @IsEpfMember, @PfNumber, @EpfJoinDate, @IsPensionMember, @Epf, @Epf,
                @Eps, @IsEsiMember, @EsiDispensary, @PtApplicable, @LwfApplicable, @TaxRegimeID, @IsInternationalWorker, @UserID);
END
GO
