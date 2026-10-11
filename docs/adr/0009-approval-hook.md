# ADR 0009: Approval hook (maker-checker)

Status: accepted, items marked Open to confirm. Owner: Sigma.

- **What it is.** `IApprovalService` (Core) lets a module ask a second person to approve an action, for example a payroll run (Phase 5). Tables and procedures: `sec.ApprovalRequest` and `sec.usp_Approval_*` (migration `0009`).
- **When it applies.** Only when the plan has `DualApproval` (Platinum). Below that, `IsRequired` is False and `IsCleared` is always True, so a module can call it unconditionally.
- **Rules.** The submitter needs Edit on the module; the approver needs Approve on the module and must be a different user than the submitter. The database enforces that too. One pending request per entity (filtered unique index). Reject needs a reason of 10 or more characters. Only the submitter can cancel. A decided request cannot be decided again.
- **How a module uses it.** Before the final step (for example "finalise payroll run"): `If approvals.IsRequired(Payroll) AndAlso Not approvals.IsCleared(Payroll, "PayrollRun", runId) Then` submit and stop. After approval, proceed and lock the run. A rejected or cancelled request can be resubmitted.
- **Tenancy.** `sec.ApprovalRequest` has a CompanyID and the tenant security policy (ADR 0007). The company comes from the session, not a parameter.
- **Audit.** Submit, approve, reject and cancel call `IAuditService.Record`. Approvals exist only on Platinum, which keeps the full trail.
- **Not covered.** An approval does not freeze the data it approved. The module must lock the entity after approval, or re-submit if it changes. Month reopen keeps its own history (ADR 0008 addendum).

Open: with the default permissions the Owner approves and the Associate prepares. A company with one Owner and no Associate cannot self-approve. Confirm this is wanted, or add a second approver role.
