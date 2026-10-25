''' <summary>
''' One row of stat.TaxForm or stat.TaxSectionRef (CR 0018): the form number or section reference the law uses for a purpose or topic, from a financial year on.
''' Screens, reports and file names read it through IStatutoryRateService and never type "Form 16" or "192".
''' </summary>
Public NotInheritable Class TaxReference
    Public Sub New(key As String, fromFinancialYear As Integer, value As String, title As String, verified As Boolean, verifiedOn As Date?, sourceRef As String)
        Me.Key = If(key, String.Empty)
        Me.FromFinancialYear = fromFinancialYear
        Me.Value = If(value, String.Empty)
        Me.Title = If(title, String.Empty)
        Me.Verified = verified
        Me.VerifiedOn = verifiedOn
        Me.SourceRef = If(sourceRef, String.Empty)
    End Sub

    Public ReadOnly Property Key As String
    ''' <summary>Start year of the first financial year the row applies to (2026 means 2026-27).</summary>
    Public ReadOnly Property FromFinancialYear As Integer
    ''' <summary>The form number (for example 130) or the section reference (for example 392), without the word Form or Section.</summary>
    Public ReadOnly Property Value As String
    ''' <summary>A short description, filled for forms and empty for section references.</summary>
    Public ReadOnly Property Title As String
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property VerifiedOn As Date?
    Public ReadOnly Property SourceRef As String
End Class
