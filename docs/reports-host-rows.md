# Payroll report layouts and the rows they run on

Written 2026-11-18 by Brave. ReportStudio (engine, viewer, Host provider) is Sigma's; the three layouts below and the code that builds their rows are Brave's. Nothing here has been run in ReportStudio or on Windows: the layouts are checked by `PayrollLayoutFilesTests` and by a rough preview drawn outside ReportStudio.

All three layouts are `Host` reports: no query and no connection string. The app gives ReportStudio the rows. Money prints with `N2` (Indian grouping), dates as `dd-MM-yyyy`, net pay in words with `AmountInWords`. No layout prints a form number or a section number.

## payslip.rsr (A4 portrait, one payslip per employee, each starts a new page, page numbers restart)

Rows: `PayslipReportRows.Build(payslips)`, from the `Payslip` list that `PayslipService.GetPayslips` returns. One row per earning line, with the employee's own columns repeated, grouped by `EmployeeID`.
- Overtime is added as the last earning line when above zero.
- An employee with no earning line and no overtime gets one line, "No earnings this month".
- `Uan`, `EsiIpNumber` and `BankAccountNumber` are whatever the payslip carries: full ids only when `includeFullIds` was asked for an approved run (and logged), masked otherwise. The layout never sees `*Enc`, `*Last4` or key columns.
- Parameter `ShowTds` (Boolean): `PayslipReportRows.ShowTds(payslips)`. The TDS line is hidden when the plan has no TDS; its row stays blank, so the line below does not move.
- The DRAFT mark shows when `RunStatusID` is below 4 (Approved).
- Notes print only when `LoanEmiDeferred` or `CarryForwardOut` is above zero.

## wage-register.rsr (A4 landscape, grouped by factory)

Rows: the first result set of `rpt.usp_WageRegister_Get` as it comes, one row per employee.
Parameters: `CompanyName`, `PeriodText` (for example "01-10-2026 to 31-10-2026"), `IsDraft`, `ShowTds`. They come from the second result set and the plan, not from rows, because ReportStudio v1 has no sub-report and the page header cannot read a group field.
Factory totals and a grand total sum the columns on screen. Employee count at the end.

## payroll-summary.rsr (A4 landscape, one line per factory)

Rows: the first result set of `rpt.usp_PayrollSummary_Get` (one row per factory, with the employer shares).
Parameters as for the wage register. The footer sums the lines and shows: EPF payable (employee, employer, EDLI, admin), ESI payable (employee, employer), net pay to employees, and the net pay in words.

## Not built yet (needs a decision on who writes it)
- A repository and service for the wage register and the summary (permission View on Payroll, audit entry, entitlement check) like `PayslipService`. Today only the payslip has one. Brave can write them; the menu entries and the print dialog are Sigma's.
- A screen that calls the viewer with the rows and parameters.
- Statutory file layouts (ECR, ESIC monthly) are text files, not layouts: unchanged.

## For the Phase 9 checks on real data
- Commit golden images for the three layouts after the first real run.
- Print the payslip with the viewer, save it as PDF, and compare with the preview at A4.
- Run 50 payslips in one print: the page numbers must restart for each employee.
- Check a long employee name and a long bank name in the payslip header (the layout wraps; nothing grows the band).
- Known ReportStudio note: `fonts/Ubuntu-BoldItaclic.ttf` should be `Ubuntu-BoldItalic.ttf`. `FontCatalog` reads file names for family and style. The layouts use no bold italic, so nothing here depends on it.
