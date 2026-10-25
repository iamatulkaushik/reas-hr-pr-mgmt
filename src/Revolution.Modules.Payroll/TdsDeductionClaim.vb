''' <summary>One declared deduction for TDS: a section, its sub-limit and the amount declared. Several claims under one section are added together and then capped.</summary>
Public NotInheritable Class TdsDeductionClaim
    Public Sub New(sectionCode As String, subLimitKey As String, amount As Decimal)
        If String.IsNullOrWhiteSpace(sectionCode) Then
            Throw New ArgumentException("A deduction claim needs a section code.", NameOf(sectionCode))
        End If
        If amount < 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(amount), "Amount cannot be negative.")
        End If
        Me.SectionCode = sectionCode.Trim()
        Me.SubLimitKey = If(subLimitKey, String.Empty).Trim()
        Me.Amount = amount
    End Sub

    Public ReadOnly Property SectionCode As String
    Public ReadOnly Property SubLimitKey As String
    Public ReadOnly Property Amount As Decimal
End Class
