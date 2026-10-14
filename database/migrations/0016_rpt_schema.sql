/* 0016_rpt_schema.sql
   ADR 0012: schema rpt holds read-only report procedures rpt.usp_<Report>_Get and no tables. Brave writes them in lane 0700 to 0799.
   The app login gets EXECUTE on the schema like the others. Idempotent. Never edit after merge: add a new number. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'rpt') EXEC (N'CREATE SCHEMA rpt');
GO
GRANT EXECUTE ON SCHEMA::rpt TO hrms_app;
GO
