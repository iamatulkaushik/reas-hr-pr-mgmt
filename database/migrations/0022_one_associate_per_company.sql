/* 0022_one_associate_per_company.sql
   ADR 0018 amended. A company is worked on by exactly one Associate, so the Associate's licence governs the company and its Owner and
   Operators without any "which licence" choice. sec.UserCompany only ever links Associates (an Owner or Operator has sec.[User].CompanyID),
   so a unique index on CompanyID is the hard rule. The index also makes cfg.usp_Licence_ForSession return at most one row.
   If a company is already linked to more than one Associate this script stops with error 50041: keep one link per company, then run it again.
   Creating an Associate with a company that already has an Associate now fails on the index (SQL error 2601). Idempotent. Never edit after merge. */
SET XACT_ABORT ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_UserCompany_OneAssociatePerCompany' AND object_id = OBJECT_ID(N'sec.UserCompany'))
BEGIN
    IF EXISTS (SELECT 1 FROM sec.UserCompany GROUP BY CompanyID HAVING COUNT(*) > 1)
        THROW 50041, N'A company is linked to more than one Associate. Keep one link per company, then run this script again.', 1;
    CREATE UNIQUE INDEX UX_UserCompany_OneAssociatePerCompany ON sec.UserCompany (CompanyID);
END
GO
