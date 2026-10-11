# ADR 0007: Security model (Phase 2)

Status: accepted, items marked Open to confirm. Owner: Sigma.

- **Passwords:** PBKDF2-SHA256, 210,000 iterations, 16-byte salt, 32-byte hash. Iteration count is stored per user so it can be raised later. Policy: 10+ characters with upper, lower, digit and symbol.
- **Lockout:** 5 consecutive failures lock the account for 15 minutes; the count restarts after a lock. Counting happens in `sec.usp_User_RecordFailedLogin`, not in application memory.
- **Forced change:** `MustChangePassword` on the user row flows into `UserSession`. While it is true, `IPermissionService` denies everything; only `IAuthService.ChangePassword` works.
- **No user enumeration:** unknown user, inactive user and wrong password return the same message and take the same time.
- **Developer:** has no company (`CompanyID` NULL, session CompanyId 0, Platinum). Seeded permissions cover Security, Licence and Audit only, never employer data.
- **Tenancy:** the connection factory sets `SESSION_CONTEXT` `CompanyID` and `IsDeveloper` after opening. `aud.AuditLog` has a row-level-security filter predicate. Every new table that has a CompanyID must join a security policy in its own migration. The Developer reads audit rows of all companies (diagnostics).
- **Permissions:** role x module x action rows in `sec.RolePermission`, seeded by `0004`. Services check `IPermissionService.IsAllowed`; nobody compares a role name. Change the matrix with a new migration, then call `PermissionService.InvalidateCache` or restart.
- **Plans:** `PlanFeatureMap` is the single feature-to-plan table. The plan comes from `mst.Company.PlanTierID` at login.
- **Audit:** Security-module events are always recorded. Other modules are recorded only when the plan includes `FullAuditTrail` (Platinum). The audit insert stamps the tenant from the session.
- **Database access:** the app login is in role `hrms_app` with EXECUTE only (`0006`).

Open: how the first Developer user and the first company are created (installer bootstrap, Phase 10). Open: an Owner who manages several companies on Platinum needs a user-to-company link table.

## Addendum: setup tool and first accounts

- `src/Revolution.Setup` (`RevolutionSetup.exe`) applies `database/migrations` in order and records each script's checksum in `cfg.SchemaVersion`. A script edited after it was applied stops the run. Line endings are normalised before hashing.
- The migration runner is the only code that runs SQL text instead of a stored procedure, because it is what creates the procedures. Everything else stays stored-procedure-only (ADR 0001).
- `add-company` and `add-user` use `mst.usp_Company_Create` and `sec.usp_User_Create`, which `0007` denies to `hrms_app`. The tool needs an administrator connection string in `HRMS_SETUP_CONNECTION`.
- Users made this way must change their password at first login.
- Passwords entered in the tool are read with hidden input; never pass a password as an argument.
