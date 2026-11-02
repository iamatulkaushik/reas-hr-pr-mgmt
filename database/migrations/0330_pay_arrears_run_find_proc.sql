/* 0330_pay_arrears_run_find_proc.sql
   Finds the regular run (RunNo 1) for a pay period, for the arrears claim. CREATE OR ALTER keeps it idempotent.
   The company comes from SESSION_CONTEXT. Returns no row when no run was made for the period. Error numbers: 50002 no company in session. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsRun_Find
    @PeriodFrom DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT RunID, PeriodFrom, PeriodTo, RunNo, StatusID, EmployeeCount, TotalGross, TotalDeductions, TotalNet
    FROM pay.PayrollRun
    WHERE CompanyID = @CompanyID AND PeriodFrom = @PeriodFrom AND RunNo = 1;
END
GO
