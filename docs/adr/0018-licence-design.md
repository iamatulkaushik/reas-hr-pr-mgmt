# ADR 0018: Licence design

Status: **proposed, for the business to confirm** (open items at the end). Owner: Sigma. Date: 2026-10-15. Follows ADR 0013 ("a separate licensing design, by module, database and user control, issued by the Developer"). Nothing here is built. Until it is, no code checks a licence and `IEntitlementService` stays the only gate.

## What the licence must do
- Be issued by the **Developer** under the Developer's own name, offline, with no licence server.
- Limit what one **Associate** may use: the highest plan, which modules, how many companies, how many users, for how long.
- Belong to **one installation** (one database), so a copy of the file does not work on another installation.
- Never lock people out of their own payroll data.

## Decisions
1. **A licence is a signed file.** The payload is JSON, signed with ECDSA P-256 (SHA-256). The file is `base64url(payload) + "." + base64url(signature)`. The app holds one or more **public** keys, each with a key id, and rejects anything that does not verify. The **private** key never enters the repository or the app. Only the Developer's licence tool holds it.
2. **Payload fields:** licence id (GUID), key id, issued-by (the Developer's user name), issued-on, **installation id**, the Associate's user name, valid-from, valid-to, **plan cap** (Copper to Platinum), **modules** (list of `ModuleKey` values), **max companies**, **max active users**, optional note. It holds no employee or company data.
3. **Installation id.** `first-run` writes a random GUID into the database (`cfg.Installation`). The Developer screen shows it; the Developer puts it in the licence. A restored copy of the same database keeps the id, which is accepted: whoever has the database copy already has the data.
4. **Where the licence lives: in the database, not in a file on each PC** (`cfg.Licence`, one current row per Associate, older rows kept). Every PC then sees the same licence. The Developer imports it from a Licence screen (the Developer already has the Licence module). The app verifies the signature every time it reads it, so a forged row is ignored.
5. **Time comes from SQL Server (`SYSUTCDATETIME()`), not from the PC's clock**, so changing a PC's date does not extend a licence.
6. **The licence tool is separate from the app:** `tools/Revolution.LicenceTool`, not packaged in the installer. Commands: `keygen` (makes the key pair; the private key is saved password-protected; prints the public key and key id to paste into Core), `issue` (builds and signs a licence from options), `inspect <file>` (shows what a licence grants). Losing the private key means no new licences can be issued under that key id; recover by shipping an app build with a new public key.
7. **Enforcement goes through services, as ADR 0003 already requires.** `ILicenceService` (Core, new) reports the status and limits. `IEntitlementService` then uses the **lower** of the company's plan and the licence's plan cap, and treats a feature as off when its module is not licensed. `IPermissionService` denies a module that is not licensed. Menus, screens and Brave's modules change nothing: they already ask those two services.
8. **Database enforcement where it is cheap.** The company limit is already enforced in the database (`cfg.PlanQuota`, error 50034, ADR 0014). Importing a licence also writes that Associate's **company cap** (a per-Associate override, still ceilinged at 500), so the database, not only the app, stops a company being created over the cap. The user cap is checked in the user-creation procedures the same way. Expiry and module limits are app-level.
9. **States and what each does.**

| State | When | Effect |
|---|---|---|
| Valid | within dates | everything the licence grants |
| Expiring | last 30 days | everything, plus a banner |
| Grace | up to 15 days after valid-to | everything, plus a banner |
| Expired | after grace | **read-only**: view, print, export and the audit trail; no create, edit, process or approve |
| Missing or invalid | no row, bad signature, wrong installation id | treated as Trial |
| Trial | 30 days from `first-run` | Copper limits, one Associate; then read-only like Expired |

   The Developer can always sign in and import a licence, in every state.
10. **Honest limits.** This is a desktop app. Someone with .NET skills can patch the app or swap the public key, and the app is not obfuscated. The licence stops casual copying and honest overuse; the strong controls are the ones the database enforces (company and user caps) and the contract. There is no revocation, because there is no server: use a short term and re-issue.

## Work to do (Sigma, in this order)
1. Core: `LicenceStatus`, `LicenceGrant`, `ILicenceService`; a pure `LicenceVerifier` (parse, verify signature, check dates and installation id) with unit tests using a test key pair.
2. Migration lane 00xx: `cfg.Installation`, `cfg.Licence`, a per-Associate company-cap override, procedures to import and read (Developer-only, checked in the database like ADR 0014), user-cap check. `first-run` writes the installation id.
3. Services: `LicenceService`; `EntitlementService` and `PermissionService` read it; a shell banner for Expiring, Grace and Expired.
4. A Licence screen for the Developer (installation id, current licence, import).
5. `tools/Revolution.LicenceTool`, added to the solution but excluded from CI like ReportViewer if it needs anything the runner lacks.
6. A test matrix: each state against each role and plan, run in CI.

## Open: the business decides
- Licence **term** (one year? three months?) and whether the 30-day warning and 15-day grace are right.
- Is there also an **employee count** cap per company (common for HRMS)? Adds a field and a database check.
- Is **read-only after expiry** acceptable, or should an expired licence stop sign-in except for the Developer? Read-only is recommended: salary and statutory filing deadlines do not wait for a renewal.
- Trial: 30 days, Copper, one Associate: right?
- Who keeps the **private key** and its backup (two named people), and the policy for a lost key.
- Crystal Reports redistribution terms (ADR 0005 and ADR 0013): still to be confirmed in writing before Phase 10. They do not change this design.
