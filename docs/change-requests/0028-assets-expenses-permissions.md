# Change request 0028: Assets and Expenses permissions for Phase 9

- Requested by: Brave, 2026-11-11, from the Owner's answers in `docs/phase9-plan-brave.md`.
- Needed by module / phase: Assets and Expenses, Phase 9.
- What is missing or wrong in the shared code: migration 0004 seeds Assets as Owner all, Associate View, Create and Edit, Operator View; and Expenses as Owner all, Associate View, Create, Edit and Export, Operator View and Create. The Owner says Assets and Recovery is Owner only (Platinum), and expenses are entered by the Operator and verified and approved by the Owner only.
- Proposed signature or schema change: a new Sigma migration in the 0030s that sets Assets to Owner all and nothing for Associate and Operator, and Expenses to Owner all (including Approve), Operator View and Create, Associate nothing (the Owner confirmed, follow-up C). No code change.
- Workaround until done: none. Brave builds the screens against the new rights and the tests set rights explicitly.
- Sigma's decision (accepted / changed / rejected) and date:
