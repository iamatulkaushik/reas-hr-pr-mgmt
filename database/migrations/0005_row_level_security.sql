/* 0005_row_level_security.sql
   Tenant isolation. The app sets SESSION_CONTEXT CompanyID (and IsDeveloper) after opening a connection.
   Filter predicate only: rows of other companies are invisible. Writes are not blocked here because the
   app login has EXECUTE only and aud.usp_Audit_Insert stamps the tenant itself.
   Every future table with a CompanyID column must be added to a security policy in its own migration. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'sec.fn_TenantPredicate', N'IF') IS NULL
    EXEC (N'CREATE FUNCTION sec.fn_TenantPredicate (@CompanyID INT)
            RETURNS TABLE WITH SCHEMABINDING
            AS RETURN SELECT 1 AS Allowed
               WHERE TRY_CONVERT(BIT, SESSION_CONTEXT(N''IsDeveloper'')) = 1
                  OR (@CompanyID IS NOT NULL AND @CompanyID = TRY_CONVERT(INT, SESSION_CONTEXT(N''CompanyID'')))');
GO
IF NOT EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'TenantPolicy_AuditLog')
    EXEC (N'CREATE SECURITY POLICY aud.TenantPolicy_AuditLog
            ADD FILTER PREDICATE sec.fn_TenantPredicate(CompanyID) ON aud.AuditLog
            WITH (STATE = ON)');
GO
