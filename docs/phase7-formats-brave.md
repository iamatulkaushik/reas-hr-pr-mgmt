# Phase 7 file formats (Brave)

Date: 2026-10-15. Status: ESIC and SBI from Owner files (ESI_Format.xlsx, two SBI format screenshots). ECR from public EPFO help. Owner ECR file still awaited.
Rule: layouts live in a format table, not in code. Each file writer reads one format row.

## 1. EPFO ECR (monthly, .txt)
11 fields, `#~#` delimiter, no header, one line per member, whole rupees, name as per UAN.
Order: UAN, name, gross, EPF wages, EPS wages, EDLI wages, EPF employee, EPS contribution, EPF-EPS employer difference, NCP days, refund of advances.
Arrear ECR: 8 fields. Not built yet.
Checks: UAN 12 digits; EPF not above gross; EPS not above EPF; EPS and EDLI not above ceiling (ceiling from rate row).
Code: `EcrMember`, `EcrValidator`, `EcrFileWriter`.

## 2. ESIC monthly contribution (Owner template)
File type: Excel 97-2003 `.xls`. Every column Text. Six columns, header text as in template.

| # | Column | Rule |
|---|---|---|
| A | IP Number | 10 digits |
| B | IP Name | Alphabets and space only |
| C | No of days wages paid/payable | Whole number. Fractions round up |
| D | Total monthly wages | Whole rupees |
| E | Reason code | 0 for all paid rows. 0 to 13 |
| F | Last working day | dd/mm/yyyy or dd-mm-yyyy. Zero-padded |

Reason codes: 0 Without Reason, 1 On Leave, 2 Left Service, 3 Retired, 4 Out of Coverage, 5 Expired, 6 Non Implemented area, 7 Compliance by Immediate Employer, 8 Suspension of work, 9 Strike/Lockout, 10 Retrenchment, 11 No Work, 12 Does not belong to this employer, 13 Duplicate IP.

Rules:
1. Last working day needed for codes 2, 3, 4, 5, 6, 10. Blank for all others.
2. Last working day only if days = 0. Part-month leaver with 1+ day: no date.
3. Code 4 valid only for April or October wage period.
4. Codes 2, 3, 4, 5, 10 remove the IP from later months.
5. All mapped employees must be in the sheet.
6. Portal calculates IP and employer contribution.

Earlier draft was wrong: codes were off by one, and the Out of Coverage rule was missing. Fixed.
Code: `EsicRow`, `EsicValidator`, `EsicSheetBuilder`.

## 3. SBI files (Owner screenshots)
### 3a. Composite beneficiary file, 9 fields
| # | Field | Max | Rule |
|---|---|---|---|
| 1 | Beneficiary type * | 1 | S same bank, O other bank |
| 2 | Action type * | 1 | A add, D delete |
| 3 | Name * | 35 | Alphanumeric |
| 4 | Account no. * | 33 | Same bank numeric. Other bank alphanumeric |
| 5 | Beneficiary code | 20 | Optional |
| 6 | IFSC code * | 11 | Same bank: branch code per sample |
| 7-9 | Address 1-3 | 35 | Optional |

Samples: `S|A|name|account||branch code|||` and `O|A|name|account||IFSC code|||`.

### 3b. Transaction file, 8 fields
| # | Field | Max | Rule |
|---|---|---|---|
| 1 | Debit or credit account | 17 | Digits |
| 2 | Branch code or IFSC | 5 / 11 | Debit and same bank: 5 digits. Other bank: IFSC |
| 3 | Date | 10 | DD/MM/YYYY |
| 4 | Debit amount | 23 | 1.00 style. Debit line only |
| 5 | Credit amount | 23 | Credit lines only |
| 6 | Reference no. | 15 | Unique |
| 7 | Description | 240 | Alphanumeric |
| 8 | Payment identifier | 11 | NEFT, RTGS or blank |

Layout: one debit line (total), then one credit line per payee. Same bank: field 8 blank. Other bank: NEFT or RTGS.

### 3c. Open points for Owner
1. Delimiter. Sheet header says `|` for both files. Beneficiary samples use `|`. Transaction samples use `#`. Built with `|` default, changeable in `SbiFileOptions`. Confirm.
2. Beneficiary header shows more delimiter choices. Default `|` used.
3. Same-bank credit branch code: 5 digits as in sheet.
4. RTGS minimum amount rule not built.
5. Description: one text for whole batch.
Code: `BankBatch`, `BankPayee`, `IBankFileWriter`, `SbiFileWriter`, `SbiValidator`, `SbiFileOptions`.

## 4. Shared changes
| Item | Needs |
|---|---|
| ECR, ESIC, SBI builders | Module code only |
| .xls export | CR 0014. Services exporter, `.xls` not `.xlsx` |
| Format table | CR later: rate-style rows for delimiters and ESIC codes |

## 5. Mapping from run lines
Module code is pure. The app loads run lines, decrypts UAN, ESI number and account, and fills `PayrollFileEmployee`. Mappers return items plus a problem list.

| Target | Source |
|---|---|
| ECR gross | GrossEarnings, whole rupees |
| ECR EPF wages | EpfWage |
| ECR EPS and EDLI wages | EpfWage capped at ceiling (rate row). Zero if no EPS or EDLI amount |
| ECR EPF, EPS, difference | EpfEmployee, EpfEmployerEps, EpfEmployerEpf |
| ECR NCP days | Base days less PaidDays rounded up |
| ESIC days, wages | PaidDays rounded up, EsiWage |
| ESIC reason, last day | Employee exit record. Empty reason means 0 |
| Bank amount | NetPay |
| Bank same or other | IFSC starts with SBIN |
| Bank branch code | Last 5 characters of an SBIN IFSC |
| Bank reference | Batch reference + 4 digit serial. Batch reference at most 11 characters |

Left out with a problem line: EPF member without UAN, ESI member without IP number, bank employee without account or IFSC. Cash and zero-net employees are skipped silently.

Owner decisions (2026-10-15):
1. Name as per UAN is the employee name. Done.
2. NCP days use calendar days of the wage month. Done.
3. Company bank IFSC field holds the branch code. Used directly as debit branch code. A full 11 character IFSC is also accepted (last 5 characters). Employee IFSC stays a full IFSC; same-bank branch code is its last 5 characters.
4. ESIC Excel: the portal reads sheet 1 only. Sheet 1 "Sheet1" is the upload sheet, six columns as the template. Sheet 2 "Instructions & Reason Codes" keeps the template reason code table and fill-in instructions only. No employee data on sheet 2. Template steps 10a and 10b (convert to Text by hand) are left out because the exporter writes Text.
5. Per member option: EPF on higher wage, and EPS (pension) on higher wage. EPS on higher wage needs EPF on higher wage. EPS wages in ECR then pass the ceiling. EDLI wages stay capped. `EpfCalculator.Calculate` has a new optional argument `pensionOnActualWage`.

Still open:
1. Employee option store: `HigherWageOption` exists as one flag. Two flags needed (EPF higher, EPS higher). Change request to Sigma for the employee module.
2. ESIC exit reason code has no source yet.
3. Arrear ECR not built.
4. PENDING (decide on runtime testing): ESIC detail information. Built as `EsicDetailSheetBuilder`, kept out of the upload workbook. Options: third sheet, separate file, or drop. Nothing calls it yet.

## 6. File service
`PayrollFileService` builds the files for a run. Needs Export on Statutory (ECR, ESIC) or Payroll (bank). Run must be Approved, Locked or Paid. Wage month is the month of the period end date. One audit entry per file, with line counts only. If the audit write fails, no file is released.
`IPayrollFileSource` loads the run with ids decrypted. No implementation yet: CR 0016.
