Imports System.Globalization
Imports Revolution.Core

''' <summary>Builds a month summary from marked days. Rejects duplicate days and days outside the month.</summary>
Public NotInheritable Class MonthSummaryBuilder
    Private Const FirstMonth As Integer = 1
    Private Const LastMonth As Integer = 12
    Private Const DateFormat As String = "dd-MM-yyyy"

    Private Sub New()
    End Sub

    Public Shared Function Build(year As Integer, month As Integer, records As IEnumerable(Of DayRecord)) As OperationResult(Of MonthSummary)
        If month < FirstMonth OrElse month > LastMonth OrElse year < 1 OrElse year > 9999 Then
            Return OperationResult(Of MonthSummary).Fail("Month or year is not valid.")
        End If
        If records Is Nothing Then
            Throw New ArgumentNullException(NameOf(records))
        End If
        Dim counts As New Dictionary(Of AttendanceStatus, Integer)()
        Dim seen As New HashSet(Of Date)()
        Dim overtime As Integer = 0
        For Each r As DayRecord In records
            If r.Day.Year <> year OrElse r.Day.Month <> month Then
                Return OperationResult(Of MonthSummary).Fail($"{r.Day.ToString(DateFormat, CultureInfo.InvariantCulture)} is outside the month.")
            End If
            If Not seen.Add(r.Day) Then
                Return OperationResult(Of MonthSummary).Fail($"{r.Day.ToString(DateFormat, CultureInfo.InvariantCulture)} is marked twice.")
            End If
            counts(r.Status) = If(counts.ContainsKey(r.Status), counts(r.Status), 0) + 1
            overtime += r.OvertimeMinutes
        Next
        Dim unmarked As Integer = DateTime.DaysInMonth(year, month) - seen.Count
        Return OperationResult(Of MonthSummary).Ok(New MonthSummary(year, month, counts, overtime, unmarked))
    End Function
End Class
