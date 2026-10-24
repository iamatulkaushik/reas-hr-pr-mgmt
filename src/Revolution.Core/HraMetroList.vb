''' <summary>
''' The HRA city list in force on a date: the listed cities and the row for every other city (CityName "*"). The percentages come from the
''' table, never from code. IStatutoryRateService.GetHraMetroList only returns a list that has the "*" row.
''' </summary>
Public NotInheritable Class HraMetroList
    Private ReadOnly _byCity As New Dictionary(Of String, HraCity)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly _others As HraCity

    Public Sub New(cities As IEnumerable(Of HraCity))
        If cities Is Nothing Then
            Throw New ArgumentNullException(NameOf(cities))
        End If
        Dim list = cities.ToList()
        Rows = list
        For Each entry As HraCity In list
            If entry.IsOtherCities Then
                _others = entry
            Else
                _byCity(entry.CityName) = entry
            End If
        Next
        If _others Is Nothing Then
            Throw New ArgumentException("An HRA city list needs a row for other cities (CityName '*').", NameOf(cities))
        End If
        EffectiveFrom = list.Min(Function(item) item.EffectiveFrom)
    End Sub

    Public ReadOnly Property EffectiveFrom As Date
    Public ReadOnly Property Rows As IReadOnlyList(Of HraCity)

    ''' <summary>The row for the city, ignoring case and surrounding spaces, or Nothing when the city is not listed.</summary>
    Public Function Find(cityName As String) As HraCity
        Dim found As HraCity = Nothing
        If cityName IsNot Nothing AndAlso _byCity.TryGetValue(cityName.Trim(), found) Then
            Return found
        End If
        Return Nothing
    End Function

    ''' <summary>True when the city is listed, so a screen can warn about a city that matches no row.</summary>
    Public Function IsListed(cityName As String) As Boolean
        Return Find(cityName) IsNot Nothing
    End Function

    ''' <summary>The exempt percentage for the city: its own row, or the row for other cities when it is not listed.</summary>
    Public Function ExemptPctFor(cityName As String) As Decimal
        Dim row = If(Find(cityName), _others)
        Return row.ExemptPct
    End Function
End Class
