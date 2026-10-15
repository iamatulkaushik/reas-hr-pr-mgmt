# Change request 0016: Bulk id decrypt for statutory and bank file export

- Requested by: Brave
- Needed by module / phase: Phase 7 (ECR, ESIC sheet, bank files)
- What is missing or wrong in the shared code: `IPayrollFileSource` must return UAN, ESI IP number and bank account in clear for every employee of a run. Today `EmployeeIdentifierService.Reveal` and the bank reveal work one employee at a time, need the Reveal permission, and write one reveal log row per call. Exporting 500 employees would make 500 calls and needs Reveal on the exporting role. The Owner and Associate have Export on Payroll and Statutory; Reveal is a separate grant.
- Proposed signature or schema change: Sigma decides the rule. Brave proposes: (1) Export on Statutory or Payroll allows the file export to decrypt the ids it needs, without a separate Reveal grant. (2) One reveal log row per employee per export, fields UAN, ESI IP, bank account as needed, reason text "ECR export", "ESIC export" or "Bank file". (3) A bulk method on the Employees module, for example `EmployeeIdentifierService.RevealForExport(employeeIds, fields, reason) As OperationResult(Of IReadOnlyDictionary(Of Integer, RevealedIds))`, same for the bank account. Plain values stay inside the call and the returned objects; nothing is logged.
- Workaround until done: `PayrollFileService` depends only on `IPayrollFileSource`. Tests use a fake. No real implementation exists yet.
- Note for whoever wires it: `Revolution.Modules.Employees` already has a `PaymentMode` type. The Payroll module has `PaymentMode` too (Neft, Rtgs). Use the full namespace where both are imported.
- Sigma's decision (accepted / changed / rejected) and date: pending
