''' <summary>A planned settlement: the lines, their totals, the window it is due in, and plain notes on what was left out and why (not eligible, not covered, not applicable).</summary>
Public NotInheritable Class FnfPlan
    Public Sub New(lines As IReadOnlyList(Of FnfLine), totals As FnfTotals, dueWindow As RunPeriod, notes As IReadOnlyList(Of String))
        Me.Lines = lines
        Me.Totals = totals
        Me.DueWindow = dueWindow
        Me.Notes = notes
    End Sub

    Public ReadOnly Property Lines As IReadOnlyList(Of FnfLine)
    Public ReadOnly Property Totals As FnfTotals
    ''' <summary>The 1st to the 7th of the month after exit.</summary>
    Public ReadOnly Property DueWindow As RunPeriod
    Public ReadOnly Property Notes As IReadOnlyList(Of String)
End Class
