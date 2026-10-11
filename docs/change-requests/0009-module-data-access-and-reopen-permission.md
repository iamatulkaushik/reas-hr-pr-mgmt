# Change request 0009: Module data access pattern and month reopen permission

- Requested by: Brave
- Needed by module / phase: Phase 4 (attendance tables and procedures), Phase 3 database work
- What is missing or wrong in the shared code:
  1. Repositories live in Revolution.Data (Sigma's). Repository interfaces live in Revolution.Core (Sigma's). `StoredProcedure` helpers are `Friend`. A module cannot write its own repository without editing shared projects.
  2. `att` tables and procedures need migrations. database/ is Sigma's. No number block is agreed.
  3. Reopening a closed month needs a permission. The seed in 0004 gives Attendance `Approve` to Owner only. Older notes said an Associate may also reopen, with a written reason.
- Proposed signature or schema change:
  1. Pick one. (A) Modules hold their own `Sql<Name>Repository` in the module project, using the public `IConnectionFactory`, and `StoredProcedure` becomes `Public`. Interfaces live in the module. Core stays small. (B) Brave sends each repository interface as a change request and Sigma adds it to Core and Data. Brave prefers A.
  2. Reserve migration numbers for Brave, reviewed by Sigma: 0100-0199 masters and employees, 0200-0299 attendance, 0300-0399 payroll, 0400-0499 statutory and tax, 0500-0599 biometric, 0600-0699 assets and expenses. Every `att` table with a CompanyID joins a security policy in its own migration (ADR 0007).
  3. Decide who may reopen. Option one: Owner only (matches the seed today, no change). Option two: Associate as well, via a new migration that grants Attendance `Approve` to Associate. That also lets an Associate approve anything else the screens map to Approve, so use a separate action if that is not wanted.
- Workaround until done: Brave keeps the engine pure (no database) and drafts SQL on a branch only. Nothing merges.
- Sigma's decision (accepted / changed / rejected) and date:
