''' <summary>
''' Relief under section 89(1) for arrears (Owner, 2026-11-01). Same method as Form 10E: A is the extra tax this year because the arrears came now;
''' B is the extra tax there would have been had each part come in its own year; relief is A less B when A is more. Only arrears of earlier years are passed in;
''' arrears of the current year are ordinary current income. Tax comes from delegates, so every year uses its own slabs and regime. Pure.
''' </summary>
Public NotInheritable Class Section89ReliefCalculator
    Private Sub New()
    End Sub

    Public Shared Function Calculate(currentIncomeWithout As Decimal, currentTaxOn As Func(Of Decimal, Decimal), years As IReadOnlyList(Of Section89Year)) As Section89Result
        If currentTaxOn Is Nothing Then Throw New ArgumentNullException(NameOf(currentTaxOn))
        If years Is Nothing Then Throw New ArgumentNullException(NameOf(years))
        If currentIncomeWithout < 0D Then Throw New ArgumentOutOfRangeException(NameOf(currentIncomeWithout), "Income cannot be negative.")
        Dim totalArrears As Decimal = years.Sum(Function(y As Section89Year) y.Arrears)
        Dim thisYear As Decimal = currentTaxOn(currentIncomeWithout + totalArrears) - currentTaxOn(currentIncomeWithout)
        Dim ownYears As New List(Of Decimal)()
        For Each y As Section89Year In years
            ownYears.Add(y.TaxOn(y.IncomeWithout + y.Arrears) - y.TaxOn(y.IncomeWithout))
        Next
        Return New Section89Result(thisYear, ownYears)
    End Function
End Class
