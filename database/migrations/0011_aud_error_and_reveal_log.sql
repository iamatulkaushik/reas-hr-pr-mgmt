/* 0011_aud_error_and_reveal_log.sql
   aud.ErrorLog: unexpected errors caught by services. aud.RevealLog: every time a protected id (PAN, Aadhaar, UAN, ESI IP,
   bank account, document number) is decrypted for display. Both are insert-only: no procedure updates or deletes them.
   Company comes from SESSION_CONTEXT, never a parameter. Tenant policy is in 0012. Idempotent. Never edit after merge.
   Error 50002 = no company in session (same number as the attendance procedures). */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'aud.ErrorLog', N'U') IS NULL
BEGIN
    CREATE TABLE aud.ErrorLog
    (
        ErrorID      BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ErrorLog PRIMARY KEY,
        CompanyID    INT            NULL,
        UserID       INT            NULL,
        ProcName     NVARCHAR(200)  NULL,
        ErrorNumber  INT            NULL,
        ErrorMessage NVARCHAR(2000) NOT NULL,
        ErrorLine    INT            NULL,
        CreatedUtc   DATETIME2(3)   NOT NULL CONSTRAINT DF_ErrorLog_CreatedUtc DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_ErrorLog_Company_Created ON aud.ErrorLog (CompanyID, CreatedUtc DESC);
END
GO
IF OBJECT_ID(N'aud.RevealLog', N'U') IS NULL
BEGIN
    CREATE TABLE aud.RevealLog
    (
        RevealID     BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RevealLog PRIMARY KEY,
        CompanyID    INT           NOT NULL CONSTRAINT FK_RevealLog_Company REFERENCES mst.Company (CompanyID),
        UserID       INT           NOT NULL CONSTRAINT FK_RevealLog_User REFERENCES sec.[User] (UserID),
        EmployeeID   INT           NULL,
        Fields       NVARCHAR(100) NOT NULL,
        RowsRevealed INT           NOT NULL CONSTRAINT CK_RevealLog_Rows CHECK (RowsRevealed >= 1),
        RevealedUtc  DATETIME2(3)  NOT NULL CONSTRAINT DF_RevealLog_RevealedUtc DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_RevealLog_Company_Revealed ON aud.RevealLog (CompanyID, RevealedUtc DESC);
END
GO
CREATE OR ALTER PROCEDURE aud.usp_Error_Insert
    @UserID INT = NULL, @ProcName NVARCHAR(200) = NULL, @ErrorNumber INT = NULL, @ErrorMessage NVARCHAR(2000), @ErrorLine INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT aud.ErrorLog (CompanyID, UserID, ProcName, ErrorNumber, ErrorMessage, ErrorLine)
    VALUES (TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID')), @UserID, @ProcName, @ErrorNumber, LEFT(@ErrorMessage, 2000), @ErrorLine);
END
GO
CREATE OR ALTER PROCEDURE aud.usp_Error_List @Top INT = 200
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Top) ErrorID, CompanyID, UserID, ProcName, ErrorNumber, ErrorMessage, ErrorLine, CreatedUtc
    FROM aud.ErrorLog
    ORDER BY ErrorID DESC;
END
GO
CREATE OR ALTER PROCEDURE aud.usp_Reveal_Insert @UserID INT, @EmployeeID INT = NULL, @Fields NVARCHAR(100), @RowsRevealed INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    INSERT aud.RevealLog (CompanyID, UserID, EmployeeID, Fields, RowsRevealed)
    VALUES (@CompanyID, @UserID, @EmployeeID, @Fields, @RowsRevealed);
END
GO
CREATE OR ALTER PROCEDURE aud.usp_Reveal_List @Top INT = 200
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT TOP (@Top) r.RevealID, r.UserID, u.UserName, r.EmployeeID, r.Fields, r.RowsRevealed, r.RevealedUtc
    FROM aud.RevealLog r
    JOIN sec.[User] u ON u.UserID = r.UserID
    WHERE r.CompanyID = @CompanyID
    ORDER BY r.RevealID DESC;
END
GO
