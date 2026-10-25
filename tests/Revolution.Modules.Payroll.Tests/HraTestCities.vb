Imports Revolution.Core

''' <summary>Test fixture shaped like the list from 2026-04-01: eight cities at 50, every other city at 40. Not product data.</summary>
Friend NotInheritable Class HraTestCities
    Private Sub New()
    End Sub

    Public Shared Function From2026() As HraMetroList
        Dim from As New Date(2026, 4, 1)
        Dim rows As New List(Of HraCity)()
        For Each name As String In New String() {"Delhi", "Mumbai", "Kolkata", "Chennai", "Bengaluru", "Hyderabad", "Pune", "Ahmedabad"}
            rows.Add(New HraCity(from, name, True, 50D, True, Nothing, "test"))
        Next
        rows.Add(New HraCity(from, HraCity.OtherCities, False, 40D, True, Nothing, "test"))
        Return New HraMetroList(rows)
    End Function
End Class
