# Change request 0017: Export hardening (review of the Payroll export, 2026-10-24)

- Requested by: Sigma (review of `EmployeeExportService`, `PayrollFileSource`, `PayrollFileService`, `PayrollExportForm`)
- Needed by module / phase: Phase 7 (ECR, ESIC, bank files); item 2 also Phase 5
- What is missing or wrong in the shared code: see the six findings below. Nothing is broken today; items 1 and 2 must be done before a pilot month.
- Workaround until done: none needed for development data.

## Findings and what each side does
| # | Finding | Fix | Who |
|---|---|---|---|
| 1 | `RevealForExport` takes any employee ids; ADR 0019 rule 2 (only the employees of an approved run) is checked only by the caller. | **Sigma delivered** migration 0026: `aud.usp_Reveal_InsertBatchForRun @UserID, @RunID, @Reason, @Fields, @EmployeeIdsJson`. It refuses a run that is missing or not Approved, Locked or Paid, and any id that is not a line of the run. Brave: add `runId` to `RevealForExport`, `IExportIdReleaser.Release` and `IEmployeeRepository.LogRevealBatch`, call the new procedure, map errors 50052, 50053 and 50054 to the existing "could not be logged" message. Pass reason `ECR export`, `ESIC export` or `Bank file` (40 characters at most; the procedure adds the run number). Sigma removes the old procedure in a later migration once nothing calls it. | Sigma done, Brave next |
| 2 | The ECR ceiling is passed in by the screen from one rate at the period end. September 2026 is a split month (15,000 for 1 to 16 September, 25,000 from 17 September, one ECR). | `PayrollFileService` reads the rate itself; the screen no longer passes a number. For a period that spans a rate change, the run service (Phase 5) must compute EPF wages and contributions per part and the ECR mapper must use the ceiling that applied to each part. **Sigma delivered** `EpfPeriodSplitter.Split(periodFrom, periodTo, AddressOf rates.GetEpfRate)` in Core: it returns the parts (`EpfPeriodPart`: dates, days, rate row) oldest first, or the rate service's own failure. Tests: `EpfPeriodSplitterTests`. No interface change. Prorate each part by its days. | Brave, Sigma helper done |
| 3 | ESIC reason code is always empty, so a leaver is sent with code 0. | Add an exit-reason source on the employee exit record (CR to Sigma if a column is needed) and map it to `EsicReasonCodes`. | Brave |
| 4 | The audit entry is written before the file is saved and does not carry the file name (ADR 0019 rule 6). | Return the file name from the service and record the audit entry after a successful save, or record a second entry on a failed save. Never the contents. | Brave |
| 5 | If the second bank file fails to save after replacing an old first file, the pair is mismatched. | Write both to temporary names in the chosen folder, then rename both; remove the temporary files on failure. | Brave |
| 6 | `_bank.SelectedIndex = 0` throws when no bank writer is registered. | Show a message and disable the screen. | Brave |

## Owner's answers (2026-10-24) that affect Brave
- Statutory registrations are held per company, with at most 5 units per company. Exports are per company; PT and LWF follow the unit's state.
- SBI first. Another bank is added as another `IBankFileWriter`; the export screen must need no change beyond listing the writer.
- Higher-wage pension option and disability (ESI ceiling 25,000) are per employee, on the employee's request: both are employee flags, not company settings.
- Policies (pay cycle, working days, overtime, leave, bonus, gratuity, loans) are set per company in the Policies tab.

## Income-tax Act, 2025 names (from 1 April 2026)
Salary TDS is section 392; the annual certificate is Form 130 (was Form 16); the quarterly return is Form 138 (was Form 24Q). Screens, report titles and file names for tax year 2026-27 use the new names. The CA supplies the section codes for `stat.TdsDeductionLimit` under the 2025 Act.

## Sigma's decision and date
Accepted, 2026-10-24. Item 1 delivered (migration 0026, not yet run against a database: written without a compiler or SQL Server). Tests for the new procedure need a database; they go in the Phase 2 matrix run on Windows CI.

## Added 2026-10-24: Haryana gazette divisors (Owner: "go as per gazette")
The gazette divides the monthly wage by **26** for the per-day wage and by **30** for a deduction (absence). `pay.CompanyPolicy` and `PolicySettings` have one `DailyWageDivisor`. Sigma adds a second column for the deduction divisor in a new migration (default 30, per company) once Brave confirms the name below; Brave then uses the 26 divisor for per-day wage and the 30 divisor for absence deductions, and keeps both as company policy values (the Owner sets them in the Policies tab). Brave: reply here with the property name you want (suggest `AbsenceDeductionDivisor`).
