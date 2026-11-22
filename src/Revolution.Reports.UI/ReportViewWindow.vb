Imports System.Drawing
Imports System.Windows.Forms
Imports ReportStudio.Core.Model
Imports ReportStudio.UI

''' <summary>
''' Shows one layout on rows a module has already built (ADR 0028): the payslip print uses it. Host-only like every report screen. The module passes the
''' parameter values it knows; only names the layout declares are used. Print, PDF, Excel and Word are on the viewer's own toolbar.
''' </summary>
Public NotInheritable Class ReportViewWindow
    Inherits BaseForm

    Private Const FormWidth As Integer = 1000
    Private Const FormHeight As Integer = 760

    Private ReadOnly _layoutFile As String
    Private ReadOnly _rows As IReadOnlyList(Of IReadOnlyDictionary(Of String, Object))
    Private ReadOnly _values As IReadOnlyDictionary(Of String, String)
    Private ReadOnly _viewer As New ViewerControl()

    Public Sub New(title As String, layoutFile As String, rows As IReadOnlyList(Of IReadOnlyDictionary(Of String, Object)), values As IReadOnlyDictionary(Of String, String))
        MyBase.New(title, "Print, PDF, Excel and Word are on the viewer's toolbar.")
        If String.IsNullOrWhiteSpace(layoutFile) Then Throw New ArgumentException("A layout file is needed.", NameOf(layoutFile))
        If rows Is Nothing Then Throw New ArgumentNullException(NameOf(rows))
        If values Is Nothing Then Throw New ArgumentNullException(NameOf(values))
        _layoutFile = layoutFile
        _rows = rows
        _values = values
        ClientSize = New Size(FormWidth, FormHeight)
        _viewer.Dock = DockStyle.Fill
        _viewer.HostOnly = True
        _viewer.RowProvider = Function(typed As IReadOnlyDictionary(Of String, Object)) CType(_rows, IEnumerable(Of IReadOnlyDictionary(Of String, Object)))
        Content.Controls.Add(_viewer)
        AddHandler Shown, Sub(sender As Object, e As EventArgs) ShowReport()
    End Sub

    Private Sub ShowReport()
        Dim problem As String = Nothing
        Dim definition As ReportDefinition = ReportLayouts.Load(_layoutFile, problem)
        If definition Is Nothing Then
            ShowError(problem)
            Return
        End If
        Dim declared As New Dictionary(Of String, String)()
        For Each parameter As ReportParameter In definition.Parameters
            Dim value As String = Nothing
            If _values.TryGetValue(parameter.Name, value) Then declared(parameter.Name) = value
        Next
        _viewer.Run(definition, declared, ReportLayouts.Folder())
    End Sub
End Class
