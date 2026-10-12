''' <summary>EPF, ESI, PT, LWF and tax settings for one employee. Defaults match the database defaults for a row not yet saved.</summary>
Public NotInheritable Class StatutoryFlags
    Public Const OldTaxRegime As Integer = 1
    Public Const NewTaxRegime As Integer = 2

    Public Property IsEpfMember As Boolean
    Public Property PfNumber As String = String.Empty
    Public Property EpfJoinDate As Date?
    Public Property IsPensionMember As Boolean = True
    Public Property HigherWageOption As Boolean
    Public Property IsEsiMember As Boolean
    Public Property EsiDispensary As String = String.Empty
    Public Property PtApplicable As Boolean = True
    Public Property LwfApplicable As Boolean = True
    ''' <summary>1 old regime, 2 new regime.</summary>
    Public Property TaxRegimeId As Integer = NewTaxRegime
    Public Property IsInternationalWorker As Boolean
End Class
