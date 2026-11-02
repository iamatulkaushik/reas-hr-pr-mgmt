/* 0331_pay_arrears_ecr_proc.sql
   Arrear ECR source: the open claims' amounts for one wage month, one row per employee. CREATE OR ALTER keeps it idempotent.
   The company comes from SESSION_CONTEXT. Cancelled claims are not included. A month can be under only one open claim for an employee (error 53101 in
   usp_ArrearsClaim_Add), so the sums add nothing twice. No UAN here: the Employees export releases it. Error numbers: 50002 no company in session. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE pay.usp_ArrearsEcr_List
    @WageMonth DATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT c.EmployeeID, e.EmpCode, e.FullName,
           SUM(m.EpfWages) AS EpfWages, SUM(m.EpsWages) AS EpsWages, SUM(m.EdliWages) AS EdliWages,
           SUM(m.EmployeeEpf) AS EmployeeEpf, SUM(m.EmployerEps) AS EmployerEps, SUM(m.EmployerEpf) AS EmployerEpf
    FROM pay.ArrearsClaimMonth m
    JOIN pay.ArrearsClaim c ON c.CompanyID = m.CompanyID AND c.ClaimID = m.ClaimID
    JOIN mst.Employee e ON e.CompanyID = c.CompanyID AND e.EmployeeID = c.EmployeeID
    WHERE m.CompanyID = @CompanyID AND c.StatusID = 1 AND m.WageMonth = @WageMonth
    GROUP BY c.EmployeeID, e.EmpCode, e.FullName
    ORDER BY e.EmpCode;
END
GO
