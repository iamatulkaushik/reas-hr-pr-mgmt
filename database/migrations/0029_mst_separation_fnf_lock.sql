/* 0029_mst_separation_fnf_lock.sql
   Review of the F&F procs (Phase 7, item 4b). The pay lane writes mst.EmployeeSeparation.FnFStatusID (usp_Fnf_Save, usp_Fnf_ChangeStatus), so that
   column must have one writer. mst.usp_EmployeeSeparation_Save could also set it, which let the status drift from pay.FnfSettlement.
   Now: a new separation always starts at 1 (Open); an update never changes FnFStatusID (the parameter stays so callers do not break, and is ignored);
   once the settlement is Approved or Paid (3 or 4) the reason, last working day and notice days cannot be changed.
   Error numbers: 51008 the settlement is approved or paid, so the exit details are locked. */
SET XACT_ABORT ON;
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
