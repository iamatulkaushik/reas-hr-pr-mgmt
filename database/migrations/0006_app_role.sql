/* 0006_app_role.sql
   The application login is a member of hrms_app and has EXECUTE only (ADR 0004). Idempotent. */
SET XACT_ABORT ON;
GO
IF DATABASE_PRINCIPAL_ID(N'hrms_app') IS NULL CREATE ROLE hrms_app;
GO
GRANT EXECUTE ON SCHEMA::cfg TO hrms_app;
GRANT EXECUTE ON SCHEMA::sec TO hrms_app;
GRANT EXECUTE ON SCHEMA::mst TO hrms_app;
GRANT EXECUTE ON SCHEMA::att TO hrms_app;
GRANT EXECUTE ON SCHEMA::pay TO hrms_app;
GRANT EXECUTE ON SCHEMA::stat TO hrms_app;
GRANT EXECUTE ON SCHEMA::aud TO hrms_app;
GO
