# Sigma review: Phase 3 and 4 branches, 2026-10-11

Reviewed on origin/main `ef50cd1`. Reviewer: Sigma. Author: Brave.

## What was checked
| Check | Result |
|---|---|
| `Revolution.Core` and `Revolution.Modules.Employees` build, warnings as errors | Pass, 0 warnings, 0 errors (.NET 10 SDK, Linux) |
| Core, Employees and Attendance engine tests, run through a throwaway runner with a small xunit stand-in | 136 of 136 pass |
| New field-crypto code and tests (Sigma) | 21 more cases, all pass; Core and the crypto service build with warnings as errors |
| `SqlAttendanceRepository` against the `att.*` procedures | Read, not compiled (needs the SQL client package, blocked here). Parameter names, types and error numbers match 0202. |
| Migrations on a SQL Server | **Not run.** No SQL Server here. Guards run. |
| Screens run and screenshotted | Not done. No attendance or employee screens exist yet. |
| Real xunit run, WinForms projects, Setup | Not done here. Needs Windows CI. |

## Decision
- **Phase 3, Employees module code (validators, code format, draft, mask, tests): approved.** Merge stands.
- **Phase 4, attendance engine, month summary, month lock rules, overtime limits, service, repository: approved as code.** Merge stands.
- **Phase 3 database lane: not approved as merged.** Two sets of masters and employee scripts exist (Brave 0100, 0103, 0105, 0108 and Sigma 0101, 0102, 0104, 0106, 0107, 0109, 0110) with the same table names. The IF-not-exists guards skip the second set, and the scripts after it fail. Remove Brave's four. Guard 7 now fails the build on this.
- **Neither phase gate is met.** The gate needs CI green on Windows, real xunit pass, screens run and checked, and the leak test. None is proven yet. Phases 3 and 4 stay "in progress".

## Findings for Brave (none block the merge)
1. `SqlAttendanceRepository.SaveDay`: the new foreign key (0203) can raise SQL error 547 for an unknown employee. It is not mapped to `AttendanceRuleException`, so the screen would see a raw `SqlException`. Map 547 on `FK_AttendanceDaily_Employee`.
2. Error 50002 (no company in session) is mapped as a rule error. It is a session fault, not a user mistake. Let it surface as a technical error.
3. Roster procedures (0206) use errors 50020 and 50021. Map them when the roster repository is written.
4. Attendance procedures with a transaction (close and reopen in 0202) do not set `XACT_ABORT` inside the procedure. A failure after the first statement can leave a transaction open until the connection is closed. Corrected by Sigma in migration 0207 (the three procedures re-created with the setting inside). Guard 8 now fails the build for any procedure that opens a transaction without it.
5. The tenant leak test now exists: `tests/Revolution.Database.Tests` (Sigma). It needs a migrated, disposable SQL Server database and the environment variable `HRMS_TEST_CONNECTION`; see its README. It has not been run yet. Add Brave's own procedures to it as they are written.
6. Tests: 87 facts and 49 data rows. Add a test that a closed month rejects `SaveDay` through the service, if not already covered.

## ReportViewer spike
See ADR 0005, "Review of the first spike commit". It is an empty template. No verdict.
