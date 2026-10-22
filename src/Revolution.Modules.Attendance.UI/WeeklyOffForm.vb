Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>Weekly off days of the company. A new rule applies from its effective date; older dates keep the older rule.</summary>
Friend NotInheritable Class WeeklyOffForm
    Inherits RosterFormBase

    Private Const DateFormat As String = "dd-MM-yyyy"

    Private Shared ReadOnly WeekDays As DayOfWeek() = {DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
                                                       DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday}

    Private ReadOnly _services As AttendanceServices
    Private ReadOnly _rules As New Dictionary(Of Integer, WeeklyOffRule)()
    Private ReadOnly _from As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = DateFormat}
    Private ReadOnly _boxes As New List(Of CheckBox)()

    Public Sub New(services As AttendanceServices, canEdit As Boolean)
        MyBase.New("Weekly off", "Attendance", New String() {"Effective from", "Weekly off days"}, canEdit, False)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
    End Sub

    Protected Overrides Sub BuildEditor(layout As TableLayoutPanel)
        AddField(layout, "Effective from", _from)
        For Each offDay In WeekDays
            Dim box As New CheckBox With {.Text = offDay.ToString(), .AutoSize = True}
            _boxes.Add(box)
            AddField(layout, String.Empty, box)
        Next
        ClearEditor()
    End Sub

    Protected Overrides Function LoadRows() As OperationResult(Of IReadOnlyList(Of RosterRow))
        Dim result = _services.Roster.ListWeeklyOff()
        If Not result.Succeeded Then Return OperationResult(Of IReadOnlyList(Of RosterRow)).Fail(result.Message)
        _rules.Clear()
        Dim rows As New List(Of RosterRow)()
        Dim index As Integer = 0
        For Each rule In result.Value
            index += 1
            _rules(index) = rule
            rows.Add(New RosterRow(index, New String() {rule.EffectiveFrom.ToString(DateFormat, CultureInfo.InvariantCulture), DaysText(rule)}))
        Next
        Return OperationResult(Of IReadOnlyList(Of RosterRow)).Ok(rows)
    End Function

    Private Shared Function DaysText(rule As WeeklyOffRule) As String
        Dim names As New List(Of String)()
        For Each offDay In WeekDays
            If rule.IsOff(offDay) Then names.Add(offDay.ToString())
        Next
        Return If(names.Count = 0, "None", String.Join(", ", names))
    End Function

    Protected Overrides Sub ShowRow(id As Integer)
        Dim rule As WeeklyOffRule = Nothing
        If Not _rules.TryGetValue(id, rule) Then
            ClearEditor()
            Return
        End If
        _from.Value = rule.EffectiveFrom
        For i As Integer = 0 To WeekDays.Length - 1
            _boxes(i).Checked = rule.IsOff(WeekDays(i))
        Next
    End Sub

    Protected Overrides Sub ClearEditor()
        _from.Value = Date.Today
        For Each box In _boxes
            box.Checked = False
        Next
        If _boxes.Count > 0 Then _boxes(_boxes.Count - 1).Checked = True
        _from.Focus()
    End Sub

    ''' <summary>Monday is bit 0 and Sunday bit 6, the same order WeeklyOffRule.IsOff reads.</summary>
    Protected Overrides Function SaveEditor() As OperationResult(Of Integer)
        Dim mask As Integer = 0
        For i As Integer = 0 To _boxes.Count - 1
            If _boxes(i).Checked Then mask = mask Or (1 << i)
        Next
        Dim saved = _services.Roster.SaveWeeklyOff(_from.Value.Date, mask)
        If Not saved.Succeeded Then Return OperationResult(Of Integer).Fail(saved.Message)
        Return OperationResult(Of Integer).Ok(0)
    End Function
End Class
