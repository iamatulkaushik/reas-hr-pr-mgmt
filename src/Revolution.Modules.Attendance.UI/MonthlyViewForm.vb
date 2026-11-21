Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>The month's monthly attendance, read only (ADR 0026): every active employee with the counts, the totals at the foot, and the incomplete or missing rows marked, because they block the month close.</summary>
Friend NotInheritable Class MonthlyViewForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1060
    Private Const FormHeight As Integer = 640
    Private Const MonthFormat As String = "MMMM yyyy"

    Private ReadOnly _services As AttendanceServices
    Private ReadOnly _month As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = MonthFormat, .ShowUpDown = True, .MaxDate = Date.Today, .Width = 150}
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _totals As New Label() With {.Dock = DockStyle.Bottom, .AutoSize = False, .Height = 30}
    Private ReadOnly _closeButton As New Button() With {.Text = "Close", .AutoSize = True, .DialogResult = DialogResult.Cancel}

    Public Sub New(services As AttendanceServices)
        MyBase.New("Month attendance", "Monthly attendance of every employee.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        _month.Value = services.Monthly.DefaultPeriod()
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        Dim top As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True}
        top.Controls.Add(New Label With {.Text = "Month", .AutoSize = True, .Margin = New Padding(0, 6, 6, 0)})
        top.Controls.Add(_month)
        _grid.Dock = DockStyle.Fill
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.ReadOnly = True
        _grid.RowHeadersVisible = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        For Each header In New String() {"Code", "Employee", "Days employed", "Present", "Half", "Weekly off", "Holiday", "Paid leave", "Absent", "OT hours", "Total", "Status"}
            _grid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = header, .FillWeight = If(header = "Employee", 26.0F, 10.0F)})
        Next
        Dim bar = CreateButtonBar()
        bar.Controls.Add(_closeButton)
        CancelButton = _closeButton
        Content.Controls.Add(_grid)
        Content.Controls.Add(_totals)
        Content.Controls.Add(top)
        AddHandler _month.ValueChanged, Sub(sender As Object, e As EventArgs) LoadRows()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadRows()
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        LoadRows()
    End Sub

    Private Sub LoadRows()
        ClearValidation()
        Dim period As New Date(_month.Value.Year, _month.Value.Month, 1)
        Dim result = _services.Monthly.LoadMonth(period)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _grid.Rows.Clear()
        Dim incomplete As Integer = 0
        For Each r In result.Value
            Dim state = If(Not r.HasRow, "Missing", If(r.IsComplete, "Complete", "Incomplete"))
            Dim at = _grid.Rows.Add(r.EmpCode, r.EmployeeName, r.WindowDays, r.PresentDays, r.HalfDays, r.WeeklyOffDays, r.HolidayDays, r.PaidLeaveDays,
                                    r.AbsentDays, r.OvertimeHours.ToString("0.##", CultureInfo.InvariantCulture), r.TotalDays, state)
            If state <> "Complete" Then
                incomplete += 1
                _grid.Rows(at).DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242)
            End If
        Next
        Dim rows = result.Value
        _totals.Text = $"Present {rows.Sum(Function(r) r.PresentDays)}   Half {rows.Sum(Function(r) r.HalfDays)}   Weekly off {rows.Sum(Function(r) r.WeeklyOffDays)}   " &
                       $"Holiday {rows.Sum(Function(r) r.HolidayDays)}   Paid leave {rows.Sum(Function(r) r.PaidLeaveDays)}   Absent {rows.Sum(Function(r) r.AbsentDays)}   " &
                       $"OT hours {rows.Sum(Function(r) r.OvertimeHours).ToString("0.##", CultureInfo.InvariantCulture)}"
        ShowStatus($"{rows.Count} employee(s); {incomplete} incomplete or missing.")
    End Sub
End Class
