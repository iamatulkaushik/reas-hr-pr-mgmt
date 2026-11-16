# Phase 7b: the Monthly Pack (Sigma to Brave)

Date: 2026-11-14. Decision: ADR 0027 (read it first). Sigma delivered the shared types and the interface (`IMonthlyPackService`, `MonthlyPackLine`, `MonthlyPack`, `PackStatus`, `ComplianceKind`, `PackAmount`, `MonthlyPackSheetBuilder`). Brave builds the data, the service and its tests; Sigma then builds the screens.

## 1. Data (statutory 04xx lane for the rules, pay 03xx lane for the filing record; ordering rule of ADR 0024; errors 53650 to 53699)
**`stat.DueDateRule`**: `Kind` TINYINT (the `ComplianceKind` values), `StateID` INT NULL (null = national), `Frequency` CHAR(1) (M monthly, Q, H, Y), `DueDay` TINYINT, `MonthsAfter` TINYINT (1 = the month after the period), optional special month (`SpecialMonth`, `SpecialDueDay`, `SpecialMonthsAfter`; tax on salary for March is 30 April), `EffectiveFrom`, `EffectiveTo` NULL, `IsVerified` BIT, `SourceNote` NVARCHAR(200). A rule with `IsVerified = 0` never produces a date. Load the proposed national rules of the ADR as unverified until the CA signs them (add them to `docs/ca-signoff`). Professional tax and labour welfare keep using `stat.PtDueMonth` and `stat.LabourWelfareRule`; read them, do not copy them.

**`pay.StatutoryFiling`**: `CompanyID`, `PeriodStart` DATE (first of the month), `Kind`, `StateID` INT NULL, `Status` TINYINT (3 Generated, 4 Filed, 5 Paid), `AmountPayable` DECIMAL(14,2) (the amount when it was generated), `EmployeeCount`, `GeneratedUtc`, `GeneratedByUserID`, `FiledOn` DATE NULL, `PaidOn` DATE NULL, `ReferenceNo` NVARCHAR(40) NULL, `Remarks` NVARCHAR(200) NULL, `UpdatedUtc`. Unique on (`CompanyID`, `PeriodStart`, `Kind`, `StateID`). Row-level security policy. Composite foreign keys on `CompanyID` like the other tenant tables.

**Procedures:** `pay.usp_Filing_List` (a month), `pay.usp_Filing_Record` (upsert one line: generated, filed or paid, amount, reference, date). A closed or reopened payroll does not delete filing rows; a changed amount shows as `Changed`.

## 2. The service (`MonthlyPackService` in the Payroll module, implementing `IMonthlyPackService`)
- **The run:** the company's payroll run for the month must be Approved, Locked or Paid. Otherwise every line is `NotReady` and `MonthlyPack.PayrollMessage` says why ("The payroll of October 2026 is not approved yet.").
- **Lines** (plan and company decide, ADR 0027 point 7), amounts from the run with the exporters' own calculations so the pack can never disagree with the file: EPF = employee share + employer EPF + EPS + EDLI + admin charges (components listed in `PackAmount`); ESIC = employee + employer; professional tax = the PT deducted, one line per state; TDS = the tax deducted from salary in the month; labour welfare = the rule's employee and employer amounts, only in the months the rule falls due. A line with nothing payable (no PT deducted, no TDS) is not shown.
- **Status:** `Generated`, `Filed` or `Paid` from `pay.StatutoryFiling`; if the stored amount differs from the current amount, `Changed`; `Ready` when final but never generated; `NotReady` otherwise (with the line's `Message`).
- **Due date:** the matching verified rule for the period gives the date; none gives `DueDate = Nothing` and `DueNote = "Date not verified"` (or "Date not set" when there is no rule at all).
- **`Generate`:** EPF writes the ECR file (and the arrears ECR file when there are arrears) with `EcrFileWriter`; ESIC writes the sheet with the existing builders; professional tax, TDS and labour welfare write a summary sheet with employee code, name and amount (no PAN, Aadhaar or other id). The ids are released through the existing releaser, only for an approved or locked run, and the export is logged first (ADR 0019). The file name is `<company code> <item> <yyyy-MM>.<ext>`; an existing file is never overwritten, the new one gets (2), (3). Then `usp_Filing_Record` marks the line Generated with the amount.
- **`GenerateAll`:** every `Ready` or `Changed` line, then `MonthlyPackSheetBuilder.Build(pack)` written as `<company code> Monthly Pack <yyyy-MM>.xls` with `ITableExporter`.
- **`Mark`:** Filed or Paid with reference and date; the date is not in the future; a line that was never generated cannot be marked. One audit entry for each mark and each generation, no values.
- **Permission:** View on Statutory to see the pack; Create or Edit on Statutory to generate and mark. Plan: the line's own plan feature (ADR 0027 point 7).

## 3. Tests (Brave)
Lines by plan (Silver sees EPF and ESIC; Gold adds TDS) and by state; every status including `Changed` after a payroll reopen; due dates (a national rule, the March special, an unverified rule gives no date); the amounts equal the totals of the ECR file and the ESIC sheet of the same run (a known-answer test); generation writes the right names and never overwrites; ids are not released for an unapproved run; mark rules; database tests for the two tables (tenant isolation, EXECUTE-only access) and for the rule lookup.

## 4. Order and gate
1. Tables, rules and procedures with the database tests (CI `database` job green).
2. The service and its tests.
3. Sigma: `MonthlyPackForm`, the menu entry `compliance.monthly.pack` (first in Compliance > Monthly, plan `EpfEsi`, permission View on Statutory), README.
4. Sigma, Phase B: the company-scoped connection and the overview of all companies.
Gate: CI green on both jobs; one real client month (about 100 employees) taken from approved payroll to the three files in under a minute; the amounts equal the old application's files for the same month (Owner); screens screenshotted.

## 5. Decisions taken (Owner can change them)
Files only, no portal upload. Due dates are data and unverified ones show no date. No overwrite of an existing file. The pack lists amounts of challans and annual returns but does not make them.
