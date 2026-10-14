# ADR 0015: How a module plugs in, and what Sigma needs to see

Status: accepted. Owner: Sigma. Date: 2026-10-14. Follows ADR 0008.

## Why
Brave reports Phase 5 loans, installments, recovery, overtime, payslip and gratuity as done and asks for "the Data project and AppServices". That code is not on `origin/main`, so nothing can be wired or reviewed yet. This ADR fixes the shape so that, once it is pushed, wiring is small and needs no further change to shared code.

## What Brave does not need from Sigma
- **No change to Revolution.Data.** Under ADR 0008 a module keeps its own `Sql<n>Repository` and interface inside the module project, using the public `IConnectionFactory` and `StoredProcedure` helpers. The module project needs a `ProjectReference` to `Revolution.Data` (Employees and Attendance already have one; Payroll has only Core today). That edit is Brave's own.

## What Sigma now provides
- **`ModuleHost`** (Revolution.Data): the shared services a module is built from: connection factory, session, entitlements, permissions, audit, approvals, statutory rates, field crypto. `AppServices.Host` builds it once.
- **Module composition class.** Each module writes one public class that takes the host and returns its own services, for example:

```
Public NotInheritable Class PayrollModule
    Public Shared Function Compose(host As ModuleHost) As PayrollServices
        Dim loans As New LoanService(New SqlLoanRepository(host.Factory), host.Session, host.Permissions, host.Entitlements, host.Audit)
        ...
        Return New PayrollServices(loans, ...)
    End Function
End Class
```

  Revolution.App adds a `ProjectReference` to the module and one line `PayrollModule.Compose(services.Host)`. Sigma owns that App edit and does it when the module is pushed.
- Rules the composition must follow: services ask `host.Permissions` and `host.Entitlements`, never a role or plan name; statutory figures come only from `host.Rates`; protected ids go through `host.Crypto`; payroll runs go through `host.Approvals` with entity name `PayrollRun` (ADR 0012).

## Error numbers (so modules never clash)
Used today: 50001 to 50005 and 50030 to 50040 (security, attendance, provisioning), 50020 and 50021 (roster), 51002 to 51007 (employees). From now: **payroll 53000 to 53999, statutory and tax 54000 to 54999, biometric 55000 to 55999, assets and expenses 56000 to 56999.** Each module keeps a map from number to a message the screen can show (as `AttendanceErrorMap` does).

## What to send so Sigma can wire and review (push the branch, or upload the changed files)
1. Migrations in lane 0300 to 0399: every table with a `CompanyID` joins a tenant security policy in its own migration (guard 6); money `DECIMAL(14,2)`, rates `DECIMAL(7,4)`, hours and days `DECIMAL(7,2)`; stored procedures only; a status-history table for payroll runs on every plan (ADR 0012).
2. The module project, with its repository interfaces, `Sql...Repository` classes, services and constructors, and its tests.
3. A change request only for something that must change in shared code, written from `docs/change-requests/TEMPLATE.md`.
4. For each of loans, installments, recovery, overtime, payslip and gratuity: the permission module and action it checks, the plan feature it needs (existing `PlanFeature` values, ADR 0012), and which `stat.*` rows it reads (gratuity uses `stat.GratuityRule`; leave encashment caps are in the pending statutory batch CR).

## Not decided here
The module registration contract and the shell menu (how a module adds a menu item, and the switch-company item) is the next Sigma task. Until it exists, Brave opens screens from a test harness.
