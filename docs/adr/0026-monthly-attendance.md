# ADR 0026: Attendance is entered monthly; daily attendance is a Platinum feature

Status: accepted (Owner, 2026-11-11: "keep daily attendance for owner Platinum plan only, else attendance is monthly always"). Owner of this ADR: Sigma. Work order for Brave: `docs/phase4b-plan-sigma.md`.

**Why.** An Associate gets attendance from the client as an Excel sheet with one row per employee per month. Typing or importing a row per employee per day is about thirty times the rows (50 companies of 100 employees is 5,000 rows a month, not 150,000) and is the slowest step of the monthly cycle. The app must beat the offline applications Associates use today on exactly this step.

**Decision.**
1. **Every plan has monthly attendance:** one row per employee per company month, with the day counts and overtime hours of the sheet. It is entered on a list (Multiple), typed for one employee (Single) or imported from Excel (Import), and it feeds payroll, leave accrual and the month close.
2. **Daily attendance is the Platinum feature `PlanFeature.DailyAttendance`:** attendance day by day, shifts, shift assignment, the range entry and (with the biometric special build) punches. It is what exists today in the Attendance module.
3. **Reading of "owner Platinum plan":** the Platinum plan (a company feature), not the Owner role. If a role rule is wanted as well, say so; this ADR gates by plan only.
4. **One source per company month.** A month is either monthly rows or daily rows, never both: the first entry decides, and entering the other kind for that month is refused. Months before a switch keep their own data. Payroll reads both through one function (`att.fn_MonthAttendance`), so a company can move from monthly to daily later without changing payroll.
5. **A company setting `AttendanceMode`** (Monthly, the default, or Daily) chooses which entry screens the company sees. Daily can be set only on a Platinum company; the database refuses it otherwise.
6. **The monthly row is complete or it is not saved as complete.** Present days, half days, weekly off, holidays, paid leave and absent days must add up to the days of the employee's employment in that month. A row that does not add up is refused at import and marked incomplete in the list; the month cannot close while an employee row is incomplete or missing (the existing rule for unmarked days). This is stricter than leaving blanks as "absent" on purpose: a silent loss of pay is a wrong salary.
7. **Weekly off and holidays are filled from the company's rules** when the sheet has no such column: the weekly-off rule and the holiday list already in the Attendance module give the counts for each employee's window.
8. **Overtime** is entered in hours. Payroll converts it with the employee's shift hours when there is a shift, and otherwise with the company policy `OtHoursPerDay` (new, default 8).

**Shared code (Sigma, done with this ADR).** `PlanFeature.DailyAttendance` (Platinum) in `PlanFeatureMap`, the matrix spec and a test; `BulkEntryForm` can open pre-filled (`initialRows`), so Multiple shows every employee of the company with the saved values. After Brave's screens exist, Sigma moves the daily-only menu entries (Range, Shifts, Shift assignment) to `DailyAttendance` and adds the Attendance mode to the company settings.

**Not changed.** The month lock (`att.MonthLock`, who may reopen, the reason of 10 characters), the daily tables, the daily import (Emp Code, Date, Status, OT Hours) and the biometric special build stay as they are for Daily mode. Errors 53600 to 53649 are reserved for monthly attendance.
