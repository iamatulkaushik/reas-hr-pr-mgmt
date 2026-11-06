/* 0503_att_punch_times_proc.sql
   Matched punch times for building attendance (Phase 8 step 0503). CREATE OR ALTER keeps it idempotent. The company comes from SESSION_CONTEXT.
   One row per matched punch (EmployeeID known) with a time from @FromTime up to but not including @ToTime, in employee and time order. No cap on rows: the service asks for at most
   nine days at a time. Unmatched punches are not returned: nobody can be marked from them. Error numbers: 50002 no company in session. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE att.usp_Punch_Times
    @FromTime DATETIME2(0), @ToTime DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT EmployeeID, PunchTime
    FROM att.Punch
    WHERE CompanyID = @CompanyID AND EmployeeID IS NOT NULL AND PunchTime >= @FromTime AND PunchTime < @ToTime
    ORDER BY EmployeeID, PunchTime;
END
GO
