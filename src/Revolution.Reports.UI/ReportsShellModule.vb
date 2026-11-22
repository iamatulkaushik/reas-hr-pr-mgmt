Imports System.Windows.Forms

''' <summary>The report screens for the shell (ADR 0016, ADR 0028). MenuLayoutModule places each one under Reports.</summary>
Public NotInheritable Class ReportsShellModule
    Implements IShellModule

    Private Const ReportsGroup As String = "Reports"
    Private Const SortOrder As Integer = 900

    Private ReadOnly _items As IReadOnlyList(Of ShellItem)

    Public Sub New(source As IReportDataSource, showTds As Func(Of Boolean))
        If source Is Nothing Then Throw New ArgumentNullException(NameOf(source))
        If showTds Is Nothing Then Throw New ArgumentNullException(NameOf(showTds))
        Dim list As New List(Of ShellItem)()
        For Each entry As ReportEntry In ReportCatalog.All
            list.Add(CreateItem(entry, source, showTds))
        Next
        _items = list
    End Sub

    Public ReadOnly Property Items As IReadOnlyList(Of ShellItem) Implements IShellModule.Items
        Get
            Return _items
        End Get
    End Property

    ''' <summary>The key of the screen of a report. MenuLayoutModule uses it to place the screen.</summary>
    Public Shared Function ScreenKey(entry As ReportEntry) As String
        If entry Is Nothing Then Throw New ArgumentNullException(NameOf(entry))
        Return "reports.screen." & entry.Key
    End Function

    Private Shared Function CreateItem(entry As ReportEntry, source As IReportDataSource, showTds As Func(Of Boolean)) As ShellItem
        Dim menu As New MenuEntry(ScreenKey(entry), ReportsGroup, entry.Title, ModuleKey.Reports, Nothing, SortOrder)
        Return New ShellItem(menu, Function() CType(New ReportScreen(entry, source, showTds), Form))
    End Function
End Class
