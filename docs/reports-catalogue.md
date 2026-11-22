# Report catalogue (first release)

Layouts are Host reports in `reports/layouts`. Rows come from the procedure named; the screen fills the report parameters. A4 landscape unless stated. All group by unit (factory), with unit and grand totals, page numbers, generated date.

| Report | Menu | Layout | Procedure | Choose | For | Columns |
|---|---|---|---|---|---|---|
| Basic Report | Reports | basic-report.rsr | rpt.usp_EmployeeBasic_Get | include left employees | Associate, Operator, Owner | code, name, father/spouse, sex, born, joined, left on, designation, department, mobile |
| Salary Sheet | Reports | salary-sheet.rsr | rpt.usp_WageRegister_Get | payroll run | Associate, Owner (cash or bank payment sheet) | code, name, designation, days, gross, EPF, ESI, prof. tax, income tax (plan with TDS), loan, other, total deductions, net pay, signature box; net pay in words |
| Wage Register (Form IV equivalent) | Reports | wage-register.rsr | rpt.usp_WageRegister_Get | payroll run | Associate | all amounts of the run incl. employer shares (Brave's layout) |
| Abstract | Reports | payroll-summary.rsr | rpt.usp_PayrollSummary_Get | payroll run | Associate, Owner | one line per unit; EPF, ESI payable; net pay in words (Brave's layout) |
| Muster Roll (Form IX equivalent) | Reports | muster-roll.rsr | rpt.usp_Muster_Get | month | Associate, Operator | code, name, 31 blank day cells, present, half, WO, holiday, PL, absent, OT hours, remarks |
| Payslip | Reports > Detailed | payslip.rsr (A4 portrait) | rpt.usp_Payslip_Get | run, employee | Associate, Owner | screen is the list today; Print... on the payslip screen opens it in the viewer (PDF for the whole run from the viewer's toolbar) |

Rules: DRAFT mark until the run is Approved; income tax column only on plans with TDS; no form or section numbers printed; ids masked unless a reveal is logged.
Menu items still planned (no layout yet): Bonus, Career History, Loans and Advances, F&F, Assets and Recoveries, Form 130/168, CA reports (Disbursement Annual, Statutory EPF/ESI, Loans and Advances).
