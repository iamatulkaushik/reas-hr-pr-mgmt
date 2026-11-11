Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>
''' One employee's whole month on one screen: pick the employee and the month, set the status and overtime of each day, Save.
''' Only days you changed are saved, and each keeps its shift and worked minutes. To remove a mark use Delete attendance.
''' </summary>
Friend NotInheritable Class MonthAttendanceForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 820
    Private Const FormHeight As Integer = 700
    Private Const MonthFormat As String = "MMMM yyyy"
    Private Const DateFormat As String = "dd-MM-yyyy"
    Private Const MinutesPerHour As Integer = 60
    Private Const MaxHours As Decimal = 24D
    Private Const ColDate As String = "colDate"
    Private Const ColDay As String = "colDay"
    Private Const ColStatus As String = "colStatus"
    Private Const ColOvertime As String = "colOvertime"
    Private Const Unmarked As String = ""

    Private ReadOnly _services As AttendanceServices
    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _employee As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 280}
    Private ReadOnly _month As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = MonthFormat, .ShowUpDown = True, .MaxDate = Date.Today, .Width = 150}
    Private ReadOnly _loadButton As New Button() With {.Text = "Load", .AutoSize = True}
    Private ReadOnly _summary As New Label() With {.Dock = DockStyle.Top, .AutoSize = False, .Height = 44}
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _saveButton As New Button() With {.Text = "Save", .AutoSize = True}
    Private ReadOnly _closeButton As New Button() With {.Text = "Close", .AutoSize = True, .DialogResult = DialogResult.Cancel}
    Private ReadOnly _labels As New List(Of String) From {Unmarked, "P  Present", "A  Absent", "HD  Half day", "WO  Weekly off", "H  Holiday", "PL  Paid leave"}
    Private ReadOnly _statuses As New Dictionary(Of String, AttendanceStatus) From {
        {"P  Present", AttendanceStatus.Present}, {"A  Absent", AttendanceStatus.Absent}, {"HD  Half day", AttendanceStatus.HalfDay},
        {"WO  Weekly off", AttendanceStatus.WeeklyOff}, {"H  Holiday", AttendanceStatus.Holiday}, {"PL  Paid leave", AttendanceStatus.PaidLeave}}
    Private _loaded As New Dictionary(Of Date, EmployeeDayRow)()
    Private _shownEmployeeId As Integer
    Private _shownMonth As Date

    Public Sub New(services As AttendanceServices, canEdit As Boolean)
        MyBase.New("Month attendance", "One employee, every day of a month.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _canEdit = canEdit
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        Dim top As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True}
        top.Controls.Add(New Label With {.Text = "Employee", .AutoSize = True, .Margin = New Padding(0, 6, 6, 0)})
        top.Controls.Add(_employee)
        top.Controls.Add(New Label With {.Text = "Month", .AutoSize = True, .Margin = New Padding(Theme.Space2, 6, 6, 0)})
        top.Controls.Add(_month)
        top.Controls.Add(_loadButton)
        _grid.Dock = DockStyle.Fill
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = ColDate, .HeaderText = "Date", .FillWeight = 25.0F, .ReadOnly = True})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = ColDay, .HeaderText = "Day", .FillWeight = 20.0F, .ReadOnly = True})
        Dim statusColumn As New DataGridViewComboBoxColumn With {.Name = ColStatus, .HeaderText = "Status", .FillWeight = 35.0F, .FlatStyle = FlatStyle.Flat}
        For Each label In _labels
            statusColumn.Items.Add(label)
        Next
        _grid.Columns.Add(statusColumn)
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = ColOvertime, .HeaderText = "OT hours", .FillWeight = 20.0F})
        Dim bar = CreateButtonBar()
        _saveButton.Enabled = _canEdit
        bar.Controls.Add(_saveButton)
        bar.Controls.Add(_closeButton)
        CancelButton = _closeButton
        Content.Controls.Add(_grid)
        Content.Controls.Add(_summary)
        Content.Controls.Add(top)
        AddHandler _loadButton.Click, Sub(sender As Object, e As EventArgs) LoadMonth()
        AddHandler _saveButton.Click, Sub(sender As Object, e As EventArgs) SaveChanges()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        Dim roll = _services.Board.LoadDay(Date.Today)
        If Not roll.Succeeded Then
            ShowError(roll.Message)
            Return
        End If
        _employee.DisplayMember = "Value"
        _employee.ValueMember = "Key"
        _employee.DataSource = roll.Value.Select(Function(r) New KeyValuePair(Of Integer, String)(r.EmployeeId, $"{r.EmpCode}  {r.EmployeeName}")).ToList()
        ShowStatus("Pick an employee and a month, then Load.")
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        LoadMonth()
    End Sub

    Protected Overrides Sub OnSaveRequested()
        SaveChanges()
    End Sub

    Private Function FirstOfMonth() As Date
        Return New Date(_month.Value.Year, _month.Value.Month, 1)
    End Function

    Private Sub LoadMonth()
        ClearValidation()
        If _employee.SelectedValue Is Nothing Then
            ShowError("Pick an employee.")
            Return
        End If
        Dim employeeId = Convert.ToInt32(_employee.SelectedValue, CultureInfo.InvariantCulture)
        Dim first = FirstOfMonth()
        Dim result = _services.Board.LoadEmployeeMonth(employeeId, first.Year, first.Month)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _loaded = result.Value.ToDictionary(Function(r) r.Day)
        _shownEmployeeId = employeeId
        _shownMonth = first
        _grid.Rows.Clear()
        For offset As Integer = 0 To Date.DaysInMonth(first.Year, first.Month) - 1
            Dim day = first.AddDays(offset)
            Dim existing As EmployeeDayRow = Nothing
            Dim label = Unmarked
            Dim hours = String.Empty
            If _loaded.TryGetValue(day, existing) Then
                label = LabelOf(existing.Status)
                If existing.OvertimeMinutes > 0 Then hours = (existing.OvertimeMinutes / CDec(MinutesPerHour)).ToString("0.##", CultureInfo.CurrentCulture)
            End If
            Dim at = _grid.Rows.Add(day.ToString(DateFormat, CultureInfo.InvariantCulture), day.ToString("ddd", CultureInfo.CurrentCulture), label, hours)
            _grid.Rows(at).Tag = day
            If day > Date.Today Then _grid.Rows(at).ReadOnly = True
        Next
        ShowSummary()
    End Sub

    Private Function LabelOf(status As AttendanceStatus) As String
        For Each pair In _statuses
            If pair.Value = status Then Return pair.Key
        Next
        Return Unmarked
    End Function

    Private Sub ShowSummary()
        Dim records = _loaded.Values.Select(Function(r) New DayRecord(r.Day, r.Status, r.OvertimeMinutes)).ToList()
        Dim built = MonthSummaryBuilder.Build(_shownMonth.Year, _shownMonth.Month, records)
        If Not built.Succeeded Then
            _summary.Text = built.Message
            Return
        End If
        Dim s = built.Value
        _summary.Text = $"Present {s.PresentDays}, Half {s.HalfDays}, Weekly off {s.WeeklyOffDays}, Holiday {s.HolidayDays}, Paid leave {s.PaidLeaveDays}, Absent {s.AbsentDays}, Unmarked {s.UnmarkedDays}" &
                        Environment.NewLine & $"Days paid {s.DaysPaid}, LOP {s.LopDays}, OT {s.OvertimeHours} h"
    End Sub

    Private Sub SaveChanges()
        If Not _canEdit Then
            ShowError("You are not allowed to mark attendance.")
            Return
        End If
        ClearValidation()
        If _shownEmployeeId = 0 Then
            ShowError("Load a month first.")
            Return
        End If
        _grid.EndEdit()
        Dim changed As New List(Of EmployeeDayRow)()
        For Each row As DataGridViewRow In _grid.Rows
            Dim label = Convert.ToString(row.Cells(ColStatus).Value, CultureInfo.InvariantCulture)
            If String.IsNullOrEmpty(label) Then Continue For
            Dim minutes As Integer
            If Not TryParseHours(Convert.ToString(row.Cells(ColOvertime).Value, CultureInfo.CurrentCulture), minutes) Then
                SetFieldError(_grid, $"Overtime on {row.Cells(ColDate).Value} must be hours such as 1.5, up to 24.")
                Return
            End If
            Dim day = DirectCast(row.Tag, Date)
            Dim status = _statuses(label)
            Dim existing As EmployeeDayRow = Nothing
            If _loaded.TryGetValue(day, existing) Then
                If existing.Status = status AndAlso existing.OvertimeMinutes = minutes Then Continue For
                changed.Add(New EmployeeDayRow(day, status, existing.ShiftId, existing.WorkedMinutes, minutes))
            Else
                changed.Add(New EmployeeDayRow(day, status, Nothing, 0, minutes))
            End If
        Next
        If changed.Count = 0 Then
            ShowStatus("Nothing changed.")
            Return
        End If
        Dim result = _services.Board.SaveEmployeeMonth(_shownEmployeeId, changed)
        LoadMonth()
        If result.Succeeded Then ShowStatus($"{changed.Count} day(s) saved.") Else ShowError(result.Message)
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
