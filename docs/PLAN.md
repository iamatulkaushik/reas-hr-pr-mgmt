# Revolution HRMS: plan v2

No deadline. Quality gates instead of dates. Sigma owns shared code. Brave owns modules.

## Locked decisions
Stack: VB.NET, WinForms, .NET 10, SQL Server Standard, Crystal Reports. Roles: Developer, Owner, Associate, Operator. Plans: Copper, Silver, Gold, Platinum. Fresh repo; the three old repos are read-only reference.
Assumed, change if wrong: Owner = old "Company" role, created by an Associate who works across several companies (ADR 0013); one database; on-premise with a Developer-issued licence key, designed before Phase 10 (ADR 0013); custom-styled native controls; Crystal required with a .NET Framework helper as fallback.

## Phase gate (every phase)
CI green on a Windows runner, tests pass, the screens were actually run and screenshotted, the other developer reviewed the pull request.

## Phases
| # | Phase | Owner | Gate |
|---|---|---|---|
| 0 | Decisions and spikes: repo, ADRs, CI, guards, Crystal spike, UI toolkit choice | Sigma, Brave on Crystal | Empty solution builds in CI. ADR 0005 has a verdict. |
| 1 | Core contracts v0.1 freeze: models, interfaces, OperationResult, session, IPermissionService, IEntitlementService, IAuditService, BaseForm, theme, controls | Sigma | Brave can build a module against stubs. |
| 2 | Security, roles, plans: login, lockout, forced password change, 4 roles, 4 plans, audit trail, row-level tenancy | Sigma, Brave tests | Role x plan x screen matrix passes. |
| 3 | Masters and employees, encrypted IDs | Brave | |
| 4 | Attendance: shifts, half/full day rules, OT and LOP, month-close lock | Brave | |
| 5 | Payroll core: salary structure, EPF and ESI, PT (19+ states), loans and EMI, payslip run, dual approval, payslip reports | Brave, Sigma for approval hooks | Known-answer tests verified by a CA. |
| 6 | Tax: TDS (both regimes), declarations, Form 16, Form 24Q | Brave | |
| 7 | Arrears, full and final, bank files, ECR and challans | Brave | |
| 8 | Biometric and RFID agent (Windows Service, one brand first) | Brave | |
| 9 | Assets and expenses | Brave | |
| 10 | Hardening: installer, licence enforcement, SQL Agent backups, security review, pilot month | Both | |

## Risks
Crystal on .NET 10 (spike now). Statutory rates change (effective-dated tables, CA sign-off). Payroll maths (tests first). Sigma as bottleneck (publish Phase 1 freeze early, one-day change-request SLA). Biometric vendor SDKs (IPunchSource interface, one brand first).
