''' <summary>One earlier financial year that arrears belong to, for the section 89 relief. IncomeWithout is that year's taxable income before the arrears. TaxOn works out the year's tax, cess included, on a taxable income under that year's rules.</summary>
Public NotInheritable Class Section89Year
    Public Sub New(label As String, incomeWithout As Decimal, arrears As Decimal, taxOn As Func(Of Decimal, Decimal))
        If taxOn Is Nothing Then Throw New ArgumentNullException(NameOf(taxOn))
        If incomeWithout < 0D Then Throw New ArgumentOutOfRangeException(NameOf(incomeWithout), "Income cannot be negative.")
        If arrears < 0D Then Throw New ArgumentOutOfRangeException(NameOf(arrears), "Arrears cannot be negative.")
        Me.Label = label
        Me.IncomeWithout = incomeWithout
        Me.Arrears = arrears
        Me.TaxOn = taxOn
    End Sub

    Public ReadOnly Property Label As String
    Public ReadOnly Property IncomeWithout As Decimal
    Public ReadOnly Property Arrears As Decimal
    Public ReadOnly Property TaxOn As Func(Of Decimal, Decimal)
End Class
