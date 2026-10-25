Imports Revolution.Core

''' <summary>
''' HRA exemption, old regime only. Each month: the least of HRA received, rent paid minus a share of salary, and the city percentage of salary.
''' Salary is basic plus DA. The city percentage comes from the HRA city list in force (HraMetroList), the share of salary is passed in
''' (10 by the rule; it has no stat table yet, see CR 0018), so no percentage is typed here. A city not in the list gets the "other cities" row.
''' The year is the sum of the months, rounded once to the nearest rupee, midpoint away from zero. The caller does not call this for the new regime.
''' </summary>
Public NotInheritable Class HraExemptionCalculator
    Private Sub New()
    End Sub

    Public Shared Function Annual(months As IEnumerable(Of HraMonthInput), cities As HraMetroList, rentSharePct As Decimal) As HraExemptionResult
        If months Is Nothing Then
            Throw New ArgumentNullException(NameOf(months))
        End If
        If cities Is Nothing Then
            Throw New ArgumentNullException(NameOf(cities))
        End If
        If rentSharePct < 0D OrElse rentSharePct > 100D Then
            Throw New ArgumentOutOfRangeException(NameOf(rentSharePct), "The share of salary is a percentage from 0 to 100.")
        End If
        Dim lines As New List(Of HraMonthResult)()
        Dim total As Decimal = 0D
        For Each month As HraMonthInput In months
            Dim line As HraMonthResult = Monthly(month, cities, rentSharePct)
            lines.Add(line)
            total += line.Exempt
        Next
        Return New HraExemptionResult(Math.Round(total, 0, MidpointRounding.AwayFromZero), lines)
    End Function

    Public Shared Function Monthly(month As HraMonthInput, cities As HraMetroList, rentSharePct As Decimal) As HraMonthResult
        Dim overShare As Decimal = Math.Max(0D, month.RentPaid - Rupee.Percent(month.BasicPlusDa, rentSharePct))
        Dim cityLimit As Decimal = Rupee.Percent(month.BasicPlusDa, cities.ExemptPctFor(month.City))
        Dim exempt As Decimal = Math.Min(month.HraReceived, Math.Min(overShare, cityLimit))
        Return New HraMonthResult(month.Month, month.HraReceived, overShare, cityLimit, exempt)
    End Function
End Class
