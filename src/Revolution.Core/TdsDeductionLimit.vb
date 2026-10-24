''' <summary>
''' The rupee cap for one TDS deduction section and sub-limit in a financial year and regime (stat.TdsDeductionLimit, CR 0012).
''' SubLimitKey is empty when the section has one cap. A section with no row is not allowed: treat it as zero, never as unlimited.
''' </summary>
Public NotInheritable Class TdsDeductionLimit
    Public Sub New(financialYear As Integer, regime As TdsRegime, sectionCode As String, subLimitKey As String, maxAmount As Decimal,
                   note As String, verified As Boolean, verifiedOn As Date?, sourceRef As String)
        If String.IsNullOrWhiteSpace(sectionCode) Then
            Throw New ArgumentException("A deduction limit needs a section code.", NameOf(sectionCode))
        End If
        Me.FinancialYear = financialYear
        Me.Regime = regime
        Me.SectionCode = sectionCode.Trim()
        Me.SubLimitKey = If(subLimitKey, String.Empty).Trim()
        Me.MaxAmount = maxAmount
        Me.Note = If(note, String.Empty)
        Me.Verified = verified
        Me.VerifiedOn = verifiedOn
        Me.SourceRef = If(sourceRef, String.Empty)
    End Sub

    ''' <summary>The financial year as its start year (2026 is 2026-27).</summary>
    Public ReadOnly Property FinancialYear As Integer
    Public ReadOnly Property Regime As TdsRegime
    ''' <summary>For example 80C, 80D, 80CCD1B, 24b.</summary>
    Public ReadOnly Property SectionCode As String
    ''' <summary>Tells apart several caps under one section, for example self and senior parent. Empty when there is one.</summary>
    Public ReadOnly Property SubLimitKey As String
    Public ReadOnly Property MaxAmount As Decimal
    Public ReadOnly Property Note As String
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property VerifiedOn As Date?
    Public ReadOnly Property SourceRef As String
End Class
