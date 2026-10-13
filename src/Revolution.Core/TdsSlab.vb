''' <summary>One income tax band. ToAmount is Nothing for the open top band. RatePct is a percentage, for example 5 means 5 percent.</summary>
Public NotInheritable Class TdsSlab
    Public Sub New(slabNo As Integer, fromAmount As Decimal, toAmount As Decimal?, ratePct As Decimal)
        Me.SlabNo = slabNo
        Me.FromAmount = fromAmount
        Me.ToAmount = toAmount
        Me.RatePct = ratePct
    End Sub

    Public ReadOnly Property SlabNo As Integer
    Public ReadOnly Property FromAmount As Decimal
    Public ReadOnly Property ToAmount As Decimal?
    Public ReadOnly Property RatePct As Decimal
End Class
