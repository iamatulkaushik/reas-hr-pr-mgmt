Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>Holiday calendar of the company for one year. A holiday is paid in attendance and in the month summary.</summary>
Friend NotInheritable Class HolidayForm
    Inherits RosterFormBase

    Private Const DateFormat As String = "dd-MM-yyyy"
    Private Const MinYear As Integer = 1900
    Private Const MaxYear As Integer = 9999

    Private ReadOnly _services As AttendanceServices
    Private ReadOnly _holidays As New Dictionary(Of Integer, HolidayItem)()
    Private ReadOnly _year As New NumericUpDown() With {.Minimum = MinYear, .Maximum = MaxYear, .Value = Date.Today.Year, .Width = 70}
    Private ReadOnly _day As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = DateFormat}
    Private ReadOnly _holidayName As New TextBox() With {.MaxLength = 100}
    Private _currentDay As Date?

    Public Sub New(services As AttendanceServices, canEdit As Boolean)
        MyBase.New("Holidays", "Attendance", New String() {"Date", "Day", "Holiday"}, canEdit, True)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
    End Sub

    Protected Overrides Sub BuildEditor(layout As TableLayoutPanel)
        TopBar.Controls.Add(New Label With {.Text = "Year", .AutoSize = True, .Margin = New Padding(0, 6, 4, 0)})
        TopBar.Controls.Add(_year)
        AddHandler _year.ValueChanged, Sub(sender As Object, e As EventArgs) YearChanged()
        AddField(layout, "Date", _day)
        AddField(layout, "Holiday name", _holidayName)
        ClearEditor()
    End Sub

    Private Sub YearChanged()
        ReloadList()
    End Sub

    Protected Overrides Function LoadRows() As OperationResult(Of IReadOnlyList(Of RosterRow))
        Dim result = _services.Roster.ListHolidays(CInt(_year.Value))
        If Not result.Succeeded Then Return OperationResult(Of IReadOnlyList(Of RosterRow)).Fail(result.Message)
        _holidays.Clear()
        Dim rows As New List(Of RosterRow)()
        Dim index As Integer = 0
        For Each item In result.Value
            index += 1
            _holidays(index) = item
            rows.Add(New RosterRow(index, New String() {item.Day.ToString(DateFormat, CultureInfo.InvariantCulture),
                                                        item.Day.DayOfWeek.ToString(), item.Name}))
        Next
        Return OperationResult(Of IReadOnlyList(Of RosterRow)).Ok(rows)
    End Function

    Protected Overrides Sub ShowRow(id As Integer)
        Dim item As HolidayItem = Nothing
        If Not _holidays.TryGetValue(id, item) Then
            ClearEditor()
            Return
        End If
        _currentDay = item.Day
        _day.Value = item.Day
        _day.Enabled = False
        _holidayName.Text = item.Name
    End Sub

    Protected Overrides Sub ClearEditor()
        _currentDay = Nothing
        _day.Enabled = True
        Dim today As Date = Date.Today
        _day.Value = If(today.Year = CInt(_year.Value), today, New Date(CInt(_year.Value), 1, 1))
        _holidayName.Clear()
        _holidayName.Focus()
    End Sub

    Protected Overrides Function SaveEditor() As OperationResult(Of Integer)
        Dim saved = _services.Roster.SaveHoliday(_day.Value.Date, _holidayName.Text)
        If Not saved.Succeeded Then Return OperationResult(Of Integer).Fail(saved.Message)
        Return OperationResult(Of Integer).Ok(0)
    End Function

    Protected Overrides Function DeleteRow(id As Integer) As OperationResult
        Dim item As HolidayItem = Nothing
        If Not _holidays.TryGetValue(id, item) Then Return OperationResult.Fail("Select a holiday first.")
        Return _services.Roster.DeleteHoliday(item.Day)
    End Function
End Class
