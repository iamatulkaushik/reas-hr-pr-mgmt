Imports System.Windows.Forms
Imports Revolution.Modules.Arrears

''' <summary>The Arrears module's menu items for the shell (ADR 0016): the arrears list and the arrears report, each on its own form. Sigma places them in the menu layout.</summary>
Public NotInheritable Class ArrearsShellModule
    Implements IShellModule

    Private Const ProcessGroup As String = "Process"
    Private Const ReportsGroup As String = "Reports"
    Private Const ListSort As Integer = 580
    Private Const ReportSort As Integer = 790

    Private ReadOnly _items As IReadOnlyList(Of ShellItem)

    Public Sub New(services As ArrearsServices)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _items = New List(Of ShellItem) From {
            New ShellItem(New MenuEntry("arrears.list", ProcessGroup, "Arrears", ModuleKey.Payroll, PlanFeature.BasicPayroll, ListSort),
                          Function() CType(New ArrearsListForm(services), Form)),
            New ShellItem(New MenuEntry("arrears.report", ReportsGroup, "Arrears report", ModuleKey.Payroll, PlanFeature.BasicPayroll, ReportSort),
                          Function() CType(New ArrearsReportForm(services), Form))}
    End Sub

    Public ReadOnly Property Items As IReadOnlyList(Of ShellItem) Implements IShellModule.Items
        Get
            Return _items
        End Get
    End Property
End Class
