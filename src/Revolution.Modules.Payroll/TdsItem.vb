''' <summary>One declared deduction. Items under one section are added together and capped by the section limit when tax is worked out. VerifiedAmount, when set, replaces the declared amount.</summary>
Public NotInheritable Class TdsItem
    Public Sub New(sectionCode As String, subLimitKey As String, description As String, declaredAmount As Decimal, proofRef As String, verifiedAmount As Decimal?)
        Me.SectionCode = If(sectionCode, String.Empty).Trim()
        Me.SubLimitKey = If(subLimitKey, String.Empty).Trim()
        Me.Description = description
        Me.DeclaredAmount = declaredAmount
        Me.ProofRef = proofRef
        Me.VerifiedAmount = verifiedAmount
    End Sub

    Public ReadOnly Property SectionCode As String
    Public ReadOnly Property SubLimitKey As String
    Public ReadOnly Property Description As String
    Public ReadOnly Property DeclaredAmount As Decimal
    Public ReadOnly Property ProofRef As String
    Public ReadOnly Property VerifiedAmount As Decimal?

    ''' <summary>The amount tax is worked on: the verified amount when proof was checked, otherwise the declared amount.</summary>
    Public ReadOnly Property EffectiveAmount As Decimal
        Get
            Return If(VerifiedAmount.HasValue, VerifiedAmount.Value, DeclaredAmount)
        End Get
    End Property
End Class
