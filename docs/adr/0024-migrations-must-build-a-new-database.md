# ADR 0024: The migrations must build an empty database

Status: accepted (Owner, 2026-11-09). Owner: Sigma.

**What went wrong.** The first run of the CI job `database` built a database from nothing and stopped with `Cannot find the object "mst.EmployeeStatutory"`. Script 0023 changes that table, but the table is created by 0105. The scripts had only ever been applied to databases built step by step, where the order did not show. Looking for the same mistake found three patch scripts:

| Script | Problem on a new database |
|---|---|
| 0023 (EPF and EPS on higher wage) | fails: the table does not exist yet; and 0108 would put the old procedures back |
| 0028 (absence divisor) | fails: `pay.CompanyPolicy` does not exist yet (0300); and 0302 would put the old procedures back |
| 0029 (separation lock) | does not fail, but 0108 puts the old `usp_EmployeeSeparation_Save` back |

**The fix.**
- 0023 and 0028 now skip their table change when the table is missing (a database that already ran them is unchanged).
- 0300 creates `AbsenceDeductionDivisor` itself, with the constraint names 0028 used (0309 and later scripts need the column).
- New 0109 (after 0108) adds the two EPF columns and the check constraint and defines `usp_EmployeeStatutory_Get`, `_SaveFlags` and `usp_EmployeeSeparation_Save` in their final form. New 0315 (after 0302) defines `usp_Policy_Get` and `_Save` in their final form. Both are idempotent and change nothing on a database that already has the final state.
- This edits three merged scripts (0023, 0028, 0300). It is an exception to "never edit after merge", allowed once because nothing is released and a new database could not be built at all. The rule stands.

**Same mistake, found by the next run.** The first fix reached further, and the job then stopped at `DENY EXECUTE ON OBJECT::att.usp_Punch_Ingest` in script 0030: the procedure is created by 0502. The script was Sigma's own, written the day before. It is now 0505 (0030 is removed). A permission statement counts like any other change: the object must exist in an earlier script.

**A zip cannot delete a file.** The superseded `0030_att_punch_ingest_deny.sql` stayed in the repository after 0505 was added, so the same error came back. Guard 10 in `scripts/check-guards.sh` now fails the build at once, with the file name, if a file that was removed on purpose is still there.

**Existing databases** (the migration runner refuses a script whose text changed after it was applied). Either create the database again (it holds test data only), or tell the runner the three new texts and then migrate:
```
UPDATE cfg.SchemaVersion SET Checksum = '60bdee5ce43f8b737b3103b3b089c7deedaed5ca4e74c294685b57681d6008d3' WHERE ScriptNumber = '0023';
UPDATE cfg.SchemaVersion SET Checksum = 'b0c6dd35f7e6259954303d28475eeaac9e0f782e2fb83162c24d143ea9b66b06' WHERE ScriptNumber = '0028';
UPDATE cfg.SchemaVersion SET Checksum = '243f3960ec99690667554a37bfd5498085472559b356dfbeb4c158eee5501540' WHERE ScriptNumber = '0300';
```
then `RevolutionSetup migrate` applies 0109 and 0315.

**Rules from now on.**
1. A patch to a table or procedure whose creating script has a higher number than the patch number is a mistake. Put the change in a script numbered after the last script that touches that object.
2. The last script that defines a procedure decides what a new database gets: a patched procedure must keep its patch in its final definition.
3. `MigrationOrderTests` (no database needed; runs in both CI jobs) checks rule 1 for tables, for `GRANT`, `DENY` and `REVOKE` on an object, and for `EXEC` of a procedure outside a procedure body, and rule 2 for the procedures patched so far. The `database` job builds an empty database from every script and runs the database tests.
