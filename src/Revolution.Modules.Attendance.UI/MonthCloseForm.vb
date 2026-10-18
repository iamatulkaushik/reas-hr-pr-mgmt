Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>Close or reopen one month. Shows the employees that block the close, and the close and reopen history.</summary>
Friend NotInheritable Class MonthCloseForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 900
    Private Const FormHeight As Integer = 580
    Private Const MonthFormat As String = "MM-yyyy"
    Private Const LocalTimeFormat As String = "dd-MM-yyyy HH:mm"
    Private Const ReasonLabel As String = "Reason for reopening (saved with the month)"

    Private ReadOnly _services As AttendanceServices
    Private ReadOnly _canClose As Boolean
    Private ReadOnly _canReopen As Boolean
    Private ReadOnly _month As New DateTimePicker()
    Private ReadOnly _state As New Label()
    Private ReadOnly _blockers As New ListBox()
    Private ReadOnly _history As New ListView()
    Private ReadOnly _closeButton As New Button()
    Private ReadOnly _reopenButton As New Button()
    Private ReadOnly _exitButton As New Button()
    Private _sheet As CloseSheet

    Public Sub New(services As AttendanceServices, canClose As Boolean, canReopen As Boolean)
        MyBase.New("Month close", "A month closes after it ends and every day of every employee is marked.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _canClose = canClose
        _canReopen = canReopen
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        _month.Format = DateTimePickerFormat.Custom
        _month.CustomFormat = MonthFormat
        _month.ShowUpDown = True
        _month.MaxDate = Date.Today
        Dim top As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True}
        top.Controls.Add(New Label With {.Text = "Month", .AutoSize = True, .Margin = New Padding(0, 6, 6, 0)})
        top.Controls.Add(_month)
        top.Controls.Add(_state)
        _state.AutoSize = True
        _state.Margin = New Padding(Theme.Space2, 6, 0, 0)

        _blockers.Dock = DockStyle.Top
        _blockers.Height = 200
        _history.Dock = DockStyle.Fill
        _history.View = View.Details
        _history.FullRowSelect = True
        _history.Columns.Add("Action", 90)
        _history.Columns.Add("When", 150)
        _history.Columns.Add("User id", 80)
        _history.Columns.Add("Reason", 480)

        Dim bar = CreateButtonBar()
        _closeButton.Text = "Close month"
        _reopenButton.Text = "Reopen..."
        _exitButton.Text = "Exit"
        _exitButton.DialogResult = DialogResult.Cancel
        bar.Controls.Add(_closeButton)
        bar.Controls.Add(_reopenButton)
        bar.Controls.Add(_exitButton)
        CancelButton = _exitButton

        Content.Controls.Add(_history)
        Content.Controls.Add(_blockers)
        Content.Controls.Add(top)
        AddHandler _month.ValueChanged, Sub(sender As Object, e As EventArgs) RefreshView()
        AddHandler _closeButton.Click, Sub(sender As Object, e As EventArgs) CloseSelected()
        AddHandler _reopenButton.Click, Sub(sender As Object, e As EventArgs) ReopenSelected()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        RefreshView()
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        RefreshView()
    End Sub

    Private Sub RefreshView()
        ClearValidation()
        Dim year = _month.Value.Year
        Dim month = _month.Value.Month
        Dim loaded = _services.Board.LoadCloseSheet(year, month)
        _blockers.Items.Clear()
        _history.Items.Clear()
        _closeButton.Enabled = False
        _reopenButton.Enabled = False
        If Not loaded.Succeeded Then
            ShowError(loaded.Message)
            Return
        End If
        _sheet = loaded.Value
        _state.Text = If(_sheet.IsClosed, "CLOSED", "OPEN")
        _state.ForeColor = If(_sheet.IsClosed, Theme.Danger, Theme.Accent)
        For Each line In _sheet.Blockers
            _blockers.Items.Add(line)
        Next
        If Not _sheet.IsClosed AndAlso _sheet.Blockers.Count = 0 Then
            ShowStatus($"{_sheet.Summaries.Count} employees ready to close.")
        End If
        _closeButton.Enabled = _canClose AndAlso _sheet.CanClose
        _reopenButton.Enabled = _canReopen AndAlso _sheet.IsClosed
        FillHistory(year, month)
    End Sub

    Private Sub FillHistory(year As Integer, month As Integer)
        Dim history = _services.Board.LoadHistory(year, month)
        If Not history.Succeeded Then
            ShowError(history.Message)
            Return
        End If
        For Each entry In history.Value
            Dim item As New ListViewItem(If(entry.IsReopen, "Reopen", "Close"))
            item.SubItems.Add(DateTime.SpecifyKind(entry.EventUtc, DateTimeKind.Utc).ToLocalTime().ToString(LocalTimeFormat, CultureInfo.InvariantCulture))
            item.SubItems.Add(If(entry.UserId.HasValue, entry.UserId.Value.ToString(CultureInfo.InvariantCulture), String.Empty))
            item.SubItems.Add(entry.Reason)
            _history.Items.Add(item)
        Next
    End Sub

    Private Sub CloseSelected()
        Dim label = _month.Value.ToString(MonthFormat, CultureInfo.InvariantCulture)
        If MessageBox.Show(Me, $"Close attendance for {label}? Nobody can edit it until it is reopened with a reason.", Text,
                           MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then
            Return
        End If
        Dim result = _services.Board.CloseMonth(_month.Value.Year, _month.Value.Month)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        ShowStatus($"Attendance for {label} is closed.")
        RefreshView()
    End Sub

    Private Sub ReopenSelected()
        Dim label = _month.Value.ToString(MonthFormat, CultureInfo.InvariantCulture)
        Dim reason = AskReason($"Reopen attendance {label}")
        If reason Is Nothing Then Return
        Dim result = _services.Board.ReopenMonth(_month.Value.Year, _month.Value.Month, reason)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        ShowStatus($"Attendance for {label} is reopened.")
        RefreshView()
    End Sub

    ''' <summary>Asks for the reopen reason. Returns Nothing when cancelled. The service checks the length.</summary>
    Private Function AskReason(title As String) As String
        Using dialog As New Form With {.Text = title, .FormBorderStyle = FormBorderStyle.FixedDialog, .StartPosition = FormStartPosition.CenterParent,
                                       .MaximizeBox = False, .MinimizeBox = False, .ShowInTaskbar = False, .ClientSize = New Size(460, 220)}
            Dim prompt As New Label With {.Text = ReasonLabel, .AutoSize = True, .Location = New Point(12, 12)}
            Dim box As New TextBox With {.Multiline = True, .MaxLength = 500, .Location = New Point(12, 36), .Size = New Size(436, 120)}
            Dim ok As New Button With {.Text = "Reopen", .DialogResult = DialogResult.OK, .Location = New Point(268, 176)}
            Dim cancel As New Button With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .Location = New Point(364, 176)}
            dialog.Controls.AddRange(New Control() {prompt, box, ok, cancel})
            dialog.AcceptButton = ok
            dialog.CancelButton = cancel
            Return If(dialog.ShowDialog(Me) = DialogResult.OK, box.Text, Nothing)
        End Using
    End Function
End Class
