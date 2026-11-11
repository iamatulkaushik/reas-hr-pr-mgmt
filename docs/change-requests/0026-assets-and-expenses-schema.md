# Change request 0026: a schema for assets and expenses

- Requested by: Brave, 2026-11-08. Phase 9 (`docs/phase9-plan-brave.md`).
- Needed by module / phase: Assets and Expenses, Phase 9, lane 0600 to 0699.
- What is missing or wrong in the shared code: `0001_init_schemas.sql` creates `cfg, sec, mst, att, pay, stat, aud` and nothing for assets or expenses. Creating a schema is in Sigma's lane (0001 to 0099).
- Proposed signature or schema change: one new schema `ast` for both modules (asset types, assets, issues, expense categories, claims), created in a Sigma migration in the 0030s, so Brave's tables start at 0600. If Sigma prefers two schemas, `ast` and `exp`, or to place them in `pay`, say so before Brave starts; nothing is written until then. Payroll recoveries and reimbursements would use the existing `pay` recovery ledger and the run's earnings, not new payroll tables.
- Workaround until done: none. Phase 9 also waits for the Owner's answers in the plan.
- Sigma's decision (accepted / changed / rejected) and date: **Accepted as proposed, 2026-11-11.** Migration `0033_ast_schema.sql` creates the one schema `ast` for both modules and gives `hrms_app` EXECUTE on it, as for every other schema (0006). Brave starts at 0600.

1. **One schema, `ast`.** Asset types, assets, issues, expense categories and claims all go in it. Recoveries and reimbursements use the existing `pay` recovery ledger and the run's earnings, as proposed; no new payroll tables.
2. **Row-level security is Brave's, per table.** Every `ast` table with a `CompanyID` gets a filter policy in the script that creates it (the pattern of 0301 and 0501). Guard 6 and `TenantIsolationTests` fail the build without one.
3. **Access is stored procedures only.** The app login has no table access in `ast`. Reports read through procedures too.
4. **Order of scripts.** 0033 comes before 0600, so the ordering rule of ADR 0024 holds. A later patch to an `ast` table or procedure goes in a script numbered after the last one that touches it.
5. **Errors:** 53500 to 53599 are reserved for `ast`.
6. **Receipts and bills.** If the Owner wants files attached, that needs a place to keep them. Send a new change request and Sigma decides where they live (database or file store, size limit, who may read).
7. **Plan and permissions:** `PlanFeature.AssetsAndExpenses` is Platinum. The rights are changed by CR 0028 (migration 0034): Assets and Expenses are the Owner's, the Operator enters expenses. Services check both, as the Arrears services do.
