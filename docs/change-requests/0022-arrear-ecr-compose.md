# Change request 0022: pass the Employees export to the Arrears module

- Requested by: Brave, 2026-11-02. Step 7e of Phase 7: the arrear ECR file is built from the open arrears claims, and the UAN must be released through the Employees export (ADR 0019), as the payroll ECR does.
- Needed by module / phase: Payroll, Phase 7 step 7e.
- What is missing or wrong in the shared code: `Revolution.App/Program.vb` composes the module with `ArrearsModule.Compose(services.Host)`, which gives no UAN release, so the screen hides the Arrear ECR button.
- Proposed signature or schema change: one line in `Program.vb`, the same as for Payroll: `ArrearsModule.Compose(services.Host, employees.Export)`. The one-argument overload stays, so the App builds before and after the change. Nothing else in the App changes. Brave added a project reference from `Revolution.Modules.Arrears` to `Revolution.Modules.Employees` (Payroll already has it).
- Workaround until done: the ECR button stays hidden. Claims, list and report work as before.
- Sigma's decision (accepted / changed / rejected) and date:
