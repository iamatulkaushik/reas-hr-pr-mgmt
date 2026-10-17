/* 0024_reveal_log_batch.sql
   Change request 0016, ADR 0019. File exports (ECR, ESIC sheet, bank file) decrypt ids for many employees in one go.
   aud.RevealLog gets a Reason column. aud.usp_Reveal_InsertBatch writes one row per employee for one export, in one statement: all or none.
   The caller writes the batch BEFORE it decrypts anything and stops if this fails, so an export that cannot be logged releases no ids.
   The company always comes from SESSION_CONTEXT. The user is taken from SESSION_CONTEXT UserID when the app has set it, otherwise from @UserID.
   Every employee must belong to the session company, ids must be distinct integers, at most 5000 per call.
   Errors: 50044 no reason given, 50045 employee list empty or not a list of distinct integers, 50046 more than 5000, 50047 an employee is not in this company.
   Idempotent. Never edit after merge. */
SET XACT_ABORT ON;
GO
IF COL_LENGTH(N'aud.RevealLog', N'Reason') IS NULL
    ALTER TABLE aud.RevealLog ADD Reason NVARCHAR(50) NULL;
GO
CREATE OR ALTER PROCEDURE aud.usp_Reveal_InsertBatch
    @UserID INT, @Reason NVARCHAR(50), @Fields NVARCHAR(100), @EmployeeIdsJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    DECLARE @ByUserID INT = COALESCE(TRY_CONVERT(INT, SESSION_CONTEXT(N'UserID')), @UserID);
    IF @Reason IS NULL OR LEN(LTRIM(RTRIM(@Reason))) = 0
        THROW 50044, N'A reveal needs a reason.', 1;
    IF @EmployeeIdsJson IS NULL OR ISJSON(@EmployeeIdsJson) <> 1
        THROW 50045, N'The employee list is not valid.', 1;
    DECLARE @Ids TABLE (EmployeeID INT NOT NULL PRIMARY KEY);
    INSERT @Ids (EmployeeID)
    SELECT DISTINCT TRY_CONVERT(INT, [value]) FROM OPENJSON(@EmployeeIdsJson) WHERE TRY_CONVERT(INT, [value]) IS NOT NULL;
    DECLARE @Listed INT = (SELECT COUNT(*) FROM OPENJSON(@EmployeeIdsJson));
    DECLARE @Distinct INT = (SELECT COUNT(*) FROM @Ids);
    IF @Distinct = 0 OR @Distinct <> @Listed
        THROW 50045, N'The employee list must be a non-empty list of distinct whole numbers.', 1;
    IF @Distinct > 5000
        THROW 50046, N'At most 5000 employees can be revealed in one export.', 1;
    IF EXISTS (SELECT 1 FROM @Ids i WHERE NOT EXISTS (SELECT 1 FROM mst.Employee e WHERE e.EmployeeID = i.EmployeeID AND e.CompanyID = @CompanyID))
        THROW 50047, N'An employee in the list is not in this company.', 1;
    INSERT aud.RevealLog (CompanyID, UserID, EmployeeID, Fields, RowsRevealed, Reason)
    SELECT @CompanyID, @ByUserID, i.EmployeeID, @Fields, 1, LTRIM(RTRIM(@Reason)) FROM @Ids i;
END
GO
CREATE OR ALTER PROCEDURE aud.usp_Reveal_List @Top INT = 200
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT TOP (@Top) r.RevealID, r.UserID, u.UserName, r.EmployeeID, r.Fields, r.RowsRevealed, r.RevealedUtc, r.Reason
    FROM aud.RevealLog r
    JOIN sec.[User] u ON u.UserID = r.UserID
    WHERE r.CompanyID = @CompanyID
    ORDER BY r.RevealID DESC;
END
GO
