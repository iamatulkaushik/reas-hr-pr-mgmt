''' <summary>
''' Final tax true-up at exit. The employee leaves part way through the financial year, so the year's tax is worked on the salary actually earned, with every month after the
''' exit month at zero, and compared with the tax already deducted. A positive adjustment is a shortfall to take in F&amp;F; a negative one is a refund. The projection itself is
''' TdsProjectionService.Project, run for the exit month. Whole rupees, as the projection is.
''' </summary>
Public NotInheritable Class FnfTdsTrueUp
    Private Sub New()
    End Sub

    ''' <summary>
    ''' The twelve months April to March for the projection. Months up to and including the exit month take their facts from earned (by YYYYMM), and a month with no entry counts as zero.
    ''' Months after the exit month are always zero, even if an entry is given.
    ''' </summary>
    Public Shared Function MonthsForExit(financialYear As Integer, exitMonthKey As Integer, earned As IReadOnlyDictionary(Of Integer, TdsMonthFacts)) As IReadOnlyList(Of TdsMonthFacts)
        If earned Is Nothing Then Throw New ArgumentNullException(NameOf(earned))
        Dim first As Integer = financialYear * 100 + 4
        If exitMonthKey < first OrElse exitMonthKey > (financialYear + 1) * 100 + 3 Then Throw New ArgumentOutOfRangeException(NameOf(exitMonthKey), "The exit month is outside the financial year.")
        Dim months As New List(Of TdsMonthFacts)(12)
        For index As Integer = 0 To 11
            Dim monthKey As Integer = If(index < 9, first + index, (financialYear + 1) * 100 + index - 8)
            Dim facts As TdsMonthFacts = Nothing
            If monthKey <= exitMonthKey AndAlso earned.TryGetValue(monthKey, facts) Then
                months.Add(New TdsMonthFacts(monthKey, facts.BasicPlusDa, facts.HraReceived, facts.TaxableSalary))
            Else
                months.Add(New TdsMonthFacts(monthKey, 0D, 0D, 0D))
            End If
        Next
        Return months
    End Function

    ''' <summary>Year's tax less the tax already deducted (this employer and any previous one). Positive is a shortfall, negative a refund.</summary>
    Public Shared Function Adjustment(projection As TdsProjectionResult) As Decimal
        If projection Is Nothing Then Throw New ArgumentNullException(NameOf(projection))
        Return projection.Annual.TotalTax - projection.TaxDeductedBefore
    End Function
End Class
