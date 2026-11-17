# Phase 5 gate checklist

PLAN gate: known-answer tests verified by a CA. Phase gate: CI green on Windows, tests pass, screens run and screenshotted, the other developer reviewed. Nothing below has been run on Windows CI with SQL Server. Tick each item with the date and who ran it. Do not close Phase 5 until every box is ticked.

Drafted 2026-11-14. The owner decided (2026-11-13) that the Windows CI and screen run happen after Phase 9. Compile errors will pile up until then (Assets and Expenses fakes already hit `Boolean?` errors). One early CI run is recommended; the gate itself stays after Phase 9.

## A. Bring the code up to date
- [ ] Apply and merge `cr0029` (landlord PAN option), `cr0030` (ESI higher ceiling), `cr0031` (ESI stay-eligible), in that order. None is compiled yet.
- [ ] Sigma decides CR 0029, 0030, 0031 (line at the end of each CR) and adds the two screen items: landlord PAN column on the rent grid (0029), ESI higher ceiling checkbox on the Statutory tab (0030).
- [ ] Migrations in order: 0110 (employees), 0344 (reads the 0110 column), 0345, 0416. Guard 7 and ADR 0024 (empty database builds) still pass.

## B. Tests that need no database (any Windows machine, .NET 10 SDK)
- [ ] `dotnet build Revolution.slnx -c Release` has no warnings (warnings are errors).
- [ ] `dotnet test Revolution.slnx -c Release` is green.
- [ ] These classes are in the run output, none skipped: `EpfCalculatorTests`, `EsiCalculatorTests`, `EsiContributionPeriodTests`, `PtCalculatorTests`, `PayrollRunCalculatorTests`, `NetPayCalculatorTests`, `LoanScheduleCalculatorTests`, `LoanRecoveryAllocatorTests`, `RunStateMachineTests`, `SalaryServiceTests`, `PayslipServiceTests`, `TdsCalculatorTests`, `TdsDeclarationValidatorTests`, `TdsProjectionServiceTests`, and in the Employees tests `EmployeeStatutoryServiceTests`.
- [ ] Sigma's finding 5 (2026-10-14): a test names "statutory deducted in full" for the shortfall case. Confirm it exists.

## C. Database tests (a disposable SQL Server database)
- [ ] Empty database. `dotnet run --project src/Revolution.Setup -- migrate`: all scripts apply, none fail. Second run reports nothing to do.
- [ ] `HRMS_TEST_CONNECTION` and `HRMS_REQUIRE_DB_TESTS=1`, then `dotnet test tests/Revolution.Database.Tests`: all pass, none skipped.
- [ ] Tenant leak test covers the `pay.*` tables (policies 0301, 0304, 0307, 0313 and later): a second company sees no rows from the first.
- [ ] By hand: `usp_TdsHraRent_Replace` refuses a row with a PAN and `PanOption` 1 or 2 (error 50344); stores `PanOption` 1 and 2 without a PAN; `usp_TdsHraRent_List` returns the value.
- [ ] By hand: `usp_EmployeeStatutory_SaveFlags` refuses `@EsiHigherCeiling = 1` for a non-member (50044).
- [ ] By hand: `usp_RunInput_List` returns `EsiHigherCeiling` and `EsiStayEligible`. `EsiStayEligible` is 1 only after a Locked or Paid regular run in the same April to September or October to March period with `EsiEmployer` above 0; arrears runs (RunNo 2 and up) do not count.
- [ ] Maker-checker (0310): the maker cannot approve their own run; a draft cannot skip approval unless the plan needs none (Sigma finding 4).

## D. Known-answer cases (the PLAN gate)
Workbook: `known_answer_cases_2026-27.xlsx` (17 cases: C01 to C13 payroll, C14 split month, C15 to C17 tax). Drafted from statute and public sources, hand-checked, **not yet verified by the owner or CA**. EPF ceiling in the workbook is 25,000 from 17-09-2026.
- [ ] Owner or CA verifies the expected values in the workbook (every sheet) and records the reference.
- [ ] Each case becomes a test in `Revolution.Modules.Payroll.Tests` with the workbook's expected values.
- [ ] C14 split month: Brave implements option C (a month that spans a rate change is split by days, rounded per part, summed): two `GetEpfRate(asOf)` calls. Expected in the workbook: 2,192 (against 2,640 on one row).
- [ ] C07 disabled employee passes after CR 0030 (employee 180, employer 780).
- [ ] C09 overtime: CA decides whether overtime is left out of the ESI ceiling test. If yes, a further CR gives the calculator a separate ceiling-test wage.
- [ ] CA confirms: Rs 25,000 ESI ceiling for disability (added in 0416 after the 0411 sign-off); the April and October contribution periods; employee whose wage later falls to the ceiling.
- [ ] CA confirms Rs 10 rounding for tax, and the Maharashtra February special month (one source says March).
- [ ] Sign-off migration for the rate rows still at `Verified = 0` (EPF 2026-09-17 row, PT, tax, ESI disabled ceiling), once the owner confirms the reference. CA membership number and signed copy are not needed (owner, 2026-11-13).

## E. Run the screens (the part no test covers)
- [ ] Salary structure: create, change with an effective date, history.
- [ ] Loan and EMI: schedule, recovery in a run, EMI cap at 25 percent of net pay.
- [ ] Payroll run: draft, process, submit, approve, lock, paid. A reopened run reverses recoveries.
- [ ] Dual approval: a second user approves; the same user cannot.
- [ ] Payslip view and print for one employee, rupee amounts in Indian format.
- [ ] TDS declaration: rent row with landlord PAN above 1,00,000: the three choices, and no HRA in the projection for "Do not claim".
- [ ] Employee Statutory tab: ESI higher ceiling checkbox enabled only for ESI members.
- [ ] Screenshot every screen above into `docs/reviews/phase5-gate/` with the date in the file name.

## F. Review by the other developer
- [ ] Sigma reviews services, repositories, screens, 0310 and the TDS wiring (the 2026-10-14 review covered calculators and 0300 to 0308 only).
- [ ] Sigma's findings 1, 3 and 5 from 2026-10-14 are closed.

## G. Payslip reports (decision needed)
PLAN lists payslip reports in Phase 5. ADR 0005 moved reports to ReportStudio, with layouts and PDF checks planned for Phase 9.
- [ ] Owner decides: close Phase 5 on `PayslipService` output and the on-screen payslip, with report layouts in Phase 9; or hold Phase 5 for the layouts.

## H. CI
- [ ] CI is green on `main` on windows-latest. CI does not start SQL Server, so section C is manual until Sigma adds a SQL Server service.

## Not part of this gate (blocked on a TAN)
Form 130 Part A import test, FVU validation of a Form 138 file. Both wait for the company TAN.

## Sign-off
Run by: ____ Date: ____ Verified by (owner or CA): ____ Reviewed by the other developer: ____
