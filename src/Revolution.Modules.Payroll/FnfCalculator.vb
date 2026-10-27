''' <summary>
''' Adds up the lines of a settlement. Deductions are notice pay recovery, loan closure, TDS shortfall, arrears recovery (an Owner option) and other deductions.
''' Everything else is paid to the employee. The lines are already whole rupees, so the totals are too.
''' </summary>
Public NotInheritable Class FnfCalculator
    Private Sub New()
    End Sub

    Public Shared Function IsDeduction(kind As FnfComponentKind) As Boolean
        Select Case kind
            Case FnfComponentKind.NoticePayRecovery, FnfComponentKind.LoanClosure, FnfComponentKind.TdsShortfall,
                 FnfComponentKind.ArrearsRecovery, FnfComponentKind.OtherDeduction
                Return True
            Case Else
                Return False
        End Select
    End Function

    Public Shared Function Total(lines As IReadOnlyList(Of FnfLine)) As FnfTotals
        If lines Is Nothing Then Throw New ArgumentNullException(NameOf(lines))
        Dim earnings As Decimal = 0D
        Dim deductions As Decimal = 0D
        For Each line As FnfLine In lines
            If line.IsDeduction Then deductions += line.Amount Else earnings += line.Amount
        Next
        Return New FnfTotals(earnings, deductions)
    End Function
End Class
