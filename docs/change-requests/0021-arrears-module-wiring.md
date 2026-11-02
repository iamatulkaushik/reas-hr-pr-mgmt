# Change request 0021: wire the Arrears module and its two screens

- Requested by: Brave, 2026-11-02. Owner decision: the arrears list and the arrears report each have their own form, in their own module, apart from the payroll run.
- Needed by module / phase: Payroll, Phase 7 step 7c (arrears list and report).
- What is missing or wrong in the shared code: the new projects `Revolution.Modules.Arrears` and `Revolution.Modules.Arrears.UI` are in the solution (Brave added three lines to `Revolution.slnx`, Sigma to confirm) but nothing composes them, and the menu layout has no position for their two screens.
- Proposed signature or schema change:
  1. `Revolution.App`: a `ProjectReference` to both projects, and one line `ArrearsModule.Compose(services.Host)`, which returns `ArrearsServices`. Then `New ArrearsShellModule(arrearsServices)` is added to the shell modules like `PayrollShellModule`.
  2. `MenuLayoutModule`: two `AddScreen` lines. `arrears.list` under Process (Brave suggests sort 580, after Leave ledger) and `arrears.report` under Reports (Brave suggests 790, after Detailed). Both use `ModuleKey.Payroll` and `PlanFeature.BasicPayroll`, as the module does.
  3. Review of migrations 0327 (tables), 0328 (tenant policy) and 0329 (procedures) in the pay lane. Error numbers 53100 to 53104.
- Workaround until done: none. The screens open from a test harness only.
- Sigma's decision (accepted / changed / rejected) and date:
