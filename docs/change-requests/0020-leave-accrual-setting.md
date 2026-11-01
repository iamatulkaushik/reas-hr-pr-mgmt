# Change request 0020: leave accrual setting under company management settings

- Requested by: Brave, 2026-11-01. Owner decision: leave accrues as days earned per days worked, set by the company under management settings.
- Needed by module / phase: Payroll, Phase 7 (leave ledger accrual).
- What is missing or wrong in the shared code: there is no company setting for leave accrual and no screen to set it. `LeaveAccrualSetting` and `LeaveAccrualCalculator` (Revolution.Modules.Payroll) are built and tested, but nothing stores or reads the setting.
- Proposed signature or schema change:
  1. New table `cfg.LeaveAccrualSetting` (Sigma lane, 04xx migration): CompanyID, LeaveTypeID, EarnedDays DECIMAL(7,2), PerWorkedDays DECIMAL(7,2) with a check above zero, RoundingID (1 Up, 2 Down, 3 Nearest, same as `RateRounding`), MaxBalance DECIMAL(7,2) NULL, audit columns. One row per company and leave type. Row-level security like the other company tables.
  2. Proc `cfg.usp_LeaveAccrualSetting_Get` (company, leave type) and `_Save` (Owner or Associate only, audited).
  3. A field group on the company management settings screen: earned days, per worked days, rounding, cap, for each leave type.
  4. Brave then adds `ILeaveAccrualSource` and a month-end accrual call in `LeaveService` that posts the earned days to the append-only leave ledger from the days actually worked (present days, not paid holidays).
- Workaround until done: leave days are entered by hand in the ledger.
- Sigma's decision (accepted / changed / rejected) and date:
