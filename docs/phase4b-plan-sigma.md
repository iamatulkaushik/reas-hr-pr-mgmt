# Phase 4b: monthly attendance (Sigma to Brave)

Date: 2026-11-11. Decision: ADR 0026 (read it first). Today the Attendance module stores and imports attendance per employee per day (`att.AttendanceDaily`, the list template Emp Code, Date, Status, OT Hours). The Associate's sheets are one row per employee per month, so the standard path becomes monthly and the daily path becomes the Platinum extra. This is the work order. The shared parts are done (`PlanFeature.DailyAttendance`, `BulkEntryForm` with `initialRows`, `ImportTemplate`, `BulkImportRunner`).

## 1. Data (Brave's migrations, 02xx lane, after 0209; ordering rule of ADR 0024)
**`att.AttendanceMonthly`**: `CompanyID`, `EmployeeID`, `PeriodStart` DATE (always the first of the month), then integer counts `PresentDays`, `HalfDays`, `WeeklyOffDays`, `HolidayDays`, `PaidLeaveDays`, `AbsentDays` (each 0 to 31), `OvertimeHours` DECIMAL(5,2) (0 to 744), `Remarks` NVARCHAR(100) NULL, `UpdatedUtc`, `UpdatedByUserID`. Unique on (`CompanyID`, `EmployeeID`, `PeriodStart`). Row-level security policy and the same employee foreign key pattern as `att.AttendanceDaily` (0203). The meanings are exactly those of `MonthSummary`: days paid = present + weekly off + holiday + paid leave + half the half days; loss of pay = absent + half the half days.

**`att.AttendanceSetting`**: `CompanyID` primary key, `Mode` TINYINT (1 Monthly, 2 Daily), `UpdatedUtc`. No row means Monthly. A procedure sets it; setting Daily is refused (error 53601) unless the company's plan is Platinum (the plan is on `mst.Company`; check how `add-company` stores it), and the setting changes only the screens, never data.

**Procedures** (stored procedures only, as always): `att.usp_AttendanceMonthly_Save` (one employee and month, upsert), `usp_AttendanceMonthly_List` (a month: every active employee of the company with the saved row or empty counts, the employee's window days in the month, and whether the row is complete), `usp_AttendanceMonthly_Delete` (one employee and month), `usp_AttendanceSetting_Get`/`_Set`. Rules in the procedures, as for the daily ones:
- The month is closed (`att.MonthLock`, `IsClosed = 1`): refuse with the same error as `usp_Attendance_Save` (50001).
- The month already has daily rows for that company: refuse (53602). A month with monthly rows refuses daily entry the same way (patch `usp_Attendance_Save` in a later script).
- The employee is not employed in that month (joining date after the month, or exit before it): refuse (53603). The window is from the later of the first of the month and the joining date to the earlier of the last of the month and the exit date.
- The counts do not add up to the window days: the save procedure stores the row but the list marks it incomplete; the import and the list screen refuse to call it complete (53604 is the error when a caller asks for a complete save).
- Each count is not more than the window days and every value is a whole number except overtime (53605).

**The seam for payroll and leave: `att.fn_MonthAttendance(@CompanyID, @PeriodStart)`**, an inline table function returning per employee `PresentDays`, `HalfDays`, `WeeklyOffDays`, `HolidayDays`, `PaidLeaveDays`, `AbsentDays`, `OvertimeMinutes`, `UnmarkedDays`, taken from the monthly row if the company month has monthly rows and otherwise from `att.AttendanceDaily`. Then switch the two readers to it, each in a new script numbered after the last script that defines that procedure (ADR 0024): the run input procedure (defined in 0311, check for later patches) and the leave accrual procedure (0333). Add the test `MigrationOrderTests` already gives you: the last definition keeps the patch.

**Overtime hours per day.** The run input takes the employee's shift `FullDayMinutes`. With no shift (monthly mode) use the new company policy `OtHoursPerDay` DECIMAL(4,2) default 8, a column on `pay.CompanyPolicy` (also in `usp_Policy_Get` and `_Save`, again after 0315) and a field on the policy screen.

## 2. Services (Attendance module)
- `AttendanceMonthlyService`: the same permission as today (View, Create, Edit, Delete on Attendance; Delete and the lock rules unchanged) and the plan feature `Attendance`. Every call also refuses Daily mode when the company is not on `DailyAttendance`.
- `MonthlyAttendanceHandler` implementing `IBulkRowHandler` (ADR 0025) for the grid and the import. `Validate`: the employee code exists and is employed in the month; numbers read with `ImportValue.TryWholeNumber` and `TryDecimal`; counts add up to the window (message: "Days add up to 24 of 30. Fill Absent Days or the missing days."); no value in any message. `RowKey`: the employee code. `Save`: calls the service (an upsert: importing the same sheet again replaces the rows of that month, unlike the employee import which only creates). Rows for a closed month are refused.
- After the run, the import reports the employees of the company that are **not** in the sheet (count and the first few codes), because a missing employee blocks the month close.
- `MonthSummaryBuilder` builds the `MonthSummary` of a monthly row (the constructor takes the counts, the overtime minutes and the unmarked days), so `MonthLockPolicy.CanClose` and the month close work as they do now. Month view in monthly mode lists the month's rows with the totals and the incomplete rows.
- The attendance mode is read by a small service so the menu items can open the right form.

## 3. Template and screens
Template **"Attendance Monthly"** (the same template serves the grid and the file; the period, year and month, is chosen on screen and defaults to the previous month, so it is not a column):

| Column | Required | Aliases | What to enter |
|---|---|---|---|
| Emp Code | Yes | Employee Code, Code | as on the roll |
| Present Days | Yes | Days Present, Worked Days | whole days present |
| Half Days | No | HD | number of half days |
| Weekly Off Days | No | WO | if the column is missing from the file, the company's weekly-off rule gives it |
| Holiday Days | No | Holidays, H | if the column is missing, the company's holiday list gives it |
| Paid Leave Days | No | Leave Days, PL, CL EL SL | paid leave taken |
| Absent Days | No | Absent, LOP Days | unpaid days |
| OT Hours | No | OT, Overtime, Overtime Hours | up to 744, two decimals |
| Employee Name | No | Name | for the Associate's reference; never saved (used only in the grid) |

Screens, keeping today's menu keys so the menu does not change; each factory opens the monthly form when the company mode is Monthly and today's daily form when it is Daily:
- `attendance.insert.multiple` and `attendance.update`: `BulkEntryForm` with `startWithFile:=False` and `initialRows` = every active employee of the company with the saved values (empty counts for new rows). Update is the same screen.
- `attendance.insert.import`: `BulkEntryForm` with `startWithFile:=True`; a month picker first.
- `attendance.insert.single`: one employee, the counts as numeric boxes and a live total against the window days.
- `attendance.delete`: delete one employee's row for the month (Associate before the lock, as today).
- `attendance.monthview` and `attendance.monthclose`: both modes.
`attendance.insert.range`, `attendance.roster.shifts` and `attendance.roster.assign` are daily only; Sigma moves their plan feature to `DailyAttendance` when your monthly screens are pushed. Holidays and Weekly off stay in every plan because they fill the monthly counts.

## 4. Muster roll and registers
The legal attendance muster roll is a day-by-day register. In monthly mode print the monthly totals with the day cells left blank for the client to complete; do not invent days. The Owner can change this (for example by marking the first N days present), but that would be made-up data, so it is not the default.

## 5. Tests
Table and procedure tests (tenant isolation, EXECUTE-only, the five refusals, closed month, upsert, one source per month); `att.fn_MonthAttendance` for a monthly month, a daily month and a company that switched; the handler (each message, a repeated code, the defaults from weekly-off and holiday rules, a closed month, the same sheet twice); the Platinum-only Daily mode; payroll run on a monthly month equal to the same numbers entered daily (a known-answer test, same paid days and overtime, the CA-style check of Phase 5); the month close; the leave accrual. Target: 100 employees saved in under 2 seconds and 2,000 in under 30 (Phase 10 load test).

## 6. Order and gate
1. Migrations and procedures, with the database tests (CI `database` job green).
2. `att.fn_MonthAttendance` and the payroll and leave readers; the known-answer test.
3. Service, handler, template, tests.
4. Screens (Multiple and Update, Import, Single, Delete, Month view).
5. Sigma: move the daily-only entries to `DailyAttendance`, add Attendance mode to the company settings, update the README.
Gate: CI green on both jobs; a real sheet of one client (about 100 employees) imported and closed in a month; the same month run through the old and the new app and compared to the rupee (Owner); screens screenshotted.

## 7. Decisions taken (Owner can change them)
Strict complete rows (no silent absent). Weekly off and holidays from the company rules when the column is missing. One source per month. Daily mode is Platinum only. The daily import template stays for Daily mode.
