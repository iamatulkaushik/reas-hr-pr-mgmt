Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>
''' Mark the same status for many employees over a date range (at most 31 days). Tick the employees, pick the days and the status, Apply.
''' Days already marked are left alone unless "Overwrite marked days" is ticked.
''' </summary>
Friend NotInheritable Class MultipleAttendanceForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 900
    Private Const FormHeight As Integer = 620
    Private Const DateFormat As String = "dd-MM-yyyy"
    Private Const MinutesPerHour As Integer = 60
    Private Const MaxHours As Decimal = 24D
    Private Const ColPick As String = "colPick"
    Private Const ColCode As String = "colCode"
    Private Const ColName As String = "colName"

    Private ReadOnly _services As AttendanceServices
    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _from As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = DateFormat, .MaxDate = Date.Today, .Width = 110}
    Private ReadOnly _to As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = DateFormat, .MaxDate = Date.Today, .Width = 110}
    Private ReadOnly _status As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 140}
    Private ReadOnly _overtime As New TextBox() With {.Width = 60, .Text = "0"}
    Private ReadOnly _overwrite As New CheckBox() With {.Text = "Overwrite marked days", .AutoSize = True}
    Private ReadOnly _search As New TextBox() With {.Width = 220}
    Private ReadOnly _allButton As New Button() With {.Text = "Tick all shown", .AutoSize = True}
    Private ReadOnly _noneButton As New Button() With {.Text = "Clear ticks", .AutoSize = True}
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _applyButton As New Button() With {.Text = "Apply", .AutoSize = True}
    Private ReadOnly _closeButton As New Button() With {.Text = "Close", .AutoSize = True, .DialogResult = DialogResult.Cancel}
    Private ReadOnly _picked As New HashSet(Of Integer)()
    Private _roll As IReadOnlyList(Of AttendanceSheetRow) = New List(Of AttendanceSheetRow)()
    Private _loading As Boolean

    Public Sub New(services As AttendanceServices, canEdit As Boolean)
        MyBase.New("Multiple attendance", "Same status for many employees over a date range.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _canEdit = canEdit
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        Dim options As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .WrapContents = True}
        options.Controls.Add(Caption("From"))
        options.Controls.Add(_from)
        options.Controls.Add(Caption("To"))
        options.Controls.Add(_to)
        options.Controls.Add(Caption("Status"))
        options.Controls.Add(_status)
        options.Controls.Add(Caption("OT hours"))
        options.Controls.Add(_overtime)
        options.Controls.Add(_overwrite)
        FillStatuses()

        Dim finder As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True}
        finder.Controls.Add(Caption("Find"))
        finder.Controls.Add(_search)
        finder.Controls.Add(_allButton)
        finder.Controls.Add(_noneButton)

        _grid.Dock = DockStyle.Fill
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.MultiSelect = False
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        _grid.Columns.Add(New DataGridViewCheckBoxColumn With {.Name = ColPick, .HeaderText = "Mark", .FillWeight = 10.0F})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = ColCode, .HeaderText = "Code", .FillWeight = 20.0F, .ReadOnly = True})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = ColName, .HeaderText = "Employee", .FillWeight = 70.0F, .ReadOnly = True})

        Dim bar = CreateButtonBar()
        _applyButton.Enabled = _canEdit
        bar.Controls.Add(_applyButton)
        bar.Controls.Add(_closeButton)
        CancelButton = _closeButton

        Content.Controls.Add(_grid)
        Content.Controls.Add(finder)
        Content.Controls.Add(options)
        AddHandler _search.TextChanged, Sub(sender As Object, e As EventArgs) FillEmployees()
        AddHandler _allButton.Click, Sub(sender As Object, e As EventArgs) TickShown(True)
        AddHandler _noneButton.Click, Sub(sender As Object, e As EventArgs) TickShown(False)
        AddHandler _applyButton.Click, Sub(sender As Object, e As EventArgs) Apply()
        AddHandler _grid.CurrentCellDirtyStateChanged, Sub(sender As Object, e As EventArgs) CommitTick()
        AddHandler _grid.CellValueChanged, AddressOf OnTickChanged
        AddHandler _from.ValueChanged, Sub(sender As Object, e As EventArgs) KeepRangeOrdered(True)
        AddHandler _to.ValueChanged, Sub(sender As Object, e As EventArgs) KeepRangeOrdered(False)
    End Sub

    Private Shared Function Caption(text As String) As Label
        Return New Label With {.Text = text, .AutoSize = True, .Margin = New Padding(Theme.Space2, 6, 4, 0)}
    End Function

    Private Sub FillStatuses()
        Dim choices As New List(Of KeyValuePair(Of AttendanceStatus, String)) From {
            New KeyValuePair(Of AttendanceStatus, String)(AttendanceStatus.Present, "P  Present"),
            New KeyValuePair(Of AttendanceStatus, String)(AttendanceStatus.Absent, "A  Absent"),
            New KeyValuePair(Of AttendanceStatus, String)(AttendanceStatus.HalfDay, "HD  Half day"),
            New KeyValuePair(Of AttendanceStatus, String)(AttendanceStatus.WeeklyOff, "WO  Weekly off"),
            New KeyValuePair(Of AttendanceStatus, String)(AttendanceStatus.Holiday, "H  Holiday"),
            New KeyValuePair(Of AttendanceStatus, String)(AttendanceStatus.PaidLeave, "PL  Paid leave")}
        _status.DataSource = choices
        _status.DisplayMember = "Value"
        _status.ValueMember = "Key"
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadRoll()
        _search.Focus()
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        LoadRoll()
    End Sub

    Protected Overrides Sub OnSaveRequested()
        Apply()
    End Sub

    Private Sub KeepRangeOrdered(fromChanged As Boolean)
        If _loading Then Return
        If _from.Value.Date <= _to.Value.Date Then Return
        _loading = True
        Try
            If fromChanged Then _to.Value = _from.Value Else _from.Value = _to.Value
        Finally
            _loading = False
        End Try
    End Sub

    Private Sub LoadRoll()
        ClearValidation()
        Dim result = _services.Board.LoadDay(Date.Today)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _roll = result.Value
        _picked.RemoveWhere(Function(id) Not _roll.Any(Function(r) r.EmployeeId = id))
        FillEmployees()
    End Sub

    Private Sub FillEmployees()
        Dim filter As String = _search.Text.Trim()
        _loading = True
        Try
            _grid.Rows.Clear()
            For Each person In _roll
                If filter.Length > 0 AndAlso person.EmpCode.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 AndAlso
                   person.EmployeeName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 Then Continue For
                Dim at = _grid.Rows.Add(_picked.Contains(person.EmployeeId), person.EmpCode, person.EmployeeName)
                _grid.Rows(at).Tag = person.EmployeeId
            Next
        Finally
            _loading = False
        End Try
        ShowCount()
    End Sub

    Private Sub CommitTick()
        If _grid.IsCurrentCellDirty Then _grid.CommitEdit(DataGridViewDataErrorContexts.Commit)
    End Sub

    Private Sub OnTickChanged(sender As Object, e As DataGridViewCellEventArgs)
        If _loading OrElse e.RowIndex < 0 OrElse _grid.Columns(e.ColumnIndex).Name <> ColPick Then Return
        Dim id = Convert.ToInt32(_grid.Rows(e.RowIndex).Tag, CultureInfo.InvariantCulture)
        If Convert.ToBoolean(_grid.Rows(e.RowIndex).Cells(ColPick).Value, CultureInfo.InvariantCulture) Then _picked.Add(id) Else _picked.Remove(id)
        ShowCount()
    End Sub

    Private Sub TickShown(ticked As Boolean)
        _loading = True
        Try
            For Each row As DataGridViewRow In _grid.Rows
                row.Cells(ColPick).Value = ticked
                Dim id = Convert.ToInt32(row.Tag, CultureInfo.InvariantCulture)
                If ticked Then _picked.Add(id) Else _picked.Remove(id)
            Next
        Finally
            _loading = False
        End Try
        ShowCount()
    End Sub

    Private Sub ShowCount()
        ShowStatus($"{_picked.Count} employee(s) ticked of {_roll.Count}.")
    End Sub

    Private Sub Apply()
        If Not _canEdit Then
            ShowError("You are not allowed to mark attendance.")
            Return
        End If
        ClearValidation()
        Dim overtime As Integer
        If Not TryParseHours(_overtime.Text, overtime) Then
            SetFieldError(_overtime, "Overtime must be hours such as 1.5, up to 24.")
            Return
        End If
        If _picked.Count = 0 Then
            ShowError("Tick at least one employee.")
            Return
        End If
        Dim status = DirectCast(_status.SelectedValue, AttendanceStatus)
        Dim days = (_to.Value.Date - _from.Value.Date).Days + 1
        Dim question = $"Mark {_picked.Count} employee(s) for {days} day(s), {_from.Value.ToString(DateFormat, CultureInfo.InvariantCulture)} to {_to.Value.ToString(DateFormat, CultureInfo.InvariantCulture)}?"
        If MessageBox.Show(Me, question, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
        Dim result = _services.Board.MarkMultiple(_from.Value.Date, _to.Value.Date, _picked.ToList(), status, overtime, _overwrite.Checked)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        ShowStatus(If(result.Value = 0, "Nothing to save: every day is already marked.", $"{result.Value} day(s) saved."))
    End Sub

    Private Shared Function TryParseHours(text As String, ByRef minutes As Integer) As Boolean
        Dim cleaned = If(text, String.Empty).Trim()
        If cleaned.Length = 0 Then
            minutes = 0
            Return True
        End If
        Dim hours As Decimal
        If Not Decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.CurrentCulture, hours) OrElse hours < 0D OrElse hours > MaxHours Then Return False
        minutes = CInt(Math.Round(hours * MinutesPerHour, 0, MidpointRounding.AwayFromZero))
        Return True
    End Function
End Class
