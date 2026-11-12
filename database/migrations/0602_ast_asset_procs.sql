/* 0602_ast_asset_procs.sql
   Asset procedures (Phase 9 step 0600). CREATE OR ALTER keeps it idempotent. Every procedure reads the company from SESSION_CONTEXT and filters on it (ADR 0007).
   Errors: 50002 no company; 53500 the values are not valid; 53501 the asset type name is already used; 53502 asset type not found; 53503 the tag number is already used;
   53504 asset type not found or not active; 53505 asset not found; 53506 the asset is not in stock; 53507 employee not found; 53508 the asset is not out with an employee;
   53509 the status cannot be changed from the current one. StatusID: 1 In stock, 2 Issued, 3 Lost, 4 Damaged, 5 Written off. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE ast.usp_AssetType_Save
    @AssetTypeID INT = NULL, @TypeName NVARCHAR(60), @UsefulLifeMonths INT, @IsActive BIT = 1, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF LEN(LTRIM(RTRIM(ISNULL(@TypeName, N'')))) = 0 OR @UsefulLifeMonths IS NULL OR @UsefulLifeMonths NOT BETWEEN 1 AND 600 THROW 53500, N'The values are not valid.', 1;
    BEGIN TRANSACTION;
        IF EXISTS (SELECT 1 FROM ast.AssetType WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND TypeName = LTRIM(RTRIM(@TypeName)) AND (@AssetTypeID IS NULL OR AssetTypeID <> @AssetTypeID))
            THROW 53501, N'The asset type name is already used.', 1;
        IF @AssetTypeID IS NULL
        BEGIN
            INSERT ast.AssetType (CompanyID, TypeName, UsefulLifeMonths, IsActive, CreatedByUserID)
            VALUES (@CompanyID, LTRIM(RTRIM(@TypeName)), @UsefulLifeMonths, @IsActive, @UserID);
            SET @AssetTypeID = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE ast.AssetType SET TypeName = LTRIM(RTRIM(@TypeName)), UsefulLifeMonths = @UsefulLifeMonths, IsActive = @IsActive
            WHERE CompanyID = @CompanyID AND AssetTypeID = @AssetTypeID;
            IF @@ROWCOUNT = 0 THROW 53502, N'Asset type not found.', 1;
        END
    COMMIT TRANSACTION;
    SELECT @AssetTypeID AS AssetTypeID;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_AssetType_List
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT t.AssetTypeID, t.TypeName, t.UsefulLifeMonths, t.IsActive
    FROM ast.AssetType t
    WHERE t.CompanyID = @CompanyID AND (@IncludeInactive = 1 OR t.IsActive = 1)
    ORDER BY t.TypeName;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_Asset_Add
    @AssetTypeID INT, @TagNo NVARCHAR(30), @Description NVARCHAR(200), @SerialNo NVARCHAR(60) = NULL, @PurchaseDate DATE, @PurchaseValue DECIMAL(12,2), @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF LEN(LTRIM(RTRIM(ISNULL(@TagNo, N'')))) = 0 OR LEN(LTRIM(RTRIM(ISNULL(@Description, N'')))) = 0 OR @PurchaseDate IS NULL OR @PurchaseValue IS NULL OR @PurchaseValue <= 0
        THROW 53500, N'The values are not valid.', 1;
    IF NOT EXISTS (SELECT 1 FROM ast.AssetType WHERE CompanyID = @CompanyID AND AssetTypeID = @AssetTypeID AND IsActive = 1) THROW 53504, N'Asset type not found or not active.', 1;
    DECLARE @AssetID INT;
    BEGIN TRANSACTION;
        IF EXISTS (SELECT 1 FROM ast.Asset WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND TagNo = LTRIM(RTRIM(@TagNo))) THROW 53503, N'The tag number is already used.', 1;
        INSERT ast.Asset (CompanyID, AssetTypeID, TagNo, Description, SerialNo, PurchaseDate, PurchaseValue, StatusID, CreatedByUserID)
        VALUES (@CompanyID, @AssetTypeID, LTRIM(RTRIM(@TagNo)), LTRIM(RTRIM(@Description)), NULLIF(LTRIM(RTRIM(ISNULL(@SerialNo, N''))), N''), @PurchaseDate, @PurchaseValue, 1, @UserID);
        SET @AssetID = CAST(SCOPE_IDENTITY() AS INT);
    COMMIT TRANSACTION;
    SELECT @AssetID AS AssetID;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_Asset_List
    @StatusID TINYINT = NULL, @EmployeeID INT = NULL, @HeldOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT a.AssetID, a.AssetTypeID, t.TypeName, a.TagNo, a.Description, a.SerialNo, a.PurchaseDate, a.PurchaseValue, a.StatusID,
           i.EmployeeID AS HolderEmployeeID, e.EmpCode AS HolderCode, e.FullName AS HolderName, i.IssueDate, i.ExpectedReturnDate
    FROM ast.Asset a
    JOIN ast.AssetType t ON t.CompanyID = a.CompanyID AND t.AssetTypeID = a.AssetTypeID
    LEFT JOIN ast.AssetIssue i ON i.CompanyID = a.CompanyID AND i.AssetID = a.AssetID AND i.ReturnDate IS NULL
    LEFT JOIN mst.Employee e ON e.CompanyID = i.CompanyID AND e.EmployeeID = i.EmployeeID
    WHERE a.CompanyID = @CompanyID
      AND (@StatusID IS NULL OR a.StatusID = @StatusID)
      AND (@EmployeeID IS NULL OR i.EmployeeID = @EmployeeID)
      AND (@HeldOnly = 0 OR i.IssueID IS NOT NULL)
    ORDER BY a.TagNo;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_Asset_Issue
    @AssetID INT, @EmployeeID INT, @IssueDate DATE, @IssueCondition NVARCHAR(100) = NULL, @ExpectedReturnDate DATE = NULL, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @IssueDate IS NULL THROW 53500, N'The values are not valid.', 1;
    IF NOT EXISTS (SELECT 1 FROM mst.Employee WHERE CompanyID = @CompanyID AND EmployeeID = @EmployeeID) THROW 53507, N'Employee not found.', 1;
    DECLARE @Status TINYINT, @PurchaseDate DATE;
    BEGIN TRANSACTION;
        SELECT @Status = StatusID, @PurchaseDate = PurchaseDate FROM ast.Asset WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND AssetID = @AssetID;
        IF @Status IS NULL THROW 53505, N'Asset not found.', 1;
        IF @Status <> 1 THROW 53506, N'The asset is not in stock.', 1;
        IF @IssueDate < @PurchaseDate OR (@ExpectedReturnDate IS NOT NULL AND @ExpectedReturnDate < @IssueDate) THROW 53500, N'The values are not valid.', 1;
        INSERT ast.AssetIssue (CompanyID, AssetID, EmployeeID, IssueDate, IssueCondition, ExpectedReturnDate, IssuedByUserID)
        VALUES (@CompanyID, @AssetID, @EmployeeID, @IssueDate, NULLIF(LTRIM(RTRIM(ISNULL(@IssueCondition, N''))), N''), @ExpectedReturnDate, @UserID);
        UPDATE ast.Asset SET StatusID = 2 WHERE CompanyID = @CompanyID AND AssetID = @AssetID;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_Asset_Return
    @AssetID INT, @ReturnDate DATE, @ReturnCondition NVARCHAR(100) = NULL, @ReturnStatusID TINYINT = 1, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @ReturnDate IS NULL OR @ReturnStatusID NOT IN (1, 4) THROW 53500, N'The values are not valid.', 1;
    DECLARE @Status TINYINT, @IssueID INT, @IssueDate DATE;
    BEGIN TRANSACTION;
        SELECT @Status = StatusID FROM ast.Asset WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND AssetID = @AssetID;
        IF @Status IS NULL THROW 53505, N'Asset not found.', 1;
        SELECT @IssueID = IssueID, @IssueDate = IssueDate FROM ast.AssetIssue WHERE CompanyID = @CompanyID AND AssetID = @AssetID AND ReturnDate IS NULL;
        IF @IssueID IS NULL OR @Status = 5 THROW 53508, N'The asset is not out with an employee.', 1;
        IF @ReturnDate < @IssueDate THROW 53500, N'The values are not valid.', 1;
        UPDATE ast.AssetIssue SET ReturnDate = @ReturnDate, ReturnCondition = NULLIF(LTRIM(RTRIM(ISNULL(@ReturnCondition, N''))), N''), ReturnedByUserID = @UserID
        WHERE CompanyID = @CompanyID AND IssueID = @IssueID;
        UPDATE ast.Asset SET StatusID = @ReturnStatusID WHERE CompanyID = @CompanyID AND AssetID = @AssetID;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_Asset_SetStatus
    @AssetID INT, @StatusID TINYINT, @UserID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    IF @StatusID NOT IN (3, 4, 5) THROW 53500, N'The values are not valid.', 1;
    DECLARE @Current TINYINT;
    BEGIN TRANSACTION;
        SELECT @Current = StatusID FROM ast.Asset WITH (UPDLOCK, HOLDLOCK) WHERE CompanyID = @CompanyID AND AssetID = @AssetID;
        IF @Current IS NULL THROW 53505, N'Asset not found.', 1;
        IF @Current = 5 OR @Current = @StatusID OR (@Current = 3 AND @StatusID = 4) THROW 53509, N'The status cannot be changed from the current one.', 1;
        UPDATE ast.Asset SET StatusID = @StatusID WHERE CompanyID = @CompanyID AND AssetID = @AssetID;
    COMMIT TRANSACTION;
END
GO
CREATE OR ALTER PROCEDURE ast.usp_Asset_History
    @AssetID INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CompanyID INT = TRY_CONVERT(INT, SESSION_CONTEXT(N'CompanyID'));
    IF @CompanyID IS NULL THROW 50002, N'No company in session.', 1;
    SELECT i.IssueID, i.EmployeeID, e.EmpCode, e.FullName, i.IssueDate, i.IssueCondition, i.ExpectedReturnDate, i.ReturnDate, i.ReturnCondition
    FROM ast.AssetIssue i
    JOIN mst.Employee e ON e.CompanyID = i.CompanyID AND e.EmployeeID = i.EmployeeID
    WHERE i.CompanyID = @CompanyID AND i.AssetID = @AssetID
    ORDER BY i.IssueDate DESC, i.IssueID DESC;
END
GO
