Imports System.Globalization

''' <summary>The Monthly Pack summary workbook: one sheet with a line per statutory item (amount, due date, status, reference) and one with the parts of each amount.</summary>
Public NotInheritable Class MonthlyPackSheetBuilder
    Public Const SummarySheetName As String = "Monthly Pack"
    Public Const BreakdownSheetName As String = "Breakdown"

    Private Sub New()
    End Sub

    Public Shared Function Build(pack As MonthlyPack) As IReadOnlyList(Of ExportSheet)
        If pack Is Nothing Then
            Throw New ArgumentNullException(NameOf(pack))
        End If
        Dim summary As New List(Of String())()
        summary.Add(New String() {"Statutory item", "State", "Employees", "Amount payable", "Due date", "Status", "Reference"})
        Dim parts As New List(Of String())()
        parts.Add(New String() {"Statutory item", "State", "Part", "Amount"})
        For Each line In pack.Lines
            summary.Add(New String() {line.Title, line.StateName, line.EmployeeCount.ToString(CultureInfo.InvariantCulture), AmountText(line.AmountPayable), DueText(line),
                                      StatusText(line.Status), line.Reference})
            For Each part In line.Components
                parts.Add(New String() {line.Title, line.StateName, part.Label, AmountText(part.Amount)})
            Next
        Next
        Return New List(Of ExportSheet) From {New ExportSheet(SummarySheetName, summary), New ExportSheet(BreakdownSheetName, parts)}
    End Function

    ''' <summary>Plain words for a status, for the sheet and for the screen.</summary>
    Public Shared Function StatusText(status As PackStatus) As String
        Select Case status
            Case PackStatus.NotReady
                Return "Not ready"
            Case PackStatus.Ready
                Return "Ready"
            Case PackStatus.Generated
                Return "Generated"
            Case PackStatus.Filed
                Return "Filed"
            Case PackStatus.Paid
                Return "Paid"
            Case PackStatus.Changed
                Return "Changed, make again"
            Case Else
                Return status.ToString()
        End Select
    End Function

    Private Shared Function AmountText(amount As Decimal?) As String
        Return If(amount.HasValue, amount.Value.ToString("0.00", CultureInfo.InvariantCulture), String.Empty)
    End Function

    Private Shared Function AmountText(amount As Decimal) As String
        Return amount.ToString("0.00", CultureInfo.InvariantCulture)
    End Function

    Private Shared Function DueText(line As MonthlyPackLine) As String
        If line.DueDate.HasValue Then
            Return line.DueDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        End If
        Return If(line.DueNote.Length = 0, "Date not set", line.DueNote)
    End Function
End Class
