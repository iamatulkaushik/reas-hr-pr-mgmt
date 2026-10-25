''' <summary>
''' A single number a tax rule needs that is neither a slab nor a rupee cap (stat.TdsRuleValue, CR 0018 addendum): for example the share of salary that
''' rent must exceed for HRA (HRA_RENT_SHARE_PCT, 10) or the yearly rent above which the landlord's PAN is needed (LANDLORD_PAN_RENT, 100000).
''' The row in force is the latest FromFinancialYear on or before the financial year asked. Rows are signed off by the CA like the other rates.
''' </summary>
Public NotInheritable Class TdsRuleValue
    Public Sub New(valueKey As String, fromFinancialYear As Integer, amount As Decimal, note As String, verified As Boolean, verifiedOn As Date?, sourceRef As String)
        If String.IsNullOrWhiteSpace(valueKey) Then
            Throw New ArgumentException("A rule value needs a key.", NameOf(valueKey))
        End If
        Me.ValueKey = valueKey.Trim()
        Me.FromFinancialYear = fromFinancialYear
        Me.Amount = amount
        Me.Note = If(note, String.Empty)
        Me.Verified = verified
        Me.VerifiedOn = verifiedOn
        Me.SourceRef = If(sourceRef, String.Empty)
    End Sub

    ''' <summary>For example HRA_RENT_SHARE_PCT or LANDLORD_PAN_RENT.</summary>
    Public ReadOnly Property ValueKey As String
    ''' <summary>Start year of the first financial year the row applies to (2026 means 2026-27).</summary>
    Public ReadOnly Property FromFinancialYear As Integer
    ''' <summary>A percent (10 means 10 percent) or a rupee amount, as the key says.</summary>
    Public ReadOnly Property Amount As Decimal
    Public ReadOnly Property Note As String
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property VerifiedOn As Date?
    Public ReadOnly Property SourceRef As String
End Class
