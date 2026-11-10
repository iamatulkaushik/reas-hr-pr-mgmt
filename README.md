# Revolution HRMS

[![ci](https://github.com/iamatulkaushik/reas-hr-pr-mgmt/actions/workflows/ci.yml/badge.svg)](https://github.com/iamatulkaushik/reas-hr-pr-mgmt/actions/workflows/ci.yml)

Windows desktop HR and payroll software for Indian companies, built for the **Associate**: the payroll professional or CA who runs payroll and statutory filings for several employers from one installation. VB.NET, WinForms, .NET 10, SQL Server Standard, on-premise.

> **Status: in active development, no stable release yet.** Phases are tracked in [`docs/PLAN.md`](docs/PLAN.md). A phase is finished only when CI is green on a Windows runner, tests pass, the screens were run and screenshotted, and the other developer reviewed the pull request. Statutory rates are effective-dated data, never code; see [Statutory data](#statutory-data).

## Contents
1. [Who uses it](#who-uses-it)
2. [Featured](#featured)
3. [Roles and access](#roles-and-access)
4. [Plans](#plans)
5. [Screens by plan](#screens-by-plan)
6. [Upcoming](#upcoming)
7. [Security model](#security-model)
8. [Architecture](#architecture)
9. [Getting started](#getting-started)
10. [Testing and CI](#testing-and-ci)
11. [Repository layout](#repository-layout)
12. [Documentation](#documentation)
13. [Working agreement](#working-agreement)

## Who uses it
| Role | What they do |
|---|---|
| **Developer** | Hidden super-admin of an installation: creates Associates, sets their plan, imports licences, sees the audit trail. Sees no employer data. |
| **Associate** | Works on several companies (up to the plan's cap). Creates companies and the Owner login of each, prepares payroll and filings, is the second approver on payroll. |
| **Owner** | The employer's admin and final approver. Resets Operator passwords, reopens a locked payroll with a written reason. |
| **Operator** | Data entry only: employees, attendance, and the like. |

One database holds all companies; row-level security keeps each company's data apart. The database stays on the office server.

## Featured
**Masters and employees**
- Company units (factories), shifts, departments, designations, holidays.
- Single employee entry with basic, employment, statutory and nomination screens.
- Aadhaar, PAN, UAN, ESI IP number, bank account and document numbers are **encrypted in the application** (AES-256-GCM) and shown masked; showing one in clear needs the Reveal permission and is logged.

**Attendance**
- Daily attendance entry and update, weekly off, shift assignment, month-close lock.

**Payroll**
- Monthly payroll run, lock, and reopen (an Owner reopens a locked run with a written reason).
- Salary structures, EPF and ESI, professional tax (state slab tables), loans and EMI, leave ledger and accrual, bonus runs, arrears (with recovery), full and final settlement.
- Income tax (TDS) declarations, both regimes, FY 2026-27 rules.
- **Maker-checker approval** on payroll (Platinum): the person who submits is never the approver.
- Bank files and statutory exports (EPFO ECR, ESIC upload sheet): plain ids are decrypted only for the run being exported, only when it is approved or locked, and every export is logged first.
- Payslip screen and payslip data. Reports are built with **ReportStudio**, the in-house reporting suite under [`reports/ReportStudio`](reports/ReportStudio) (it replaced Crystal Reports, [ADR 0005](docs/adr/0005-crystal-reports.md)); checking payslip layouts, printing and bulk runs against real data is part of phase 9.

**Security and administration**
- Sign-in with lockout (5 wrong passwords lock the account for 15 minutes), forced password change at first sign-in, **File > Change Password** for everyone, and a permission-checked **reset** of another user's password ([ADR 0022](docs/adr/0022-password-change-and-reset.md)).
- Audit trail of changes and sign-in events; reveal log for protected ids.
- Signed **licence files** (ECDSA P-256) issued offline by the Developer with the licence tool; trial mode with Copper limits for 28 days ([ADR 0018](docs/adr/0018-licence-design.md)).
- A setup tool for migrations, first company and users, key management, and emergency password reset.

## Roles and access
Screens never check a role name. They ask `IPermissionService` (what the role may do in a module) and `IEntitlementService` (what the plan and licence include). A menu entry shows only when the user has View on its module and the plan includes its feature. The grants below are the seeded defaults (migrations 0004, 0008, 0013, 0014, 0032); change them with a new migration, never by editing an old one.

| Module | Developer | Owner | Associate | Operator |
|---|---|---|---|---|
| Masters | - | View, Create, Edit, Delete, Approve, Export | View, Create, Edit | View |
| Employees | - | all, plus Reveal | View, Create, Edit, Export, Reveal | View, Create, Edit |
| Attendance | - | all, plus Reopen | View, Create, Edit, Export | View, Create, Edit |
| Payroll | - | all, plus Reopen | View, Create, Edit, Export, Approve (second approver) | - |
| Statutory | - | all | View, Create, Edit, Export | - |
| Reports | - | View, Export | View, Export | - |
| Biometric (special build) | - | View, Create, Edit | View, Create, Edit | - |
| Assets | - | all | View, Create, Edit | View |
| Expenses | - | all | View, Create, Edit, Export | View, Create |
| Security | View, Create, Edit | View, Create, Edit | View, Create, Edit | - |
| Licence | View, Create, Edit | - | - | - |
| Audit | View, Export | View, Export | - | - |

"all" means View, Create, Edit, Delete, Approve and Export.

**Who may reset whose password** (database-enforced): a Developer resets an Associate; an Associate resets the Owner of the open company it works on; an Owner resets an Operator of its own company. Nobody resets themselves or a Developer in the app; the Developer's emergency tool is `RevolutionSetup reset-password`.

## Plans
Four plans; each includes everything in the plan below it. The plan of a company is the lower of its plan and the licence's plan cap.

| Feature | Copper | Silver | Gold | Platinum |
|---|:-:|:-:|:-:|:-:|
| Employees | yes | yes | yes | yes |
| Attendance | yes | yes | yes | yes |
| Basic payroll | yes | yes | yes | yes |
| Payslips | yes | yes | yes | yes |
| EPF and ESI | - | yes | yes | yes |
| Professional tax | - | yes | yes | yes |
| Loans | - | yes | yes | yes |
| Bank files | - | yes | yes | yes |
| TDS | - | - | yes | yes |
| Form 16 and Form 24Q | - | - | yes | yes |
| Arrears | - | - | yes | yes |
| Full and final settlement | - | - | yes | yes |
| Shift, overtime and loss of pay rules | - | - | yes | yes |
| Biometric (special build) | - | - | yes | yes |
| Dual approval (maker-checker) | - | - | - | yes |
| Full audit trail | - | - | - | yes |
| Assets and expenses | - | - | - | yes |
| Multi-company | - | - | - | yes |

**Companies per Associate:** Copper 5, Silver 15, Gold 35, Platinum 90 (enforced in the database; a licence can raise one Associate's cap, never above 500).

**Licence states:** valid; expiring (last 30 days, banner); grace (up to 15 days after expiry, banner); then **read-only** (view, print, audit trail; no export, create, edit, process or approve). With no usable licence the installation is a **trial**: Copper limits, one Associate, 28 days from first run, then read-only. The Developer can always sign in and import a licence. Time comes from SQL Server, not the PC clock.

## Screens by plan
Generated from the menu layout in `src/Revolution.App/MenuLayoutModule.vb`. **Available** screens open; **Planned** entries are listed in the menu but disabled ("not available yet"). The last column shows who has View on the screen's module; the plan column is the lowest plan that includes the screen.

**Master**

| Menu path | Screen | Status | Minimum plan | Visible to (View on module) |
|---|---|---|---|---|
| Master > Company | Basic | Planned | Copper | Owner, Associate, Operator |
| Master > Company | Statutory / Banking | Planned | Copper | Owner, Associate, Operator |
| Master > Company > Factory / Units | Units | Available | Copper | Owner, Associate, Operator |
| Master > Company > Factory / Units | Shifts | Available | Copper | Owner, Associate, Operator |
| Master > Company | Shut | Planned | Copper | Owner, Associate, Operator |
| Master | Department | Available | Copper | Owner, Associate, Operator |
| Master | Designation | Available | Copper | Owner, Associate, Operator |
| Master | Daily Wages | Planned | Copper | Owner, Associate, Operator |
| Master | Holidays | Available | Copper | Owner, Associate, Operator |

**Employee**

| Menu path | Screen | Status | Minimum plan | Visible to (View on module) |
|---|---|---|---|---|
| Employee > Add Employee | Single Employee | Available | Copper | Owner, Associate, Operator |
| Employee > Add Employee | Multiple Employee | Planned | Copper | Owner, Associate, Operator |
| Employee > Add Employee | Bulk Import Employee | Planned | Copper | Owner, Associate, Operator |
| Employee > Update | Basic | Available | Copper | Owner, Associate, Operator |
| Employee > Update | Employment | Available | Copper | Owner, Associate, Operator |
| Employee > Update | Statutory | Available | Copper | Owner, Associate, Operator |
| Employee > Update | Nomination | Available | Copper | Owner, Associate, Operator |
| Employee > Update | Education | Planned | Copper | Owner, Associate, Operator |
| Employee | Disable / Retire | Planned | Copper | Owner, Associate, Operator |
| Employee | Delete | Planned | Copper | Owner, Associate, Operator |

**Attendance**

| Menu path | Screen | Status | Minimum plan | Visible to (View on module) |
|---|---|---|---|---|
| Attendance > Insert | Single | Available | Copper | Owner, Associate, Operator |
| Attendance > Insert | Multiple | Planned | Copper | Owner, Associate, Operator |
| Attendance > Insert | Import | Planned | Copper | Owner, Associate, Operator |
| Attendance | Update | Available | Copper | Owner, Associate, Operator |
| Attendance | Delete | Planned | Copper | Owner, Associate, Operator |
| Attendance | Month close | Available | Copper | Owner, Associate, Operator |
| Attendance | Weekly off | Available | Copper | Owner, Associate, Operator |
| Attendance | Shift assignment | Available | Copper | Owner, Associate, Operator |

**Career**

| Menu path | Screen | Status | Minimum plan | Visible to (View on module) |
|---|---|---|---|---|
| Career | Promotion | Planned | Copper | Owner, Associate, Operator |
| Career | Increment | Planned | Copper | Owner, Associate, Operator |
| Career | Transfer | Planned | Copper | Owner, Associate, Operator |

**Process**

| Menu path | Screen | Status | Minimum plan | Visible to (View on module) |
|---|---|---|---|---|
| Process | Payroll | Available | Copper | Owner, Associate |
| Process | Lock Payroll | Available | Copper | Owner, Associate |
| Process | Bonuses | Available | Copper | Owner, Associate |
| Process | Full and Final Settlements | Available | Gold | Owner, Associate |
| Process | Bank file | Available | Silver | Owner, Associate |
| Process | Income tax declaration | Available | Gold | Owner, Associate |
| Process | Loans | Available | Silver | Owner, Associate |
| Process | Leave ledger | Available | Gold | Owner, Associate |
| Process | Leave accrual | Available | Gold | Owner, Associate |
| Process | Arrears | Available | Gold | Owner, Associate |

**Compliance**

| Menu path | Screen | Status | Minimum plan | Visible to (View on module) |
|---|---|---|---|---|
| Compliance > Monthly | EPFO (ECR file) | Available | Silver | Owner, Associate |
| Compliance > Monthly | ESIC (upload sheet) | Available | Silver | Owner, Associate |
| Compliance > Half-Yearly | Form 20 CL | Planned | to be assigned | Owner, Associate |
| Compliance > Half-Yearly | LWF Challan | Planned | to be assigned | Owner, Associate |
| Compliance > Annual | Form 34 | Planned | to be assigned | Owner, Associate |
| Compliance > Annual | Form V | Planned | to be assigned | Owner, Associate |
| Compliance > Annual | Form IV | Planned | to be assigned | Owner, Associate |
| Compliance > Annual | Form D | Planned | to be assigned | Owner, Associate |

**Reports**

| Menu path | Screen | Status | Minimum plan | Visible to (View on module) |
|---|---|---|---|---|
| Reports | Salary Sheet | Planned | Copper | Owner, Associate |
| Reports | Abstract | Planned | Copper | Owner, Associate |
| Reports | Arrears report | Available | Gold | Owner, Associate |
| Reports > Detailed | Payslip | Available | Copper | Owner, Associate |
| Reports > Detailed | Bonus | Planned | Copper | Owner, Associate |
| Reports > Detailed | Career History | Planned | Copper | Owner, Associate |
| Reports > Detailed | Loans and Advances | Planned | Silver | Owner, Associate |
| Reports > Detailed | F&F | Planned | Gold | Owner, Associate |
| Reports > Detailed | Assets and Recoveries | Planned | Platinum | Owner, Associate |
| Reports > Detailed | Form 130/168 | Planned | Gold | Owner, Associate |
| Reports > CA Reports | Disbursement Annual | Planned | Copper | Owner, Associate |
| Reports > CA Reports | Statutory EPF/ESI | Planned | Silver | Owner, Associate |
| Reports > CA Reports | Loans and Advances | Planned | Silver | Owner, Associate |

**File and Administration** (not tied to a plan)

| Menu | Screen | Status | Visible to |
|---|---|---|---|
| File | Select Company | Available (enabled when the user has more than one company) | users with more than one company |
| File | Change Password | Available | every signed-in user |
| File | Financial Year | Planned | - |
| File | Sign Out, Exit | Available | every signed-in user |
| Administration | Licences | Available | Developer |
| Administration | Reset user password | Available | Developer, Owner, Associate |

## Upcoming
Roadmap by phase (full gates in [`docs/PLAN.md`](docs/PLAN.md)). "Implemented" means the screens and services exist in the repository; each phase gate (CI, screenshots, review, and for payroll a CA's known-answer check) is signed off separately.

| Phase | Scope | State |
|---|---|---|
| 0 to 2 | Decisions, core contracts, security, roles, plans | Implemented |
| 3 | Masters and employees, encrypted ids | Implemented, more employee screens planned |
| 4 | Attendance: shifts, rules, month-close lock | Implemented, multiple and import entry planned |
| 5 | Payroll core, maker-checker, payslips | Implemented |
| 6 | Tax: TDS both regimes, declarations; Form 16 and Form 24Q (Form 130 and Form 138 under the Income-tax Act 2025) | TDS implemented, Form 16 and Form 24Q planned |
| 7 | Arrears, full and final, bank files, ECR and challans | Implemented (ECR, ESIC sheet, bank file), challans and some forms planned |
| 8 | Biometric and RFID punches | **Special build per customer**, not in the standard product ([ADR 0021](docs/adr/0021-biometric-is-a-special-build.md)): built once a customer gives the device make, model and support type |
| 9 | Assets and expenses, plus the ReportStudio checks on real data | Planned |
| 10 | Hardening: installer, licence enforcement, SQL Agent backups, security review, pilot month | Planned |

**Planned screens** (already in the menu, disabled): company basic and statutory profile, daily wages, shut days; multiple and bulk-import employee entry (the shared editable list and file import are built, [ADR 0025](docs/adr/0025-bulk-import-and-multi-row-entry.md); the screens are in [`docs/phase3b-plan-sigma.md`](docs/phase3b-plan-sigma.md)); employee education, disable or retire, delete; multiple and import attendance entry, attendance delete; career promotion, increment, transfer; Form 20 CL, LWF challan, Forms 34, V, IV and D; salary sheet, abstract, bonus, career history, loans and advances, F&F, assets and recoveries, Form 130/168 and the CA reports.

**Statutory points still open** (the CA verification pack, part G): the September 2026 EPF split month, EPS and EDLI eligibility, the ESI contribution-period rule and disability ceiling, Haryana LWF due date and wage definition, and Income-tax Act 2025 section codes.

**Arrears is a Gold feature** ([ADR 0023](docs/adr/0023-arrears-is-a-gold-feature.md)); the menu already follows, the module services are being aligned.

## Security model
- **Tenancy:** every table with a company id has a row-level security policy keyed on the session's company; the app sets the session context at sign-in. A guard script and a database test fail the build if a table lacks a policy.
- **App login is EXECUTE-only:** the app talks to the database through stored procedures; it cannot read tables directly or create users. Setup work uses a separate admin connection that the running app never has.
- **Passwords:** PBKDF2-SHA256 with a per-user salt and a stored iteration count; policy of 10 characters with upper case, lower case, a digit and a symbol. The database never sees a plain password.
- **Protected ids:** AES-256-GCM with the company, owner and field bound to each value, a separate HMAC blind index for duplicate checks, versioned keys held outside the database ([ADR 0011](docs/adr/0011-field-encryption.md)).
- **Approval:** the submitter of a payroll run is never its approver ([ADR 0009](docs/adr/0009-approval-hook.md)).
- **Licences:** signed with ECDSA P-256; the private key stays with the Developer's licence tool and never enters the repository or the app.
- **Honest limit:** this is a desktop app; the licence stops casual copying and honest overuse, and the strong controls are the ones the database enforces.

## Statutory data
Rates and rules live in effective-dated `stat.*` tables, loaded by migrations, so a rate change is data, not code. The national rates, income tax FY 2026-27 (both regimes), Haryana labour welfare and Haryana minimum wages were verified by a CA on 24-10-2026 (record in [`docs/ca-signoff`](docs/ca-signoff)); payroll and TDS refuse unverified rows while verification is required. State data (professional tax slabs, labour welfare, minimum wages) is added state by state, Haryana first, each with CA verification.

## Architecture
```
Revolution.App            shell, menu, composition root
Revolution.UI.Common      BaseForm, theme, controls, shared screens (login, password, licence)
Revolution.Modules.*      feature modules (Employees, Attendance, Payroll, Arrears, Biometric) and their *.UI projects
Revolution.Services       auth, permissions, entitlements, audit, approval, licence, password services
Revolution.Data           SQL Server access (stored procedures only), connection factory, module host
Revolution.Core           models, interfaces, OperationResult: no dependencies
Revolution.Setup          setup tool: migrate, companies, users, keys, password reset
tools/Revolution.LicenceTool   issues and inspects licences (not shipped with the app)
reports/ReportStudio      report designer, definition format and viewer
```
- Modules receive services through a `ModuleHost` and offer **menu entries** (`IShellModule`); the shell builds the menu from them ([ADR 0016](docs/adr/0016-module-menu-and-shell.md)).
- Every form inherits `BaseForm`; one theme class holds colours, fonts and spacing ([ADR 0006](docs/adr/0006-ui.md)).
- Stack: VB.NET (Option Strict On, warnings as errors), WinForms, .NET 10, SQL Server Standard.

## Getting started
**Requirements:** Windows, .NET 10 SDK, SQL Server (Standard or Developer).

```
dotnet build Revolution.slnx
dotnet test  Revolution.slnx
./scripts/check-guards.sh
```

**First run** (details in [`docs/CONTRACT-v0.1.md`](docs/CONTRACT-v0.1.md), section "Running it"):
1. Create an empty database named `HRMS`.
2. `set HRMS_SETUP_CONNECTION=<admin connection string>`, then `RevolutionSetup migrate`.
3. `RevolutionSetup add-company "Acme" Gold`, then `RevolutionSetup add-user dev Developer` and `RevolutionSetup add-user owner1 Owner 1` (each asks for a temporary password and forces a change at first sign-in).
4. Create a SQL login in the database role `hrms_app`, copy `src/Revolution.App/appsettings.example.json` to `appsettings.json` and edit the connection string.
5. Start the app. Developer imports the licence from Administration > Licences (until then the installation is a 28-day trial).

**Setup tool commands**

| Command | Purpose |
|---|---|
| `migrate [folder]` | apply database scripts (default `database/migrations`) |
| `add-company <name> <plan>` | create a company on Copper, Silver, Gold or Platinum |
| `add-user <userName> <role> [companyId]` | create a user; an Associate may have no company |
| `set-associate-plan <userName> <plan>` | set how many companies an Associate may hold |
| `reset-password <userName>` | emergency reset: temporary password, change forced, lock cleared |
| `keys-init`, `keys-info`, `keys-add-version`, `keys-backup <file>`, `keys-restore <file>` | field-encryption keys ([ADR 0011](docs/adr/0011-field-encryption.md)) |
| `create-agent-login <loginName>` | biometric special build only |

## Testing and CI
- GitHub Actions (`.github/workflows/ci.yml`) on a Windows runner: drift guards, restore, build and test of every project (the ReportViewer is excluded).
- `MigrationOrderTests` checks, without a database, that the migration scripts build an empty database in file order ([ADR 0024](docs/adr/0024-migrations-must-build-a-new-database.md)).
- A second job, `database`, starts SQL Server in a container, applies every migration (standard and `database/special/biometric`) and runs `tests/Revolution.Database.Tests` for real: tenant isolation, EXECUTE-only access, the agent wrapper and password reset rules.
- Unit tests per module and service. Locally, database tests need `HRMS_TEST_CONNECTION` (see `tests/Revolution.Database.Tests/README.md`) and otherwise return without checking.
- `scripts/check-guards.sh` fails the build on drift: SQL outside the migration folders, duplicate migration numbers, a table with a company id and no security policy, and similar rules.

## Repository layout
```
src/        application, modules, services, data access, setup tool
tests/      unit and database tests
database/   migrations (never edited after merge), special/ for customer special builds
docs/       plan, ADRs, change requests, module contracts, rates, CA sign-off
reports/    ReportStudio
tools/      licence tool
scripts/    guard checks
```

## Documentation
- Plan and phases: [`docs/PLAN.md`](docs/PLAN.md)
- Decisions (ADRs 0001 to 0025): [`docs/adr/`](docs/adr)
- Module contracts: [`docs/CONTRACT-v0.1.md`](docs/CONTRACT-v0.1.md)
- Change requests between module and core developers: [`docs/change-requests/`](docs/change-requests)
- Who owns what: [`docs/OWNERSHIP.md`](docs/OWNERSHIP.md)
- Statutory rates and CA sign-off: [`docs/rates/`](docs/rates), [`docs/ca-signoff/`](docs/ca-signoff)
- Icons: [`docs/icons.md`](docs/icons.md); ideas for later: [`BACKLOG.md`](BACKLOG.md)

## Working agreement
Two developers: **Sigma** owns shared code (core, services, data access, migrations, the shell) and **Brave** owns the feature modules. Brave asks for changes to shared code with a change request in `docs/change-requests/`; Sigma answers in the same file. New ideas go to the backlog, not into the current phase. See [`docs/OWNERSHIP.md`](docs/OWNERSHIP.md) and [`CLAUDE.md`](CLAUDE.md).

## License
No licence file is present in this repository. Treat the code as proprietary to Revolution Associates until the owner adds one.
