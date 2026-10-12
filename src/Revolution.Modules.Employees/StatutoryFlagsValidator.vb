''' <summary>Checks the statutory flags that the database cannot check on its own.</summary>
Public NotInheritable Class StatutoryFlagsValidator
    Public Const MaximumPfNumberLength As Integer = 30
    Public Const MaximumDispensaryLength As Integer = 100

    Private Sub New()
    End Sub

    Public Shared Function Validate(flags As StatutoryFlags, today As Date) As IReadOnlyList(Of String)
        If flags Is Nothing Then
            Throw New ArgumentNullException(NameOf(flags))
        End If
        Dim problems As New List(Of String)
        If flags.TaxRegimeId <> StatutoryFlags.OldTaxRegime AndAlso flags.TaxRegimeId <> StatutoryFlags.NewTaxRegime Then
            problems.Add("Choose the old or the new tax regime.")
        End If
        If flags.HigherWageOption AndAlso Not flags.IsEpfMember Then
            problems.Add("Higher wage option needs EPF membership.")
        End If
        If flags.EpfJoinDate.HasValue AndAlso flags.EpfJoinDate.Value.Date > today.Date Then
            problems.Add("EPF joining date cannot be in the future.")
        End If
        If If(flags.PfNumber, String.Empty).Trim().Length > MaximumPfNumberLength Then
            problems.Add("PF number is too long.")
        End If
        If If(flags.EsiDispensary, String.Empty).Trim().Length > MaximumDispensaryLength Then
            problems.Add("ESI dispensary name is too long.")
        End If
        Return problems
    End Function
End Class
