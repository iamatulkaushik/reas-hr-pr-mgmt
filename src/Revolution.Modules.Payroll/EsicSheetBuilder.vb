Imports System.Globalization

''' <summary>Builds the ESIC monthly contribution sheet as text cells. Every cell is Text, as the portal requires.</summary>
Public NotInheritable Class EsicSheetBuilder
    ''' <summary>Header text exactly as in the ESIC template.</summary>
    Public Shared ReadOnly Header As String() = {
        "IP Number " & vbLf & "(10 Digits)",
        "IP Name" & vbLf & "( Only alphabets and space )",
        "No of Days for which wages paid/payable during the month",
        "Total Monthly Wages",
        " Reason Code for Zero workings days(numeric only; provide 0 for all other reasons- Click on the link for reference)",
        " Last Working Day" & vbLf & "( Format DD/MM/YYYY  or DD-MM-YYYY)"}

    Private Sub New()
    End Sub

    Public Shared Function Build(rows As IReadOnlyCollection(Of EsicRow), lastDayCodes As IReadOnlyCollection(Of Integer), wageMonth As Integer) As IReadOnlyList(Of String())
        Dim problems As IReadOnlyList(Of String) = EsicValidator.Validate(rows, lastDayCodes, wageMonth)
        If problems.Count > 0 Then
            Throw New InvalidOperationException("ESIC sheet is not valid: " & String.Join(" ", problems))
        End If
        Dim table As New List(Of String())()
        table.Add(Header)
        For Each r As EsicRow In rows
            table.Add(Cells(r))
        Next
        Return table
    End Function

    Public Shared Function Cells(r As EsicRow) As String()
        Return {
            r.IpNumber,
            EcrNameCleaner.Clean(r.IpName),
            r.DaysPaid.ToString(CultureInfo.InvariantCulture),
            Decimal.Truncate(r.TotalWages).ToString(CultureInfo.InvariantCulture),
            EsicValidator.EffectiveReason(r).ToString(CultureInfo.InvariantCulture),
            If(r.LastWorkingDay.HasValue, r.LastWorkingDay.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), String.Empty)
        }
    End Function
End Class
