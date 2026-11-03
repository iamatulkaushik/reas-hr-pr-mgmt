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
- Sigma's decision (accepted / changed / rejected) and date: **Accepted with changes, 2026-11-01.**

1. **Table and procs move to the pay lane (Brave, 0326), not 04xx.** The table points at `pay.LeaveType` (0317), and a Sigma-lane migration (0001 to 0099) would run before it exists. 04xx is the statutory lane (no CompanyID, no tenant policy), so it does not fit a company table. Keep the name `pay.LeaveAccrualSetting`, with its tenant policy in its own migration (guard 6). Sigma reviews the migration.
2. **Columns accepted as proposed.** One row per company and leave type. Checks: `PerWorkedDays > 0`, `EarnedDays >= 0`, `MaxBalance` NULL or `>= 0`. `RoundingID` 1 Up, 2 Down, 3 Nearest. `_Save` is allowed for Owner and Associate only, writes an `IAuditService` entry, and uses a stored procedure (ADR 0008).
3. **Screen.** There is no company management settings screen yet. Sigma builds that host later. Until then Brave adds a plain form under Payroll (Leave accrual) in `Revolution.Modules.Payroll.UI` with a single `AddScreen` line in `MenuLayoutModule`, to be moved into the settings screen as one field group.
4. **Posting.** Accrual posts from present days only, once per company, leave type and month, so a second call for the same month is refused. Brave owns `ILeaveAccrualSource` and the `LeaveService` call.

**Built 2026-11-02 (Brave):** migrations 0326 (`pay.LeaveAccrualSetting`, `pay.LeaveAccrualRun`), 0332 (tenant policy, own migration) and 0333 (procedures); `ILeaveAccrualSource`, `SqlLeaveAccrualSource`, `LeaveAccrualService` (rule save and posting for Owner and Associate only, Payroll Edit, FullAndFinal plan feature, audit without days; posting from present days only, once per leave type and month, never for a month that has not started); plain form `LeaveAccrualForm` in `Revolution.Modules.Payroll.UI`; one `AddScreen` line in `MenuLayoutModule` (key `payroll.leaveaccrual`, sort 575) and one icon line in `MenuIconMap`. Two corrections to the text above: `RoundingID` follows the real `RateRounding` enum (1 Nearest, 2 Up, 3 Down, not 1 Up, 2 Down, 3 Nearest), and the rule lives in `PayrollServices.LeaveAccrual`, so the settings host can call it later.
