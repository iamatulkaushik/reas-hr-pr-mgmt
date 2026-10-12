''' <summary>One professional tax band. ToAmount is Nothing for the open top band. GenderId: 0 all, 1 male, 2 female.</summary>
Public NotInheritable Class PtSlab
    Public Sub New(genderId As Integer, slabNo As Integer, fromAmount As Decimal, toAmount As Decimal?, taxAmount As Decimal, specialMonthTax As Decimal?)
        Me.GenderId = genderId
        Me.SlabNo = slabNo
        Me.FromAmount = fromAmount
        Me.ToAmount = toAmount
        Me.TaxAmount = taxAmount
        Me.SpecialMonthTax = specialMonthTax
    End Sub

    Public ReadOnly Property GenderId As Integer
    Public ReadOnly Property SlabNo As Integer
    Public ReadOnly Property FromAmount As Decimal
    Public ReadOnly Property ToAmount As Decimal?
    Public ReadOnly Property TaxAmount As Decimal
    ''' <summary>The tax for the state's special month (for example February), when it differs. Nothing means the normal amount.</summary>
    Public ReadOnly Property SpecialMonthTax As Decimal?
End Class
