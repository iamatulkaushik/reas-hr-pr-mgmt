''' <summary>One row of the HRA city list (stat.HraMetroCity, CR 0012). CityName "*" is the row for every city that is not listed.</summary>
Public NotInheritable Class HraCity
    ''' <summary>The CityName of the row that gives the percentage for every city not listed.</summary>
    Public Const OtherCities As String = "*"

    Public Sub New(effectiveFrom As Date, cityName As String, isMetro As Boolean, exemptPct As Decimal,
                   verified As Boolean, verifiedOn As Date?, sourceRef As String)
        If String.IsNullOrWhiteSpace(cityName) Then
            Throw New ArgumentException("A city row needs a city name.", NameOf(cityName))
        End If
        Me.EffectiveFrom = effectiveFrom
        Me.CityName = cityName.Trim()
        Me.IsMetro = isMetro
        Me.ExemptPct = exemptPct
        Me.Verified = verified
        Me.VerifiedOn = verifiedOn
        Me.SourceRef = If(sourceRef, String.Empty)
    End Sub

    Public ReadOnly Property EffectiveFrom As Date
    Public ReadOnly Property CityName As String
    Public ReadOnly Property IsMetro As Boolean
    ''' <summary>Percentage of salary that can be exempt for this city (the metro or non-metro percentage in force).</summary>
    Public ReadOnly Property ExemptPct As Decimal
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property VerifiedOn As Date?
    Public ReadOnly Property SourceRef As String

    Public ReadOnly Property IsOtherCities As Boolean
        Get
            Return String.Equals(CityName, OtherCities, StringComparison.Ordinal)
        End Get
    End Property
End Class
