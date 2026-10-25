''' <summary>
''' The employer's NPS contribution that is exempt from TDS under section 80CCD(2), as a percent of basic plus DA, for a financial year and regime
''' (stat.TdsEmployerNps, CR 0018). It is a percent, not a rupee cap, so it is not a TdsDeductionLimit. The payroll module works out the rupee cap from it.
''' </summary>
Public NotInheritable Class TdsEmployerNps
    Public Sub New(financialYear As Integer, regime As TdsRegime, pctOfBasicDa As Decimal, note As String, verified As Boolean, verifiedOn As Date?, sourceRef As String)
        If pctOfBasicDa < 0D OrElse pctOfBasicDa > 100D Then
            Throw New ArgumentOutOfRangeException(NameOf(pctOfBasicDa), "The employer NPS percent must be between 0 and 100.")
        End If
        Me.FinancialYear = financialYear
        Me.Regime = regime
        Me.PctOfBasicDa = pctOfBasicDa
        Me.Note = If(note, String.Empty)
        Me.Verified = verified
        Me.VerifiedOn = verifiedOn
        Me.SourceRef = If(sourceRef, String.Empty)
    End Sub

    ''' <summary>The financial year as its start year (2026 is 2026-27).</summary>
    Public ReadOnly Property FinancialYear As Integer
    Public ReadOnly Property Regime As TdsRegime
    ''' <summary>The exempt percent of basic plus DA (for example 14).</summary>
    Public ReadOnly Property PctOfBasicDa As Decimal
    Public ReadOnly Property Note As String
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property VerifiedOn As Date?
    Public ReadOnly Property SourceRef As String
End Class
