''' <summary>One surcharge band on the tax, by total income. ToIncome is Nothing for the open top band. RatePct is a percentage.</summary>
Public NotInheritable Class TdsSurchargeBand
    Public Sub New(slabNo As Integer, fromIncome As Decimal, toIncome As Decimal?, ratePct As Decimal)
        Me.SlabNo = slabNo
        Me.FromIncome = fromIncome
        Me.ToIncome = toIncome
        Me.RatePct = ratePct
    End Sub

    Public ReadOnly Property SlabNo As Integer
    Public ReadOnly Property FromIncome As Decimal
    Public ReadOnly Property ToIncome As Decimal?
    Public ReadOnly Property RatePct As Decimal
End Class
