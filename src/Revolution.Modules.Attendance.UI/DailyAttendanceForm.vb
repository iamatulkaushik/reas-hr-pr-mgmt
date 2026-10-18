Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>Mark one day for every employee on the roll. Status is typed as a code (P, A, HD, WO, H, PL). Save writes the marked rows.</summary>
Friend NotInheritable Class DailyAttendanceForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 980
    Private Const FormHeight As Integer = 600
    Private Const ColCode As String = "colCode"
    Private Const ColName As String = "colName"
    Private Const ColStatus As String = "colStatus"
    Private Const ColOvertime As String = "colOvertime"
    Private Const MinutesPerHour As Integer = 60
    Private Const DateFormat As String = "dd-MM-yyyy"
    Private Const LegendText As String = "P Present   A Absent   HD Half day   WO Weekly off   H Holiday   PL Paid leave. Overtime in hours."

    Private Shared ReadOnly Codes As New Dictionary(Of String, AttendanceStatus)(StringComparer.OrdinalIgnoreCase) From {
        {"P", AttendanceStatus.Present}, {"A", AttendanceStatus.Absent}, {"HD", AttendanceStatus.HalfDay},
        {"WO", AttendanceStatus.WeeklyOff}, {"H", AttendanceStatus.Holiday}, {"PL", AttendanceStatus.PaidLeave}}

    Private ReadOnly _services As AttendanceServices
    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _date As New DateTimePicker()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _saveButton As New Button()
    Private ReadOnly _closeButton As New Button()
    Private ReadOnly _loadButton As New Button()
    Private _rows As IReadOnlyList(Of AttendanceSheetRow) = New List(Of AttendanceSheetRow)()
    Private ReadOnly _changed As New HashSet(Of Integer)()
    Private _dirty As Boolean
    Private _loading As Boolean

    Public Sub New(services As AttendanceServices, canEdit As Boolean)
        MyBase.New("Daily attendance", LegendText)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _canEdit = canEdit
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        Dim top As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True}
        _date.Format = DateTimePickerFormat.Custom
        _date.CustomFormat = DateFormat
        _date.MaxDate = Date.Today
        _loadButton.Text = "Load (F5)"
        top.Controls.Add(New Label With {.Text = "Date", .AutoSize = True, .Margin = New Padding(0, 6, 6, 0)})
        top.Controls.Add(_date)
        top.Controls.Add(_loadButton)

        _grid.Dock = DockStyle.Fill
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn(ColCode, "Code", 15.0F, True)
        AddColumn(ColName, "Employee", 50.0F, True)
        AddColumn(ColStatus, "Status", 15.0F, Not _canEdit)
        AddColumn(ColOvertime, "OT hours", 20.0F, Not _canEdit)

        Dim bar = CreateButtonBar()
        _saveButton.Text = "Save"
        _saveButton.Enabled = _canEdit
        _closeButton.Text = "Close"
        _closeButton.DialogResult = DialogResult.Cancel
        bar.Controls.Add(_saveButton)
        bar.Controls.Add(_closeButton)
        CancelButton = _closeButton

        Content.Controls.Add(_grid)
        Content.Controls.Add(top)
        AddHandler _loadButton.Click, Sub(sender As Object, e As EventArgs) ReloadWithConfirm()
        AddHandler _date.ValueChanged, Sub(sender As Object, e As EventArgs) OnDateChanged()
        AddHandler _saveButton.Click, Sub(sender As Object, e As EventArgs) SaveSheet()
        AddHandler _grid.CellEndEdit, AddressOf OnCellEndEdit
    End Sub

    Private Sub AddColumn(name As String, header As String, weight As Single, isReadOnly As Boolean)
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = name, .HeaderText = header, .FillWeight = weight, .ReadOnly = isReadOnly,
            .SortMode = DataGridViewColumnSortMode.NotSortable})
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadSheet()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If _dirty AndAlso e.CloseReason = CloseReason.UserClosing AndAlso
           MessageBox.Show(Me, "Discard the unsaved attendance?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                           MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then
            e.Cancel = True
            Return
        End If
        MyBase.OnFormClosing(e)
    End Sub

    Protected Overrides Sub OnSaveRequested()
        SaveSheet()
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        ReloadWithConfirm()
    End Sub

    Private Sub ReloadWithConfirm()
        If _dirty AndAlso MessageBox.Show(Me, "Discard the unsaved attendance and reload?", Text, MessageBoxButtons.YesNo,
                                          MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then
            Return
        End If
        LoadSheet()
    End Sub

    Private Sub OnDateChanged()
        If _loading Then Return
        ReloadWithConfirm()
    End Sub

    Private Sub LoadSheet()
        ClearValidation()
        Dim result = _services.Board.LoadDay(_date.Value.Date)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _loading = True
        _rows = result.Value
        _grid.Rows.Clear()
        For Each row In _rows
            _grid.Rows.Add(row.EmpCode, row.EmployeeName, CodeOf(row.Status), HoursText(row.OvertimeMinutes))
        Next
        _loading = False
        _dirty = False
        _changed.Clear()
        ShowStatus($"{_rows.Count} employees")
    End Sub

    Private Sub OnCellEndEdit(sender As Object, e As DataGridViewCellEventArgs)
        If _loading OrElse e.RowIndex < 0 Then Return
        ClearValidation()
        Dim index = e.RowIndex
        Dim old = _rows(index)
        Dim statusText = Convert.ToString(_grid.Rows(index).Cells(ColStatus).Value, CultureInfo.InvariantCulture).Trim()
        Dim status As AttendanceStatus? = Nothing
        If statusText.Length > 0 Then
            Dim parsed As AttendanceStatus
            If Not Codes.TryGetValue(statusText, parsed) Then
                ShowError("Status must be P, A, HD, WO, H or PL.")
                _grid.Rows(index).Cells(ColStatus).Value = CodeOf(old.Status)
                Return
            End If
            status = parsed
        End If
        Dim overtime As Integer
        If Not TryParseHours(Convert.ToString(_grid.Rows(index).Cells(ColOvertime).Value, CultureInfo.CurrentCulture), overtime) Then
            ShowError("Overtime must be hours such as 1.5, up to 24.")
            _grid.Rows(index).Cells(ColOvertime).Value = HoursText(old.OvertimeMinutes)
            Return
        End If
        Dim updated = New AttendanceSheetRow(old.EmployeeId, old.EmpCode, old.EmployeeName, status, old.ShiftId, old.WorkedMinutes, overtime)
        _rows = _rows.Select(Function(r, i) If(i = index, updated, r)).ToList()
        _changed.Add(updated.EmployeeId)
        _dirty = True
    End Sub

    Private Sub SaveSheet()
        If Not _canEdit Then Return
        ClearValidation()
        If Not _grid.EndEdit() Then Return
        Dim result = _services.Board.SaveDay(_date.Value.Date, _rows.Where(Function(r) _changed.Contains(r.EmployeeId)).ToList())
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _dirty = False
        _changed.Clear()
        ShowStatus("Attendance saved.")
        LoadSheet()
    End Sub

    Private Shared Function CodeOf(status As AttendanceStatus?) As String
        If Not status.HasValue Then Return String.Empty
        Return Codes.First(Function(pair) pair.Value = status.Value).Key
    End Function

    Private Shared Function HoursText(minutes As Integer) As String
        Return (minutes / CDec(MinutesPerHour)).ToString("0.00", CultureInfo.CurrentCulture)
    End Function

    Private Shared Function TryParseHours(text As String, ByRef minutes As Integer) As Boolean
        Dim cleaned = If(text, String.Empty).Trim()
        If cleaned.Length = 0 Then
            minutes = 0
            Return True
        End If
        Dim hours As Decimal
        If Not Decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.CurrentCulture, hours) OrElse hours < 0D OrElse hours > 24D Then Return False
        minutes = CInt(Math.Round(hours * MinutesPerHour, 0, MidpointRounding.AwayFromZero))
        Return True
    End Function
End Class
