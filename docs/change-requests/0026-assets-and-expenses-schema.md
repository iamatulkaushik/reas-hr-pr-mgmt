# Change request 0026: a schema for assets and expenses

- Requested by: Brave, 2026-11-08. Phase 9 (`docs/phase9-plan-brave.md`).
- Needed by module / phase: Assets and Expenses, Phase 9, lane 0600 to 0699.
- What is missing or wrong in the shared code: `0001_init_schemas.sql` creates `cfg, sec, mst, att, pay, stat, aud` and nothing for assets or expenses. Creating a schema is in Sigma's lane (0001 to 0099).
- Proposed signature or schema change: one new schema `ast` for both modules (asset types, assets, issues, expense categories, claims), created in a Sigma migration in the 0030s, so Brave's tables start at 0600. If Sigma prefers two schemas, `ast` and `exp`, or to place them in `pay`, say so before Brave starts; nothing is written until then. Payroll recoveries and reimbursements would use the existing `pay` recovery ledger and the run's earnings, not new payroll tables.
- Workaround until done: none. Phase 9 also waits for the Owner's answers in the plan.
- Sigma's decision (accepted / changed / rejected) and date:
