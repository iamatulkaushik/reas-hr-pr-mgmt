/* 0007_provisioning_procs.sql
   Create-company and create-user procedures for the setup tool. The app login is denied them (ADR 0004: app has EXECUTE only,
   and must not be able to mint users). Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE mst.usp_Company_Create @CompanyName NVARCHAR(200), @PlanTierID TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT mst.Company (CompanyName, PlanTierID) VALUES (@CompanyName, @PlanTierID);
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO
CREATE OR ALTER PROCEDURE sec.usp_User_Create
    @UserName NVARCHAR(100), @CompanyID INT, @RoleID TINYINT,
    @PasswordHash VARBINARY(32), @PasswordSalt VARBINARY(16), @Iterations INT, @MustChangePassword BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT sec.[User] (UserName, CompanyID, RoleID, PasswordHash, PasswordSalt, Iterations, MustChangePassword)
    VALUES (@UserName, @CompanyID, @RoleID, @PasswordHash, @PasswordSalt, @Iterations, @MustChangePassword);
    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO
DENY EXECUTE ON OBJECT::mst.usp_Company_Create TO hrms_app;
DENY EXECUTE ON OBJECT::sec.usp_User_Create TO hrms_app;
GO
