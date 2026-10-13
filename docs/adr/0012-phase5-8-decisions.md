# ADR 0012: Decisions for Brave, Phases 5 to 8

Status: accepted, items marked Open to confirm. Owner: Sigma. Answers Brave's question list of 2026-10-13.

## Change requests
- **CR 0011 (PT due months): not decided.** The file is not on origin/main (last CR is 0010). Send it. The principle is fixed: due months are statutory data, so they go in effective-dated `stat.*` rows with Verified, VerifiedOn and SourceRef, never in code.
- **New stat tables (LWF, minimum wage, bonus rule, TDS deduction limits, HRA metro list): accepted.** File one batch CR (0012 or next free number) with the columns each needs and the phase that needs it first. Same pattern as 0400: no CompanyID, no tenant policy, read procedures only, rows loaded `Verified = 0` and flipped after the CA signs. Sigma writes the Core types, `IStatutoryRateService` and repository members and the SQL readers; the tables go in migrations 0402 and up. Order: TDS deduction limits and HRA metro list (Phase 6), bonus rule (Phase 5/7), LWF and minimum wage (Phase 7).

## Permissions and plans
- **No new `PlanFeature` and no new `ModuleKey` are needed.** Payroll, Statutory, Reports and Biometric modules exist. Plan features already exist for BasicPayroll, Payslips, EpfEsi, ProfessionalTax, Loans, BankFiles, Tds, Form16And24Q, Arrears, FullAndFinal, Biometric, ShiftOtLop and DualApproval, mapped in `PlanFeatureMap` (ADR 0003 draft split, unchanged).
- **Mapping to use.** Loans: module Payroll, feature Loans (Silver). EPF, ESI, PT, LWF, bonus, bank files: module Payroll or Statutory, feature EpfEsi, ProfessionalTax or BankFiles. TDS, Form 16, 24Q: module Statutory, features Tds and Form16And24Q (Gold). Biometric agent and punches: module Biometric, feature Biometric (Gold). LWF follows ProfessionalTax; bonus and gratuity follow EpfEsi. A genuinely new feature changes what is licensed and needs a CR and the Owner's confirmation.
- **Grants already seeded (0004).** Owner has Payroll and Statutory View, Create, Edit, Delete, Approve, Export. Associate has View, Create, Edit, Export. Operator has none. Gaps, to be seeded by Sigma in migration 0014: Owner Reopen (7) on Payroll (a locked run reopens only with a written reason, same rule as the attendance month); Associate Create and Edit on Biometric (device and code mapping).

## Approvals (payroll run)
- Call `IApprovalService` with module Payroll, **entity name `PayrollRun`**, entity id the run id as text, summary from one fixed template of at most 500 characters: period, run number, employee count, gross, deductions, net.
- **There is no payload.** An approval does not freeze data (ADR 0009). Brave must (a) set the run status to "submitted" so it cannot be edited, (b) store a totals checksum on the run at submit and compare it at finalise, and (c) cancel the approval and return the run to draft if anything changes. Maker and checker are user ids.
- Other entity names, added when the phase starts: `ArrearsRun`, `FinalSettlement`, `BankFile`.
- **Status history on every plan.** The audit trail keeps non-security events only on Platinum (ADR 0007). Run status changes (who, when, from, to, reason) go into an append-only `pay` table, as `att.MonthLockEvent` does.

## Database conventions
- **Money:** `DECIMAL(14,2)` for every new amount column. Percentages and rates `DECIMAL(7,4)`. Hours and days `DECIMAL(7,2)`. No float or money types. Existing `DECIMAL(12,2)` columns stay (merged scripts are never edited). Parameters set precision and scale explicitly.
- **Lanes confirmed:** 0300 to 0399 `pay.*`, 0400 to 0499 stat and tax, 0500 to 0599 biometric, 0600 to 0699 assets and expenses. **New: 0700 to 0799 reports (`rpt` procedures).** Sigma keeps 0001 to 0099 for security, agent login and registration, schemas and permission seeds.
- **Reports:** a new schema `rpt` (this amends ADR 0004's schema list). It holds read-only procedures `rpt.usp_<Report>_Get` and no tables. A report spans masters, attendance and payroll, so one schema avoids cross-module permission surprises and makes "read only" checkable. Sigma creates the schema in 0014; Brave writes the procedures in lane 0700. A new guard fails the build if an `rpt` procedure contains INSERT, UPDATE, DELETE or MERGE.
- **Report data path:** the app runs the `rpt` procedure on its own tenant-safe connection and gives Crystal a data file (a typed DataSet written to a temp file, deleted after). A .NET Framework helper therefore needs no database connection, no credentials and no tenant context.

## Crystal spike (ADR 0005)
- **x86 and .NET Framework 4.8: approved for the helper project only** (`Revolution.ReportViewer`). Use x64 if the runtime in use supports it. The main app stays net10.0-windows, AnyCPU.
- **Package source:** use only SAP's own developer package and runtime installer. Do not use unofficial NuGet repackages. Record the exact version, download page and install steps in ADR 0005. Do not commit installers or SAP binaries.
- **Licence:** this needs the SAP licence terms for redistributing the runtime read and confirmed in writing by the business. Sigma cannot answer it. Open: the Owner of the business.
- **CI:** until the verdict, the helper cannot build on the runner (no SAP assemblies). Keep it in the solution (guard 4) but build it only on the machine that has the SAP install; Sigma will add a condition to CI so the runner skips it.

## Payroll screens and composition root
- **Brave builds the payroll screens** (module project `Revolution.Modules.Payroll`, forms on `BaseForm`). Sigma provides BaseForm, theme, login and shared controls.
- **Composition root:** `AppServices` (Revolution.App) is written and committed locally (`bedd5c7`), waiting to be pushed. It exposes session, entitlements, permissions, audit, auth, approvals, rate service, field crypto and the connection factory. Modules take what they need through their constructors.
- **Next from Sigma:** a module registration contract and the real shell menu (module key, title, required feature and permission, how to create the main form). Until then Brave can open forms from a small test harness. No dates: this is gate-based.

## CI and database
- Windows CI exists in `.github/workflows/ci.yml` and starts when the repo is pushed. The SQL Server gates need an instance: use SQL Server Developer edition (Standard features) on a dev machine and set `HRMS_TEST_CONNECTION`.
- Sigma will add a second CI job on Linux with a SQL Server service container that runs the migrations and `Revolution.Database.Tests`. Open: whether `Revolution.Setup` migrate runs on Linux. It is checked when the job is written.

## Unattended work and the biometric agent
- **No unattended payroll.** Payroll, statutory and approval work is always started by a signed-in user, on that user's session. SQL Agent runs backups only.
- **Agent login:** a separate SQL login `hrms_agent` in its own role, EXECUTE only on a small named set of procedures (punch ingest and agent authentication). Created by Setup with the administrator connection. Its connection string is kept machine-protected on the agent computer. It does not use the app login.
- **No session company for the agent.** The company comes from the agent's registration row, resolved inside the procedure, never from a parameter or from `SESSION_CONTEXT` set by the client. Open: how the lookup passes row-level security (the tenant predicate hides rows when no company is set). Sigma settles it in migration 0015 and adds the case to the tenant leak test.
- **Agent auth: per device, bound to one company.** The Owner registers a device (permission Biometric, Create); a random 32-byte key is shown once and only its hash is stored. Revoking one lost device does not touch the others. The agent keeps its key in a machine-protected file. Tables and procedures: Sigma, migration 0015. Punch tables and `IPunchSource`: Brave, lane 0500.

## Audit events
- **Fixed list.** Sigma adds a Core class `AuditActions` with constants, so names cannot drift. Format `<Entity>.<Verb>`, plus the existing names (`ApprovalSubmitted` and similar) kept as they are.
- **Payroll run:** `PayrollRun.Create`, `.Process`, `.Submit`, `.Approve`, `.Reject`, `.Cancel`, `.Finalise`, `.Reopen`, `.MarkPaid`, `.DeleteDraft`. Brave asks for additions through a CR. The approval service records its own approval events; do not duplicate them.
