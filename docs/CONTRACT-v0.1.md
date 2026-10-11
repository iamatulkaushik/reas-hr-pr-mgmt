# Contract v0.1 (Phase 1 freeze)

For Brave. These are stable. A change goes through a change request (`docs/change-requests/TEMPLATE.md`); Sigma answers within one working day.

## Core (`Revolution.Core`)
- `OperationResult` / `OperationResult(Of T)`: services return these. No MessageBox in Services or Data.
- `UserRole`, `PlanTier`, `PlanFeature`, `ModuleKey`, `PermissionAction`.
- `ISessionContext.Current` gives the `UserSession` (UserId, UserName, Role, CompanyId, Plan).
- `IPermissionService.IsAllowed(module, action)`: role questions. Never compare a role name.
- `IEntitlementService.IsEnabled(feature)`: plan questions. Never compare a plan name.
- `IAuditService.Record(New AuditEntry(module, action, entityName, entityId, details))`: call after every business change. Kept only on plans with `FullAuditTrail`; security events always kept.
- `IAuthService`: used by the login screens; modules do not call it.

## Services (`Revolution.Services`)
`SessionContext`, `AuthService`, `PermissionService`, `EntitlementService`, `AuditService`, `ProvisioningService`, `PasswordHasher`, `PasswordPolicy`. Modules receive the interfaces, not these classes.

## UI (`Revolution.UI.Common`)
Every form inherits `BaseForm`:
```vb
Public NotInheritable Class EmployeeListForm
    Inherits BaseForm

    Public Sub New(permissions As IPermissionService)
        MyBase.New("Employees")
        ' Add controls to Content. Toolbar items go in Toolbar.Items.
        ' Ctrl+S, F5 and F1 call OnSaveRequested, OnRefreshRequested and OnHelpKeyPressed.
    End Sub
End Class
```
- No colours, fonts or sizes in forms: `Theme` and `ThemeStyler` own them. Spacing is multiples of `Theme.Space1` (8 px).
- Long work: `Await RunBusyAsync(Function() Task.Run(Sub() ...))`.
- Errors: `SetFieldError(control, message)` for one field, `ShowError(message)` for a summary, `ClearValidation()` first.
- Set `AcceptButton` and `CancelButton`; data entry is keyboard-first.

## Not frozen yet
Employee and company models, master-data repositories, `IPunchSource` (Phase 8). Ask by change request when a module needs them.

## Running it
1. Create an empty database named `HRMS`.
2. `set HRMS_SETUP_CONNECTION=<admin connection string>` then `RevolutionSetup migrate`.
3. `RevolutionSetup add-company "Acme" Gold`, then `RevolutionSetup add-user dev Developer` and `RevolutionSetup add-user owner1 Owner 1`.
4. Create a SQL login in role `hrms_app`, copy `src/Revolution.App/appsettings.example.json` to `appsettings.json` and edit it.
