Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports ReportStudio.Core.Model
Imports ReportStudio.UI

''' <summary>
''' One report in a window (ADR 0028): the user chooses a payroll run, a month or the employee filter, presses Show, and the ReportStudio viewer
''' shows the layout on the rows from IReportDataSource. The viewer is host-only: it never opens a database or a file named in a layout.
''' Print, PDF, Excel and Word come from the viewer's own toolbar.
''' </summary>
Public NotInheritable Class ReportScreen
    Inherits BaseForm

    Private Const FormWidth As Integer = 1100
    Private Const FormHeight As Integer = 720
    Private Const DatePattern As String = "dd-MM-yyyy"
    Private Const RunComboWidth As Integer = 330
    Private Const BarPadding As Integer = 8

    Private ReadOnly _entry As ReportEntry
    Private ReadOnly _source As IReportDataSource
    Private ReadOnly _showTds As Func(Of Boolean)
    Private ReadOnly _viewer As New ViewerControl()
    Private ReadOnly _run As New ComboBox()
    Private ReadOnly _month As New DateTimePicker()
    Private ReadOnly _includeExited As New CheckBox()
    Private ReadOnly _show As New Button()
    Private ReadOnly _close As New Button()
    Private _rows As IReadOnlyList(Of IReadOnlyDictionary(Of String, Object)) = Array.Empty(Of IReadOnlyDictionary(Of String, Object))()

    Public Sub New(entry As ReportEntry, source As IReportDataSource, showTds As Func(Of Boolean))
        MyBase.New(TitleOf(entry), "Choose what to show, then press Show.")
        If entry Is Nothing Then Throw New ArgumentNullException(NameOf(entry))
        If source Is Nothing Then Throw New ArgumentNullException(NameOf(source))
        If showTds Is Nothing Then Throw New ArgumentNullException(NameOf(showTds))
        _entry = entry
        _source = source
        _showTds = showTds
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
        AddHandler Shown, Sub(sender As Object, e As EventArgs) LoadRuns()
    End Sub

    Private Shared Function TitleOf(entry As ReportEntry) As String
        Return If(entry Is Nothing, "Report", entry.Title)
    End Function

    Private Sub BuildLayout()
        _viewer.Dock = DockStyle.Fill
        _viewer.HostOnly = True
        _viewer.RowProvider = Function(typed As IReadOnlyDictionary(Of String, Object)) CType(_rows, IEnumerable(Of IReadOnlyDictionary(Of String, Object)))
        Content.Controls.Add(_viewer)

        Dim bar As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .WrapContents = False, .Padding = New Padding(BarPadding)}
        Select Case _entry.Input
            Case ReportInput.PayrollRun
                AddCaption(bar, "Payroll run")
                _run.DropDownStyle = ComboBoxStyle.DropDownList
                _run.Width = RunComboWidth
                bar.Controls.Add(_run)
            Case ReportInput.Month
                AddCaption(bar, "Month")
                _month.Format = DateTimePickerFormat.Custom
                _month.CustomFormat = "MMMM yyyy"
                _month.ShowUpDown = True
                _month.Value = Date.Today.AddMonths(-1)
                bar.Controls.Add(_month)
            Case Else
                _includeExited.Text = "Include employees who have left"
                _includeExited.AutoSize = True
                bar.Controls.Add(_includeExited)
        End Select
        _show.Text = "Show"
        _show.AutoSize = True
        _close.Text = "Close"
        _close.AutoSize = True
        _close.DialogResult = DialogResult.Cancel
        CancelButton = _close
        bar.Controls.Add(_show)
        bar.Controls.Add(_close)
        AddHandler _show.Click, Sub(sender As Object, e As EventArgs) DoShow()
        Content.Controls.Add(bar)
    End Sub

    Private Shared Sub AddCaption(bar As FlowLayoutPanel, caption As String)
        bar.Controls.Add(New Label With {.Text = caption, .AutoSize = True, .Margin = New Padding(0, 6, 6, 0)})
    End Sub

    Private Sub LoadRuns()
        If _entry.Input <> ReportInput.PayrollRun Then Return
        Try
            For Each info As ReportRunInfo In _source.ListRuns()
                _run.Items.Add(info)
            Next
        Catch ex As ReportDataException
            ShowError(ex.Message)
            Return
        End Try
        If _run.Items.Count > 0 Then
            _run.SelectedIndex = 0
        Else
            ShowStatus("There is no payroll run yet.")
        End If
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        DoShow()
    End Sub

    Private Sub DoShow()
        ClearValidation()
        Dim run As ReportRunInfo = TryCast(_run.SelectedItem, ReportRunInfo)
        If _entry.Input = ReportInput.PayrollRun AndAlso run Is Nothing Then
            SetFieldError(_run, "Choose a payroll run.")
            Return
        End If
        Dim definition As ReportDefinition = LoadDefinition()
        If definition Is Nothing Then Return
        Dim choice As New ReportChoice With {.RunId = If(run Is Nothing, 0, run.RunId), .Month = _month.Value, .IncludeExited = _includeExited.Checked}
        Try
            _rows = _source.GetRows(_entry, choice)
        Catch ex As ReportDataException
            ShowError(ex.Message)
            Return
        End Try
        If _viewer.Run(definition, ParameterValues(definition, run), ReportLayouts.Folder()) Then
            ShowStatus(If(_rows.Count = 0, "There is nothing to show for this choice.", _rows.Count.ToString(CultureInfo.InvariantCulture) & " rows."))
        End If
    End Sub

    Private Function LoadDefinition() As ReportDefinition
        Dim problem As String = Nothing
        Dim definition As ReportDefinition = ReportLayouts.Load(_entry.LayoutFile, problem)
        If definition Is Nothing Then ShowError(problem)
        Return definition
    End Function

    ''' <summary>The values the host gives the layout's parameters. Only names the layout declares are passed.</summary>
    Private Function ParameterValues(definition As ReportDefinition, run As ReportRunInfo) As Dictionary(Of String, String)
        Dim known As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        known("ShowTds") = BoolText(_showTds())
        Select Case _entry.Input
            Case ReportInput.PayrollRun
                known("CompanyName") = run.CompanyName
                known("PeriodText") = run.PeriodText
                known("IsDraft") = BoolText(run.IsDraft)
            Case ReportInput.Month
                known("CompanyName") = CompanyFromRows()
                known("MonthText") = _month.Value.ToString("MMMM yyyy", CultureInfo.InvariantCulture)
            Case Else
                known("CompanyName") = CompanyFromRows()
                known("AsOnText") = Date.Today.ToString(DatePattern, CultureInfo.InvariantCulture)
        End Select
        Dim declared As New Dictionary(Of String, String)()
        For Each parameter As ReportParameter In definition.Parameters
            Dim value As String = Nothing
            If known.TryGetValue(parameter.Name, value) Then declared(parameter.Name) = value
        Next
        Return declared
    End Function

    Private Function CompanyFromRows() As String
        If _rows.Count > 0 AndAlso _rows(0).ContainsKey("CompanyName") Then
            Return Convert.ToString(_rows(0)("CompanyName"), CultureInfo.InvariantCulture)
        End If
        Return String.Empty
    End Function

    Private Shared Function BoolText(value As Boolean) As String
        Return If(value, "true", "false")
    End Function
End Class
