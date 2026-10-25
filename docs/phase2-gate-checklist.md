# Phase 2 gate checklist

PLAN gate: the role x plan x screen matrix passes. Nothing below has been run on Windows CI with SQL Server. Tick each item with the date and who ran it. Do not close Phase 2 until every box is ticked.

## A. Run the tests that need no database (any Windows machine, .NET 10 SDK)
- [ ] `dotnet build Revolution.slnx -c Release` has no warnings (warnings are errors).
- [ ] `dotnet test Revolution.slnx -c Release` is green.
- [ ] `RolePlanLicenceMatrixTests` ran and passed (not skipped): the seed scripts grant exactly the documented role x plan matrix and no script removes a grant.
- [ ] `AuthServiceTests`, `EntitlementServiceTests`, `AuditServiceTests`, `ApprovalServiceTests`, `FieldCryptoTests` are in the run output.

## B. Run the database tests (a disposable SQL Server database)
- [ ] Create an empty database (for example `HRMS_Test`). Never use a real one.
- [ ] `set HRMS_SETUP_CONNECTION=<db_owner connection string>` then `dotnet run --project src/Revolution.Setup -- migrate`. All scripts apply with no error.
- [ ] Run the migrate step a second time. It reports nothing to do (idempotent) and no checksum error.
- [ ] `set HRMS_TEST_CONNECTION=<same string>` and `set HRMS_REQUIRE_DB_TESTS=1`, then `dotnet test tests/Revolution.Database.Tests`. Every test passes, none is skipped. Without the second variable the tests pass without checking anything.
- [ ] After the new migration 0026: apply it and, by hand, check that `aud.usp_Reveal_InsertBatchForRun` refuses (a) a draft run (50053), (b) an employee of another run (50054), (c) an empty reason (50044); and logs one row per employee for an approved run.

## C. Run the screens (the part no test covers)
- [ ] Login: wrong password five times locks the account; the first login with a temporary password forces a password change.
- [ ] One user of each role (Developer, Owner, Associate, Operator) on one company of each plan (Copper, Silver, Gold, Platinum). For every menu item, the item is visible and works only where the matrix says; a hidden item cannot be reached by keyboard.
- [ ] Licence: an expired or revoked licence stops a company at login with a clear message; the Developer can still sign in.
- [ ] Switch company (Associate with two companies): data from the first company is not visible after the switch.
- [ ] Audit: each login, failed login, role change and approval above appears on the audit screen with user and time; a Copper company shows no full audit trail, a Platinum company does.
- [ ] Screenshot every screen above into `docs/reviews/phase2-gate/` with the date in the file name.

## D. CI
- [ ] CI is green on `main` on windows-latest. CI does not start a SQL Server, so section B is not covered by CI yet. Decision needed: a SQL Server service for CI (Sigma writes it), or keep section B as a manual gate before every phase close.

## Sign-off
Run by: ____ Date: ____ Reviewed by the other developer: ____
