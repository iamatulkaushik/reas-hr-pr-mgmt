# Change request 0015: Two EPF higher-wage flags per employee

- Requested by: Brave
- Needed by module / phase: Phase 7 (ECR) and payroll run (EPF calculation)
- What is missing or wrong in the shared code: `mst.EmployeeStatutory` has one flag, `HigherWageOption`. The Owner wants a per-member choice of EPF on higher wage and EPS (pension) on higher wage. They are separate choices. EPS on higher wage needs EPF on higher wage.
- Proposed signature or schema change: Add `EpfOnHigherWage BIT` and `EpsOnHigherWage BIT` to `mst.EmployeeStatutory` (Sigma lane). Keep `HigherWageOption` as is until migrated: both new flags start from its value. `StatutoryFlagsValidator`: EPS on higher wage needs EPF on higher wage; both need EPF membership. `StatutoryFlags` gets the two properties.
- Workaround until done: Brave reads `HigherWageOption` as EPF on higher wage and treats EPS on higher wage as off. `PayrollFileEmployee.EpsOnHigherWage` is filled by the caller.
- Sigma's decision (accepted / changed / rejected) and date: pending
