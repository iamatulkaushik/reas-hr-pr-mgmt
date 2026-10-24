Imports System.Windows.Forms
Imports Revolution.Modules.Payroll

''' <summary>The Payroll module's menu items for the shell (ADR 0016): the three statutory and bank file exports (ADR 0019).</summary>
Public NotInheritable Class PayrollShellModule
    Implements IShellModule

    Private Const PayrollGroup As String = "Payroll"
    Private Const EcrSort As Integer = 400
    Private Const EsicSort As Integer = 410
    Private Const BankSort As Integer = 420

    Private ReadOnly _items As IReadOnlyList(Of ShellItem)

    Public Sub New(services As PayrollServices, permissions As IPermissionService)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        _items = New List(Of ShellItem) From {
            New ShellItem(New MenuEntry("payroll.export.ecr", PayrollGroup, "ECR file (EPFO)", ModuleKey.Statutory, PlanFeature.EpfEsi, EcrSort),
                          Function() CType(New PayrollExportForm(services, PayrollFileKind.Ecr, CanExport(permissions, ModuleKey.Statutory)), Form)),
            New ShellItem(New MenuEntry("payroll.export.esic", PayrollGroup, "ESIC upload sheet", ModuleKey.Statutory, PlanFeature.EpfEsi, EsicSort),
                          Function() CType(New PayrollExportForm(services, PayrollFileKind.Esic, CanExport(permissions, ModuleKey.Statutory)), Form)),
            New ShellItem(New MenuEntry("payroll.export.bank", PayrollGroup, "Bank file", ModuleKey.Payroll, PlanFeature.BankFiles, BankSort),
                          Function() CType(New PayrollExportForm(services, PayrollFileKind.BankFile, CanExport(permissions, ModuleKey.Payroll)), Form))}
    End Sub

    ''' <summary>Only enables or disables the button. The service checks Export again.</summary>
    Private Shared Function CanExport(permissions As IPermissionService, moduleKey As ModuleKey) As Boolean
        Return permissions.IsAllowed(moduleKey, PermissionAction.Export)
    End Function

    Public ReadOnly Property Items As IReadOnlyList(Of ShellItem) Implements IShellModule.Items
        Get
            Return _items
        End Get
    End Property
End Class
