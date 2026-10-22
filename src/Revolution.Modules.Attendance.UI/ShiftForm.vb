Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>Shift master: start and end time, unpaid break, full and half day minutes, and the overtime rounding step.</summary>
Friend NotInheritable Class ShiftForm
    Inherits RosterFormBase

    Private Const TimeFormat As String = "HH:mm"
    Private Const ShowFormat As String = "hh\:mm"
    Private Const MaxMinutes As Integer = 1440

    Private ReadOnly _services As AttendanceServices
    Private ReadOnly _shifts As New Dictionary(Of Integer, ShiftItem)()
    Private ReadOnly _name As New TextBox() With {.MaxLength = 100}
    Private ReadOnly _start As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = TimeFormat, .ShowUpDown = True}
    Private ReadOnly _end As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = TimeFormat, .ShowUpDown = True}
    Private ReadOnly _break As New NumericUpDown() With {.Minimum = 0, .Maximum = MaxMinutes}
    Private ReadOnly _breakAfter As New NumericUpDown() With {.Minimum = 0, .Maximum = MaxMinutes}
    Private ReadOnly _fullDay As New NumericUpDown() With {.Minimum = 1, .Maximum = MaxMinutes}
    Private ReadOnly _halfDay As New NumericUpDown() With {.Minimum = 1, .Maximum = MaxMinutes}
    Private ReadOnly _rounding As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList}
    Private ReadOnly _active As New CheckBox() With {.Text = "Active", .AutoSize = True}
    Private _currentId As Integer

    Public Sub New(services As AttendanceServices, canEdit As Boolean)
        MyBase.New("Shifts", "Attendance", New String() {"Shift", "Start", "End", "Full day (min)", "Status"}, canEdit, False)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
    End Sub

    Protected Overrides Sub BuildEditor(layout As TableLayoutPanel)
        _rounding.DataSource = RoundingChoices()
        _rounding.DisplayMember = "Value"
        _rounding.ValueMember = "Key"
        AddField(layout, "Shift name", _name)
        AddField(layout, "Start time", _start)
        AddField(layout, "End time (earlier than start means the shift ends the next day)", _end)
        AddField(layout, "Unpaid break (minutes)", _break)
        AddField(layout, "Break is cut only when the day is longer than (minutes)", _breakAfter)
        AddField(layout, "Full day (net minutes)", _fullDay)
        AddField(layout, "Half day (net minutes)", _halfDay)
        AddField(layout, "Overtime rounds down to", _rounding)
        AddField(layout, "Status", _active)
        ClearEditor()
    End Sub

    Private Shared Function RoundingChoices() As List(Of KeyValuePair(Of Integer, String))
        Return New List(Of KeyValuePair(Of Integer, String)) From {
            New KeyValuePair(Of Integer, String)(CInt(OvertimeRounding.None), "No rounding (1 minute)"),
            New KeyValuePair(Of Integer, String)(CInt(OvertimeRounding.QuarterHour), "15 minutes"),
            New KeyValuePair(Of Integer, String)(CInt(OvertimeRounding.HalfHour), "30 minutes"),
            New KeyValuePair(Of Integer, String)(CInt(OvertimeRounding.Hour), "60 minutes")}
    End Function

    Protected Overrides Function LoadRows() As OperationResult(Of IReadOnlyList(Of RosterRow))
        Dim result = _services.Roster.ListShifts()
        If Not result.Succeeded Then Return OperationResult(Of IReadOnlyList(Of RosterRow)).Fail(result.Message)
        _shifts.Clear()
        Dim rows As New List(Of RosterRow)()
        For Each item In result.Value
            Dim id As Integer = item.ShiftId.GetValueOrDefault()
            _shifts(id) = item
            rows.Add(New RosterRow(id, New String() {item.Name, item.StartTime.ToString(ShowFormat, CultureInfo.InvariantCulture),
                                                     item.EndTime.ToString(ShowFormat, CultureInfo.InvariantCulture),
                                                     item.FullDayMinutes.ToString(CultureInfo.InvariantCulture), If(item.IsActive, "Active", "Inactive")}))
        Next
        Return OperationResult(Of IReadOnlyList(Of RosterRow)).Ok(rows)
    End Function

    Protected Overrides Sub ShowRow(id As Integer)
        Dim item As ShiftItem = Nothing
        If Not _shifts.TryGetValue(id, item) Then
            ClearEditor()
            Return
        End If
        _currentId = id
        _name.Text = item.Name
        _start.Value = Date.Today.Add(item.StartTime)
        _end.Value = Date.Today.Add(item.EndTime)
        _break.Value = item.UnpaidBreakMinutes
        _breakAfter.Value = item.BreakAppliesAfterMinutes
        _fullDay.Value = item.FullDayMinutes
        _halfDay.Value = item.HalfDayMinutes
        _rounding.SelectedValue = CInt(item.OvertimeStep)
        _active.Checked = item.IsActive
    End Sub

    Protected Overrides Sub ClearEditor()
        _currentId = 0
        _name.Clear()
        _start.Value = Date.Today.AddHours(9)
        _end.Value = Date.Today.AddHours(18)
        _break.Value = 30
        _breakAfter.Value = 300
        _fullDay.Value = 480
        _halfDay.Value = 240
        _rounding.SelectedIndex = 0
        _active.Checked = True
        _name.Focus()
    End Sub

    Protected Overrides Function SaveEditor() As OperationResult(Of Integer)
        Dim rounding As OvertimeRounding = CType(Convert.ToInt32(_rounding.SelectedValue), OvertimeRounding)
        Dim item As New ShiftItem(If(_currentId = 0, Nothing, CType(_currentId, Integer?)), _name.Text,
                                  _start.Value.TimeOfDay, _end.Value.TimeOfDay, CInt(_break.Value), CInt(_breakAfter.Value),
                                  CInt(_fullDay.Value), CInt(_halfDay.Value), rounding, _active.Checked)
        Return _services.Roster.SaveShift(item)
    End Function
End Class
