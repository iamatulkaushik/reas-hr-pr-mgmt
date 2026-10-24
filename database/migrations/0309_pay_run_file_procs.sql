/* 0309_pay_run_file_procs.sql
   Payroll file export read model. Brave lane 0300-0399. Idempotent. Never edit after merge.
   pay.usp_RunFile_Employees returns the run lines of one run with the plain employee facts the ECR, ESIC and bank files need:
   code, name, EPF and ESI membership, EPS on higher wage, payment mode, IFSC, whether an account is on file, and the exit date.
   No encrypted column is read here. UAN, ESI IP number and account number are decrypted later in the app by
   EmployeeExportService, after the batch log row is written (ADR 0019). Tenant filter comes from SESSION_CONTEXT. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_RunFile_Employees @RunID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT l.EmployeeID, e.EmpCode, e.FullName, l.PaidDays, l.GrossEarnings, l.EpfWage, l.EsiWage, l.EpfEmployee, l.EpfEmployerEpf,
           l.EpfEmployerEps, l.Edli, l.EsiEmployee, l.EsiEmployer, l.NetPay,
           CAST(ISNULL(s.IsEpfMember, 0) AS BIT)      AS IsEpfMember,
           CAST(ISNULL(s.EpsOnHigherWage, 0) AS BIT)  AS EpsOnHigherWage,
           CAST(ISNULL(s.IsEsiMember, 0) AS BIT)      AS IsEsiMember,
           CAST(CASE WHEN b.PaymentModeID = 1 THEN 1 ELSE 0 END AS BIT) AS PaysByBank,
           b.Ifsc,
           CAST(CASE WHEN b.AccountLast4 IS NULL THEN 0 ELSE 1 END AS BIT) AS HasAccount,
           e.ExitDate
    FROM pay.PayrollRunLine l
    JOIN mst.Employee e ON e.CompanyID = l.CompanyID AND e.EmployeeID = l.EmployeeID
    LEFT JOIN mst.EmployeeStatutory s ON s.CompanyID = l.CompanyID AND s.EmployeeID = l.EmployeeID
    LEFT JOIN mst.EmployeeBank b ON b.CompanyID = l.CompanyID AND b.EmployeeID = l.EmployeeID
    WHERE l.CompanyID = @CompanyID AND l.RunID = @RunID
    ORDER BY l.EmployeeID;
END
GO
