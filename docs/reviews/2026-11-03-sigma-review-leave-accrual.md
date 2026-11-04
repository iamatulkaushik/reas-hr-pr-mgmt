# Sigma review of Brave's leave accrual push (commit 07ce519), 2026-11-03

Scope: migrations 0326, 0332 and 0333, `LeaveAccrualCalculator`, `LeaveAccrualService`, the rounding codes in CR 0020, and Brave's two lines in `MenuLayoutModule` and `MenuIconMap`. Static review only: nothing was run (no SQL Server, no Windows). Guards pass.

## Approved as written
- **0326 tables.** `pay.LeaveAccrualSetting` is one row per company and leave type. Its checks are `PerWorkedDays > 0`, `EarnedDays >= 0`, `MaxBalance` NULL or `>= 0`, `RoundingID` 1 to 3. It points at `pay.LeaveType` (0317), so it sits correctly in the pay lane (CR 0020, point 1).
- **0332 tenant policy.** In its own migration, covering both tables, as guard 6 requires.
- **0333 procedures.** The company comes from session context only. `XACT_ABORT` is on, a month is posted once per company and leave type (a second post is refused), and the save procedure checks its inputs again.
- **Present days.** `usp_LeaveAccrual_Present` counts attendance status 1 as a full day and 3 as a half day; weekly off, holiday and paid leave do not count. That matches the attendance codes.
- **Ledger rows.** Posting writes entry type 2 (Accrual) with positive days, which `CK_LeaveLedger_Sign` requires.
- **Rounding numbers in CR 0020.** My original text (1 Up, 2 Down, 3 Nearest) was wrong. `RateRounding` is 1 Nearest, 2 Up, 3 Down, and Brave's migration comment, check constraint and form follow that. The corrected numbers in CR 0020 stand.
- **Edits to Sigma files.** One `AddScreen` line (`payroll.leaveaccrual`, sort 575) and one icon line, which CR 0020 allowed.

## Note for Brave (not a blocker)
- `LeaveAccrualCalculator` rounds the earned days with `Rupee.Round`, so accrual is always whole days: "up" is the ceiling, "down" the floor, "nearest" half a day or more. That is what the form says. If the Owner later wants half-day accrual, the calculator needs its own rounding step; `Rupee.Round` is for money.

## Still open
- The Leave accrual form does two jobs, the rule (a setting) and the monthly posting (a process). Brave splits it, then Sigma removes the duplicate Process menu entry. The settings screen that hosts the rule is built (`CompanySettingsForm`, see the settings patch).
