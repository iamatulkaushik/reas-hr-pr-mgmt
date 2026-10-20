# ADR 0016: How a module adds menu items, and how the shell switches company

Status: accepted. Owner: Sigma. Date: 2026-10-14. Closes the open item at the end of ADR 0015.

## Decision
- A module describes its menu with `MenuEntry` objects (Revolution.Core): a unique key such as `payroll.runs`, a top-level group such as `Payroll`, a title, the `ModuleKey` the user needs View on, an optional `PlanFeature`, and a sort order.
- The module wraps each entry with the code that opens its screen in a `ShellItem` (Revolution.UI.Common) and returns them from one class that implements `IShellModule`. The screen factory returns a new `BaseForm`-derived form, not yet shown.
- The shell asks `IPermissionService` and `IEntitlementService` through `MenuFilter`. Entries the user may not open, or whose plan feature is off, are not shown. The shell checks View again when an entry is clicked. Nobody checks a role or plan name (ADR 0003).
- Two entries with the same key stop the app at start-up with a clear message. Keys are unique across all modules.
- Adding a module to the app is one line in `Program.vb` (Sigma does it when the module is pushed and reviewed): `modules.Add(New PayrollShellModule(PayrollModule.Compose(services.Host)))`.

## Switch company
- The File menu shows "Switch company..." when `IAuthService.ListCompanies` succeeds with more than one company. The shell does not look at the role.
- Choosing it closes every screen opened from the menu (after a warning if any are open), then shows the company picker. When a company is chosen, the session, plan and menus are rebuilt. If a screen refuses to close, the company is not changed.
- Why: row-level security follows the session company, so a screen left open would otherwise keep showing the old company's data under the new company's name.

## For Brave
```
Public NotInheritable Class PayrollShellModule
    Implements IShellModule

    Private ReadOnly _items As IReadOnlyList(Of ShellItem)

    Public Sub New(services As PayrollServices)
        _items = New List(Of ShellItem) From {
            New ShellItem(New MenuEntry("payroll.runs", "Payroll", "Payroll runs", ModuleKey.Payroll, PlanFeature.BasicPayroll, 10),
                          Function() New PayrollRunForm(services))
        }
    End Sub

    Public ReadOnly Property Items As IReadOnlyList(Of ShellItem) Implements IShellModule.Items
        Get
            Return _items
        End Get
    End Property
End Class
```
Use the existing `PlanFeature` and `ModuleKey` values. A new one needs a change request.

## Not done here
No MDI, no tabs, no toolbar buttons, no recent-items list: screens open as normal windows owned by the shell. Revisit if the pilot month shows a need.

## Status of testing
`MenuFilter` has unit tests. The menu builder and the shell were written without a compiler or a Windows machine: build and run them before relying on them.

## Amendment (Oct 18 2026): menu layout
- A menu group is a path: `Master|Company|Factory / Units` opens nested submenus.
- Feature modules offer screens under their own keys. `MenuLayoutModule` (Revolution.App, Sigma) decides where each screen sits and lists agreed positions whose screens do not exist yet as planned items, shown disabled. Only `MenuLayoutModule` and the Licence module are registered with the shell.
- File menu: Select Company, Financial Year (planned), Sign Out, Exit. Sign Out closes the open screens and returns to the login.
