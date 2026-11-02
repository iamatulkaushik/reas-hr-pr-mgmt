# Change request 0021: wire the Arrears module and its two screens

- Requested by: Brave, 2026-11-02. Owner decision: the arrears list and the arrears report each have their own form, in their own module, apart from the payroll run.
- Needed by module / phase: Payroll, Phase 7 step 7c (arrears list and report).
- What is missing or wrong in the shared code: the new projects `Revolution.Modules.Arrears` and `Revolution.Modules.Arrears.UI` are in the solution (Brave added three lines to `Revolution.slnx`, Sigma to confirm) but nothing composes them, and the menu layout has no position for their two screens.
- Proposed signature or schema change:
  1. `Revolution.App`: a `ProjectReference` to both projects, and one line `ArrearsModule.Compose(services.Host)`, which returns `ArrearsServices`. Then `New ArrearsShellModule(arrearsServices)` is added to the shell modules like `PayrollShellModule`.
  2. `MenuLayoutModule`: two `AddScreen` lines. `arrears.list` under Process (Brave suggests sort 580, after Leave ledger) and `arrears.report` under Reports (Brave suggests 790, after Detailed). Both use `ModuleKey.Payroll` and `PlanFeature.BasicPayroll`, as the module does.
  3. Review of migrations 0327 (tables), 0328 (tenant policy) and 0329 (procedures) in the pay lane. Error numbers 53100 to 53104.
- Workaround until done: none. The screens open from a test harness only.
- Sigma's decision (accepted / changed / rejected) and date: **Accepted, 2026-11-02.** Sigma did the wiring: `Revolution.App` references both projects, `Program.vb` adds `New ArrearsShellModule(ArrearsModule.Compose(services.Host))` to the sources, and `MenuLayoutModule` has `arrears.list` under Process (580) and `arrears.report` under Reports (790), as suggested. The solution entries are confirmed.

Migration review (0327, 0328, 0329): **accepted, no changes needed.** The company comes from session context only, the header totals are worked out in `usp_ArrearsClaim_Add` from the month rows, the open-month overlap check holds an update lock inside the transaction, and the tenant policy covers both tables. Two notes for the next step (7d), not blockers:
1. `usp_ArrearsClaim_Add` trusts each month's figures from the caller. Check in the service that `GrossDifference = DueGross - PaidGross` before the call, or add that check in the procedure when the add-claim screen is built.
2. `Reason` and `CancelReason` accept an empty string. The service already requires a reason; keep that check as the only guard, or add a `LEN(LTRIM(RTRIM(...))) > 0` check constraint in a later migration (never edit 0327).
