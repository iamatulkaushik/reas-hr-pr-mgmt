Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>
''' One employee's attendance for one month (ADR 0026): pick the month and the employee, enter the counts, and watch the total against the days the employee worked.
''' Save stores the row even when the total is short (the month cannot close until it matches); Delete attendance removes a row.
''' </summary>
Friend NotInheritable Class MonthlySingleForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 620
    Private Const FormHeight As Integer = 560
    Private Const MonthFormat As String = "MMMM yyyy"
    Private Const MaxDays As Integer = 31

    Private ReadOnly _services As AttendanceServices
    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _month As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = MonthFormat, .ShowUpDown = True, .MaxDate = Date.Today, .Width = 150}
    Private ReadOnly _employee As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 300}
    Private ReadOnly _window As New Label() With {.AutoSize = True}
    Private ReadOnly _present As Counter = NewCounter()
    Private ReadOnly _half As Counter = NewCounter()
    Private ReadOnly _weeklyOff As Counter = NewCounter()
    Private ReadOnly _holiday As Counter = NewCounter()
    Private ReadOnly _paidLeave As Counter = NewCounter()
    Private ReadOnly _absent As Counter = NewCounter()
    Private ReadOnly _overtime As New NumericUpDown() With {.DecimalPlaces = 2, .Minimum = 0D, .Maximum = 744D, .Width = 90}
    Private ReadOnly _remarks As New TextBox() With {.MaxLength = 100, .Width = 300}
    Private ReadOnly _total As New Label() With {.AutoSize = True, .Font = New Font(SystemFonts.DefaultFont, FontStyle.Bold)}
    Private ReadOnly _fillRules As New Button() With {.Text = "Fill weekly off and holidays", .AutoSize = True}
    Private ReadOnly _saveButton As New Button() With {.Text = "Save", .AutoSize = True}
    Private ReadOnly _closeButton As New Button() With {.Text = "Close", .AutoSize = True, .DialogResult = DialogResult.Cancel}
    Private _rows As IReadOnlyList(Of MonthlyAttendanceRow) = New List(Of MonthlyAttendanceRow)()
    Private _loading As Boolean

    ''' <summary>A numeric box that shows a whole number from 0 to 31.</summary>
    Private NotInheritable Class Counter
        Inherits NumericUpDown
    End Class

    Public Sub New(services As AttendanceServices, canEdit As Boolean)
        MyBase.New("Monthly attendance - single", "One employee, one month.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _canEdit = canEdit
        ClientSize = New Size(FormWidth, FormHeight)
        _month.Value = services.Monthly.DefaultPeriod()
        BuildLayout()
    End Sub

    Private Shared Function NewCounter() As Counter
        Return New Counter() With {.Minimum = 0D, .Maximum = MaxDays, .DecimalPlaces = 0, .Width = 70}
    End Function

    Private Sub BuildLayout()
        Dim table As New TableLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 2}
        table.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        AddLine(table, "Month", _month)
        AddLine(table, "Employee", _employee)
        AddLine(table, "Days employed", _window)
        AddLine(table, "Present days", _present)
        AddLine(table, "Half days", _half)
        AddLine(table, "Weekly off days", _weeklyOff)
        AddLine(table, "Holiday days", _holiday)
        AddLine(table, "Paid leave days", _paidLeave)
        AddLine(table, "Absent days", _absent)
        AddLine(table, "Overtime hours", _overtime)
        AddLine(table, "Remarks", _remarks)
        AddLine(table, "Total", _total)
        Dim bar = CreateButtonBar()
        _saveButton.Enabled = _canEdit
        _fillRules.Enabled = _canEdit
        bar.Controls.Add(_saveButton)
        bar.Controls.Add(_fillRules)
        bar.Controls.Add(_closeButton)
        CancelButton = _closeButton
        Content.Controls.Add(table)
        AddHandler _month.ValueChanged, Sub(sender As Object, e As EventArgs) LoadMonth()
        AddHandler _employee.SelectedIndexChanged, Sub(sender As Object, e As EventArgs) ShowEmployee()
        For Each box As NumericUpDown In New NumericUpDown() {_present, _half, _weeklyOff, _holiday, _paidLeave, _absent}
            AddHandler box.ValueChanged, Sub(sender As Object, e As EventArgs) UpdateTotal()
        Next
        AddHandler _saveButton.Click, Sub(sender As Object, e As EventArgs) SaveRow()
        AddHandler _fillRules.Click, Sub(sender As Object, e As EventArgs) FillFromRules()
    End Sub

    Private Shared Sub AddLine(table As TableLayoutPanel, caption As String, control As Control)
        table.Controls.Add(New Label With {.Text = caption, .AutoSize = True, .Margin = New Padding(0, 8, 12, 0)})
        control.Margin = New Padding(0, 4, 0, 4)
        table.Controls.Add(control)
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadMonth()
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        LoadMonth()
    End Sub

    Private Function Period() As Date
        Return New Date(_month.Value.Year, _month.Value.Month, 1)
    End Function

    Private Function Selected() As MonthlyAttendanceRow
        Return If(_employee.SelectedIndex >= 0 AndAlso _employee.SelectedIndex < _rows.Count, _rows(_employee.SelectedIndex), Nothing)
    End Function

    Private Sub LoadMonth()
        ClearValidation()
        Dim keep = Selected()
        Dim result = _services.Monthly.LoadMonth(Period())
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _rows = result.Value
        _loading = True
        Try
            _employee.Items.Clear()
            For Each r In _rows
                _employee.Items.Add($"{r.EmpCode}  {r.EmployeeName}")
            Next
            If _rows.Count > 0 Then
                Dim at = If(keep Is Nothing, 0, _rows.ToList().FindIndex(Function(r) r.EmployeeId = keep.EmployeeId))
                _employee.SelectedIndex = Math.Max(at, 0)
            End If
        Finally
            _loading = False
        End Try
        ShowEmployee()
        ShowStatus($"{_rows.Count} employee(s) in {Period().ToString(MonthFormat, CultureInfo.InvariantCulture)}.")
    End Sub

    Private Sub ShowEmployee()
        Dim person = Selected()
        _loading = True
        Try
            If person Is Nothing Then
                _window.Text = String.Empty
                For Each box As NumericUpDown In New NumericUpDown() {_present, _half, _weeklyOff, _holiday, _paidLeave, _absent, _overtime}
                    box.Value = 0D
                Next
                _remarks.Text = String.Empty
            Else
                _window.Text = $"{person.WindowDays} ({person.WindowFrom.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)} to {person.WindowTo.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)})"
                _present.Value = person.PresentDays
                _half.Value = person.HalfDays
                _weeklyOff.Value = person.WeeklyOffDays
                _holiday.Value = person.HolidayDays
                _paidLeave.Value = person.PaidLeaveDays
                _absent.Value = person.AbsentDays
                _overtime.Value = person.OvertimeHours
                _remarks.Text = person.Remarks
            End If
        Finally
            _loading = False
        End Try
        UpdateTotal()
    End Sub

    Private Function TotalDays() As Integer
        Return CInt(_present.Value + _half.Value + _weeklyOff.Value + _holiday.Value + _paidLeave.Value + _absent.Value)
    End Function

    Private Sub UpdateTotal()
        Dim person = Selected()
        If person Is Nothing Then
            _total.Text = String.Empty
            Return
        End If
        Dim total = TotalDays()
        _total.Text = $"{total} of {person.WindowDays}"
        _total.ForeColor = If(total = person.WindowDays, Color.SeaGreen, Color.Firebrick)
    End Sub

    Private Sub FillFromRules()
        Dim person = Selected()
        If person Is Nothing Then Return
        Dim rules = _services.Monthly.RuleDefaults(person)
        If Not rules.Succeeded Then
            ShowError(rules.Message)
            Return
        End If
        _weeklyOff.Value = rules.Value.WeeklyOffDays
        _holiday.Value = rules.Value.HolidayDays
        ShowStatus("Weekly off days and holidays are filled from the company rules.")
    End Sub

    Private Sub SaveRow()
        ClearValidation()
        Dim person = Selected()
        If person Is Nothing Then
            ShowError("Choose an employee first.")
            Return
        End If
        Dim row = person.WithCounts(CInt(_present.Value), CInt(_half.Value), CInt(_weeklyOff.Value), CInt(_holiday.Value), CInt(_paidLeave.Value),
                                    CInt(_absent.Value), _overtime.Value, _remarks.Text.Trim())
        Dim result = _services.Monthly.Save(Period(), row, False)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Dim total = row.TotalDays
        LoadMonth()
        If total = person.WindowDays Then
            ShowStatus("Saved.")
        Else
            ShowStatus($"Saved, but the days add up to {total} of {person.WindowDays}. The month cannot close until they match.")
        End If
    End Sub
End Class
