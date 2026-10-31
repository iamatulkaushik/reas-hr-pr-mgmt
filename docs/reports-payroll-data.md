# Payroll report data (Brave, lane 0700)

For Sigma and the Crystal helper (ADR 0005, ADR 0012). The app runs the procedure on its own tenant-safe connection and hands Crystal a typed DataSet file. Every first result set carries `RunStatusID`. Below 4 (Approved) the report prints a DRAFT mark.

| Procedure | Result sets | Use |
|---|---|---|
| `rpt.usp_Payslip_Get @RunID, @EmployeeID = NULL` | 1 employee rows, 2 earnings per employee | Payslip, one or all |
| `rpt.usp_WageRegister_Get @RunID, @FactoryID = NULL` | 1 row per employee, all amounts | Wage register by factory |
| `rpt.usp_PayrollSummary_Get @RunID` | 1 totals per factory, 2 run total | Summary and cover sheet |

## Payslip, result set 1
RunID, RunStatusID, PeriodFrom, PeriodTo, CompanyName, CompanyAddress1, CompanyAddress2, FactoryName, EpfSubCode, EsiSubCode, EmployeeID, EmpCode, FullName, FatherOrSpouseName, JoinDate, PfNumber, UanLast4, UanEnc, EsiIpLast4, EsiIpEnc, StatutoryKeyVersion, BankName, Ifsc, AccountLast4, AccountEnc, BankKeyVersion, DesignationName, DepartmentName, PaidDays, OvertimeHours, OvertimeAmount, GrossEarnings, EpfEmployee, EsiEmployee, ProfessionalTax, TdsAmount, LoanEmi, OtherDeductions, CarryForwardIn, TotalDeductions, NetPay, LoanEmiDeferred, CarryForwardOut.

## Payslip, result set 2
EmployeeID, ComponentCode, ComponentName, SortOrder, Amount. Overtime is not a component. It has its own column, `OvertimeAmount`, in result set 1.

## Notes
- UAN, ESI number and bank account come back as stored: `*Enc` ciphertext, `*Last4`, and key versions. The Owner wants them on the payslip. The application decrypts with `IFieldCrypto`, writes one reveal log entry per print job (`aud.usp_Reveal_Insert`, fields named, rows counted), and passes plain text only in the temporary DataSet file, deleted after printing. Never log plain or cipher values.
- Money is whole rupees in `DECIMAL(14,2)`. Dates are `DATE`.
- Income tax (CR 0019, migration 0701): `TdsAmount` is in payslip result set 1 and the wage register, and summed in the run summary. It is 0 when the plan has no `Tds` feature or the line was written before 0323.
  The data never hides it; the app passes the report parameter `ShowTds` (true when the plan has `Tds`) and the layout shows the deduction line "Income tax (TDS)" only when it is true.
  The label is that plain text. A section number is printed only from a verified `IStatutoryRateService.GetTaxSectionRef("SALARY_TDS", year)` row, never an unverified one.
- Procedures only read. Error 50311 means the run is not in this company.
- Needs schema `rpt` from Sigma's 0014.
