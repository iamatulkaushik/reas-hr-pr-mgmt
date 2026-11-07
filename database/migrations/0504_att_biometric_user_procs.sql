/* 0504_att_biometric_user_procs.sql
   Biometric users, apart from devices (Phase 8, Owner 2026-11-06: devices change with the company and its needs, so people are managed as biometric users, not per device).
   A biometric user is an employee with a biometric user id (mst.Employee.BiometricID, one per company, the same id on every device the person is enrolled on) and, optionally,
   an RFID card. Any device of any brand that reports that id or card is matched to the employee. CREATE OR ALTER keeps it idempotent. The company comes from SESSION_CONTEXT.
   usp_BiometricUser_List: employees who work (or left in the last 30 days) with their id, active card, newest punch and punches in the last 30 days.
   usp_BiometricUser_Unknown: ids and cards seen on devices that match nobody, with counts, first and last time and the number of devices that saw them.
   usp_BiometricUser_NextId: the next unused id (largest in the company plus one) for enrolling a new person on a device.
   usp_BiometricUser_Assign: sets or clears the employee's biometric id. It writes only BiometricID, UpdatedUtc and UpdatedByUserID of mst.Employee, which the Employees module owns
   (change request 0024). Waiting punches are matched afterwards with usp_Punch_Resolve.
   Error numbers: 50002 no company in session, 53303 values not valid, 53304 employee not found, 53307 the id belongs to another employee. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE att.usp_BiometricUser_List
    @OnlyUnassigned BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @Today DATE = CAST(SYSDATETIME() AS DATE);
    SELECT e.EmployeeID, e.EmpCode, e.FullName, e.BiometricID, c.CardNo, p.LastPunchTime, ISNULL(p.Punches, 0) AS Punches30
    FROM mst.Employee e
    LEFT JOIN att.RfidCard c ON c.CompanyID = e.CompanyID AND c.EmployeeID = e.EmployeeID AND c.IsActive = 1
    OUTER APPLY (SELECT MAX(x.PunchTime) AS LastPunchTime, COUNT(*) AS Punches
                 FROM att.Punch x
                 WHERE x.CompanyID = e.CompanyID AND x.EmployeeID = e.EmployeeID AND x.PunchTime >= DATEADD(DAY, -30, CAST(@Today AS DATETIME2(0)))) p
    WHERE e.CompanyID = @CompanyID AND (e.ExitDate IS NULL OR e.ExitDate >= DATEADD(DAY, -30, @Today))
      AND (@OnlyUnassigned = 0 OR (e.BiometricID IS NULL AND c.CardID IS NULL))
    ORDER BY e.EmpCode;
END
GO
CREATE OR ALTER PROCEDURE att.usp_BiometricUser_Unknown
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT TOP (500) DeviceUserID, CardNo, COUNT(*) AS PunchCount, MIN(PunchTime) AS FirstSeen, MAX(PunchTime) AS LastSeen, COUNT(DISTINCT DeviceID) AS DeviceCount
    FROM att.Punch
    WHERE CompanyID = @CompanyID AND EmployeeID IS NULL
    GROUP BY DeviceUserID, CardNo
    ORDER BY MAX(PunchTime) DESC;
END
GO
CREATE OR ALTER PROCEDURE att.usp_BiometricUser_NextId
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT CAST(ISNULL(MAX(BiometricID), 0) + 1 AS INT) AS NextId FROM mst.Employee WHERE CompanyID = @CompanyID;
END
GO
CREATE OR ALTER PROCEDURE att.usp_BiometricUser_Assign
    @EmployeeID INT, @BiometricID INT = NULL, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @BiometricID IS NOT NULL AND @BiometricID <= 0 THROW 53303, N'The biometric id must be more than zero.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID) THROW 53304, N'Employee not found.', 1;
    BEGIN TRANSACTION;
        IF @BiometricID IS NOT NULL AND EXISTS (SELECT 1 FROM mst.Employee WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND BiometricID = @BiometricID AND EmployeeID <> @EmployeeID)
            THROW 53307, N'The biometric id belongs to another employee.', 1;
        UPDATE mst.Employee SET BiometricID = @BiometricID, UpdatedUtc = SYSUTCDATETIME(), UpdatedByUserID = @UserID
        WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID;
    COMMIT TRANSACTION;
END
GO
