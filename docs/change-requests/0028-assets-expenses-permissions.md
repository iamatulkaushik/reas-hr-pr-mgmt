# Change request 0028: Assets and Expenses permissions for Phase 9

- Requested by: Brave, 2026-11-11, from the Owner's answers in `docs/phase9-plan-brave.md`.
- Needed by module / phase: Assets and Expenses, Phase 9.
- What is missing or wrong in the shared code: migration 0004 seeds Assets as Owner all, Associate View, Create and Edit, Operator View; and Expenses as Owner all, Associate View, Create, Edit and Export, Operator View and Create. The Owner says Assets and Recovery is Owner only (Platinum), and expenses are entered by the Operator and verified and approved by the Owner only.
- Proposed signature or schema change: a new Sigma migration in the 0030s that sets Assets to Owner all and nothing for Associate and Operator, and Expenses to Owner all (including Approve), Operator View and Create, Associate nothing (the Owner confirmed, follow-up C). No code change.
- Workaround until done: none. Brave builds the screens against the new rights and the tests set rights explicitly.
- Sigma's decision (accepted / changed / rejected) and date: **Accepted as proposed, 2026-11-11.** Migration `0034_permission_assets_expenses_owner_only.sql` (after 0033). No code change in Brave's modules.
1. **Assets:** the Owner has all six actions (View, Create, Edit, Delete, Approve, Export); the Associate and the Operator have none.
2. **Expenses:** the Owner has all six (Approve included, so the Owner verifies and approves); the Operator has View and Create (enters claims); the Associate has none.
3. **How:** 0034 deletes the older rows that 0004 seeded for the Associate and the Operator and asserts the Owner and Operator rows again; it is idempotent. `MatrixSpec` (which also had the Associate's Edit on Security from 0032 missing) and the README tables are updated, so the matrix tests assert the new rights.
4. **Menu:** the planned report "Assets and Recoveries" now uses the Assets module for its permission, so only the Owner sees it. When Brave builds the Assets and Expenses screens, use `ModuleKey.Assets` and `ModuleKey.Expenses` and the plan feature `AssetsAndExpenses`; Sigma places the menu entries.
5. **Brave's tests** set the rights explicitly, as proposed; keep them so a seed change cannot hide a mistake.
