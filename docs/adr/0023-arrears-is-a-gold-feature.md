# ADR 0023: Arrears is a Gold feature

Status: accepted (Owner, 2026-11-09). Owner: Sigma.

- `PlanFeatureMap` already lists `PlanFeature.Arrears` at Gold (ADR 0003, `MatrixSpec`). The Arrears screens and services were gated by `PlanFeature.BasicPayroll` (Copper), so a Copper or Silver company could open them. The plan map is right; the gates are brought to it.
- **Done by Sigma:** the two menu entries in `MenuLayoutModule` (`arrears.list`, `arrears.report`) use `PlanFeature.Arrears`; the README tables follow.
- **For Brave** (module code, with its tests): `PlanFeature.BasicPayroll` becomes `PlanFeature.Arrears` in `ArrearsShellModule` (two entries) and in the four services that refuse a plan without payroll: `ArrearsService`, `ArrearsRunService`, `ArrearsEcrService`, `ArrearsPrepareService`. The refusal message becomes "Arrears is not part of your plan." Tests that enable only `BasicPayroll` for these services must enable `Arrears`; add one test per service that a plan without `Arrears` is refused.
- **Not affected:** arrears that a regular payroll run recovers or includes stay with Basic payroll; this decision is only about the Arrears screens, the arrears run, the arrears ECR file and the arrears report.
