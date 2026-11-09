/* 0030_att_punch_ingest_deny.sql
   Superseded by 0505 (ADR 0024). Idempotent. Never edit after merge: add a new number.
   This script was first written to deny att.usp_Punch_Ingest to the app login. The procedure is created by 0502, so on a new database the DENY ran too early and failed.
   It is kept (not deleted) so that no file has to be removed from the repository: it now does the same DENY only when the procedure already exists, and 0505 does it
   for every database. Edited 2026-11-09 as a one-time exception, like 0023, 0028 and 0300 in ADR 0024. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'att.usp_Punch_Ingest', N'P') IS NOT NULL
    DENY EXECUTE ON OBJECT::att.usp_Punch_Ingest TO hrms_app;
GO
