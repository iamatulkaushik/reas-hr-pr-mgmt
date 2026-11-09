/* 0505_att_punch_ingest_deny.sql
   Security fix, change request 0023; ADR 0021, ADR 0024. Idempotent. Never edit after merge: add a new number.
   att.usp_Punch_Ingest (0502) takes the company as a parameter and is meant to be called only by the agent wrapper of a biometric special build. The schema grant of 0006
   (GRANT EXECUTE ON SCHEMA::att TO hrms_app) covered it, so any signed-in user could have written punches into another company. This closes it in every database,
   whether or not the biometric special build is installed. The wrapper (database/special/biometric) reaches the procedure through ownership chaining, which skips this check.
   Number 0505: it must come after the script that creates the procedure (0502). It was first written as 0030, which fails on a new database; 0030 is removed. */
SET XACT_ABORT ON;
GO
DENY EXECUTE ON OBJECT::att.usp_Punch_Ingest TO hrms_app;
GO
