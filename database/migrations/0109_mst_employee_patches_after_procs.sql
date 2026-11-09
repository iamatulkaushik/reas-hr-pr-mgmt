/* 0109_mst_employee_patches_after_procs.sql
   ADR 0024. Idempotent. Never edit after merge: add a new number.
   On a new database the patch scripts 0023 (EPF and EPS on higher wage) and 0029 (separation lock) run before the table (0105) and procedures (0108) they change exist,
   and 0108 would then put the old procedures back. This script runs after 0108 and sets the final state: the two columns and the check constraint on mst.EmployeeStatutory,
   usp_EmployeeStatutory_Get and _SaveFlags as in 0023, usp_EmployeeSeparation_Save as in 0029. On a database that already ran 0023 and 0029 it changes nothing but
   redefines the same procedures. Errors: as 0023 (50042, 50043) and 0029 (51008). */
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
                1, @ExitInterviewDone, @UserID);
        SELECT CAST(SCOPE_IDENTITY() AS INT) AS SeparationID;
    END
    ELSE
    BEGIN
        IF EXISTS (SELECT 1 FROM mst.EmployeeSeparation WITH (UPDLOCK)
                   WHERE SeparationID = @SeparationID AND CompanyID = @CompanyID AND EmployeeID = @EmployeeID AND FnFStatusID >= 3
                     AND (ReasonID <> @ReasonID OR LastWorkingDay <> @LastWorkingDay
                          OR NoticePeriodDays <> @NoticePeriodDays OR NoticeServedDays <> @NoticeServedDays))
            THROW 51008, N'The settlement is approved or paid, so the exit details cannot be changed.', 1;
        UPDATE mst.EmployeeSeparation
        SET ReasonID = @ReasonID, NoticeDate = @NoticeDate, LastWorkingDay = @LastWorkingDay, NoticePeriodDays = @NoticePeriodDays,
            NoticeServedDays = @NoticeServedDays, ExitInterviewDone = @ExitInterviewDone
        WHERE SeparationID = @SeparationID AND CompanyID = @CompanyID AND EmployeeID = @EmployeeID;
        IF @@ROWCOUNT = 0 THROW 51005, N'Separation record not found.', 1;
        SELECT @SeparationID AS SeparationID;
    END
END
GO
