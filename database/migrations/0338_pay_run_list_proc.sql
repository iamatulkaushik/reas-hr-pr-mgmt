/* 0338_pay_run_list_proc.sql
   Lists every payroll run of a pay period for the Lock Payroll screen (step 7f). Brave lane 0300-0399 (taken over by Sigma, 2026-11-04). CREATE OR ALTER keeps it idempotent.
   The company always comes from SESSION_CONTEXT. One row per run, the regular run (RunNo 1) first, then the off-cycle runs (arrears) in run number order.
   Error numbers: 50002 no company in session. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_Run_ListForPeriod
    @PeriodFrom DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT RunID, PeriodFrom, PeriodTo, RunNo, StatusID, EmployeeCount, TotalGross, TotalDeductions, TotalNet
    FROM pay.PayrollRun
    WHERE CompanyID = @CompanyID AND PeriodFrom = @PeriodFrom
    ORDER BY RunNo;
END
GO
