# Phase 4 plan (Brave): attendance

## Done on branch feature/attendance-engine
- Revolution.Modules.Attendance (pure rules, no database, Core only):
  - ShiftDefinition: day and night shifts, unpaid break, full and half day minutes, overtime rounding step.
  - AttendanceEngine: punches to Present, HalfDay or Absent, worked minutes, overtime above scheduled minutes. Missing or odd punches return NeedsReview.
  - MonthSummaryBuilder and MonthSummary: days paid, LOP days, overtime hours. Days paid + LOP + unmarked = days in month.
  - MonthLockPolicy: close only after the month ends and every day is marked. Reopen needs a reason of 10+ characters.
  - OvertimeLimitChecker: weekly and quarterly cap warnings. Caps come from the caller.
- Revolution.Modules.Attendance.Tests: 42 cases.
- Build: 0 warnings, 0 errors. Same files as the Employees tests: 101 passed, 0 failed through a throwaway runner with warnings as errors. A real xunit run is not done. NuGet is blocked here. Needs Windows CI.

## Blocked, waiting on Sigma
| Item | Waiting for |
|---|---|
| Add projects to solution | CR 0008 |
| att tables, procedures, month-lock table, security policy | CR 0009 (migration lane, data access pattern) |
| Who may reopen a month | CR 0009 |
| Attendance forms (daily, bulk, month close) | BaseForm, theme, controls (earlier CR 0004) |
| Shift and OT features only on Gold | PlanFeature.ShiftOtLop already exists. Screens ask IEntitlementService. |

## Next, once unblocked
1. Migrations 0200-0299: att.Shift, att.AttendanceDaily (unique EmployeeID + date), att.MonthLock, security policy per ADR 0007.
2. Procedures `att.usp_<Entity>_<Action>`, all with the company filter.
3. AttendanceRepository and AttendanceService: permission check, month-lock check, audit, OperationResult.
4. Screens: frmDailyAttendance, frmMonthClose.
5. Tests: operator cannot edit a locked month, reopen audited, cross-company leak.

## Assumptions to confirm
- Half day if net worked time is at least the half-day minutes. Full day if at least the full-day minutes. Both set per shift.
- The unpaid break comes off only when the in-to-out span is longer than a per-shift limit (suggest 300 minutes, the Factories Act rest interval). Confirm.
- Overtime rounds down to the company step (1, 15, 30 or 60 minutes). Round down is an assumption.
- Overtime counts only on a Present day, only above scheduled paid minutes.
- Weekly and quarterly overtime caps come from the rate table. No number is in code. Haryana values need a labour consultant.
- Work on weekly off or a holiday is entered by hand. Pay rules for it come in Phase 5.
- A month closes only after its last day. The last day itself is not enough.
