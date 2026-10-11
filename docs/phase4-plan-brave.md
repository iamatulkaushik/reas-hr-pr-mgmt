# Phase 4 plan (Brave): attendance

## Done on branch feature/attendance-engine (merged)
- Revolution.Modules.Attendance (pure rules, no database, Core only):
  - ShiftDefinition: day and night shifts, unpaid break, full and half day minutes, overtime rounding step.
  - AttendanceEngine: punches to Present, HalfDay or Absent, worked minutes, overtime above scheduled minutes. Missing or odd punches return NeedsReview.
  - MonthSummaryBuilder and MonthSummary: days paid, LOP days, overtime hours. Days paid + LOP + unmarked = days in month.
  - MonthLockPolicy: close only after the month ends and every day is marked. Reopen needs a reason of 10+ characters.
  - OvertimeLimitChecker: weekly and quarterly cap warnings. Caps come from the caller.
- Revolution.Modules.Attendance.Tests: 42 cases.
- Build: 0 warnings, 0 errors. Same files as the Employees tests: 101 passed, 0 failed through a throwaway runner with warnings as errors. A real xunit run is not done. NuGet is blocked here. Needs Windows CI.

## Done on branch feature/attendance-data
CR 0007, 0008 and 0009 are accepted (ADR 0008). Unblocked items below.
- Migrations in lane 0200-0299:
  - 0200: att.Shift, att.AttendanceDaily (unique company + employee + date), att.MonthLock (state, reopen user, time, reason), att.MonthLockEvent (append-only close and reopen history).
  - 0201: tenant security policy on all three tables (ADR 0007).
  - 0202: procedures. Company comes from SESSION_CONTEXT, never a parameter. Error 50001 to 50005 for closed month, no company, already closed, not closed, reason too short. Reopen writes the unlock and the history row in one transaction.
- Revolution.Modules.Attendance:
  - IAttendanceRepository and SqlAttendanceRepository (IConnectionFactory, StoredProcedure). Database rule errors become AttendanceRuleException.
  - AttendanceService: MarkDay (Edit), LoadMonth (View), CloseMonth (Approve), ReopenMonth (Reopen, reason audited). Every business change is audited.
  - Shifts and overtime need PlanFeature.ShiftOtLop. Future days refused. Overtime only on a Present day.
- Tests: 24 new service cases (permission, plan, lock, reopen, history on a plan without audit trail). Total 66 for the module.
- Verified here: .NET 10 SDK, Core and both modules build, 0 warnings, 0 errors. Guards pass. The 24 new cases ran 24 passed through a throwaway runner. SqlAttendanceRepository compiled against a stub of Microsoft.Data.SqlClient only. NuGet is blocked here. Needs Windows CI and a real SQL Server run of 0200 to 0202.

## Not done yet
| Item | Why |
|---|---|
| Foreign key att.AttendanceDaily.EmployeeID to mst.Employee | Employee table comes in lane 0100 (phase 3 database work) |
| Shift save and list in repository and service | Procedures exist (att.usp_Shift_Save, att.usp_Shift_List). Add with the shift screen |
| Screens: frmDailyAttendance, frmMonthClose | Next step. BaseForm and Theme exist now |
| Cross-company leak test | Needs real SQL Server |

## Next
1. Phase 3 database lane 0100-0199 (employees), then the key from 0200.
2. Attendance screens on BaseForm.
3. Shift service and screen.

## Assumptions to confirm
- Half day if net worked time is at least the half-day minutes. Full day if at least the full-day minutes. Both set per shift.
- The unpaid break comes off only when the in-to-out span is longer than a per-shift limit (suggest 300 minutes, the Factories Act rest interval). Confirm.
- Overtime rounds down to the company step (1, 15, 30 or 60 minutes). Round down is an assumption.
- Overtime counts only on a Present day, only above scheduled paid minutes.
- Weekly and quarterly overtime caps come from the rate table. No number is in code. Haryana values need a labour consultant.
- Work on weekly off or a holiday is entered by hand. Pay rules for it come in Phase 5.
- A month closes only after its last day. The last day itself is not enough.
- Closing a month needs Approve (Owner only today). Reopen is a separate permission.
- Decided with Sigma: the reopen reason is stored in att.MonthLock and att.MonthLockEvent on every plan. The IAuditService call stays and is plan-gated.
- Employee foreign key: added by a later 0200-lane migration after mst.Employee is merged.
