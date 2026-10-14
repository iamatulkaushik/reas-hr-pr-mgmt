# Phase 5 start: what Sigma delivers and what Brave builds

Date: 2026-10-11. Phases 3 and 4 are approved as code (see `docs/reviews/2026-10-11-sigma-review-phase3-4.md`); their gates stay open.

## Delivered by Sigma (shared code, ready to use)
| Item | Where |
|---|---|
| Field encryption: `IFieldCrypto`, `FieldContext`, `AesGcmFieldCrypto`, key set, backup, DPAPI store, Setup key commands | Core, Services, Setup. ADR 0011, CR 0010 |
| Approval hook (Platinum only): `IApprovalService` | ADR 0009. Use it for the payroll run approval |
| Rate service: `IStatutoryRateService` returns EPF, ESI, professional tax (with slabs) and the wage rule for a date, or a failure message | Core, Services, Data. Tables in 0400, readers in 0401 |
| Attendance transaction fix (0207) and guards 6 to 8 | database, scripts |

Use the rate service for every rate. A failure (no row, or row not signed off by the CA) must stop the payroll run with that message. Never put a rate, ceiling or slab in code.

## Brave builds (Phase 5, lane 0300 for tables and procedures)
1. Salary structure: components with flags (EPF wage, ESI wage, overtime base, wage exclusions), templates, employee salary effective-dated, revisions.
2. Payroll run: draft, processed, approved, locked, paid. Approval only when `IApprovalService.IsRequired` is true. Maker and checker are user ids, never logins.
3. EPF, ESI, PT calculation in VB services (decimal, `MidpointRounding.AwayFromZero`, rounding kind from the rate row for ESI). Known-answer tests from `docs/sql-intake` (EPF 12 / 3.67 / 8.33 / 0.5 / 0.5 on a 15,000 ceiling; ESI 0.75 / 3.25 on 21,000 rounded up; PT by slab). The CA signs the expected values.
4. Loans and EMI, payslip data. Reports wait for ADR 0005.
5. Every table has a `CompanyID`, a composite key to its parent, and a tenant policy in its own migration (guard 6).

## Blocked, and by whom
| Blocker | Who |
|---|---|
| ADR 0005 Crystal verdict (needed before payslip reports) | Brave finishes the spike, Sigma reviews |
| Rates: none loaded. Each row needs the CA's sign-off | Owner of the business and the CA |
| Employee ids on a real machine: first Windows run of the key store | Sigma and CI |
| Duplicate migration scripts from the older Phase 3 lane | Sigma. Fixed once Brave's 0100, 0103, 0105 and 0108 are removed. Guard 7 fails until then |
| Phase gates (CI green, screens run, leak test) | Brave and Sigma, on Windows CI and a SQL Server |

## Sigma next
1. ~~Composition root~~ Done 2026-10-13: `AppServices` in Revolution.App wires session, entitlements, permissions, audit, auth, approvals, rate service and field crypto. The app stops with a clear message when the key file is missing or cannot be opened. Modules get their services from `AppServices` (Brave registers module forms in the shell through a change request).
2. Re-encryption job for a new key version (after the first real rotation). Not started: no rotation has happened.
3. ~~Phase 6 contract: TDS rate reader~~ Done 2026-10-13: see `docs/phase6-contract-sigma.md`.
4. ~~Associate on several companies (ADR 0013, 0014)~~ Built 2026-10-14: migrations 0014 to 0016, company picker, create company and Owner. Not yet run on Windows or SQL Server.
5. Next: module registration contract and the real shell menu (with a switch-company item); the licence design (ADR) before Phase 10; the sign-off migration once the CA confirms the rates.
6. Review Brave's Phase 5 pull requests against the list above. Nothing to review yet: origin/main has no payroll tables or services (0300 lane is empty).
