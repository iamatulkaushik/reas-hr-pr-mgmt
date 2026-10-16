''' <summary>Works out a licence's state on a day (ADR 0018). Pass the date from SQL Server, never the PC's clock.</summary>
Public NotInheritable Class LicenceEvaluator
    Public Const WarningDays As Integer = 30
    Public Const GraceDays As Integer = 15
    ''' <summary>The trial runs this many days from the installation date, then the app is read-only.</summary>
    Public Const TrialDays As Integer = 28

    Private Sub New()
    End Sub

    Public Shared Function StateOn(grant As LicenceGrant, today As DateOnly) As LicenceState
        If grant Is Nothing Then Throw New ArgumentNullException(NameOf(grant))
        If today.DayNumber < grant.ValidFrom.DayNumber Then
            Return LicenceState.NotYetValid
        End If
        Dim remaining = LicenceEvaluator.DaysLeft(grant, today)
        If remaining > WarningDays Then
            Return LicenceState.Valid
        End If
        If remaining >= 0 Then
            Return LicenceState.Expiring
        End If
        If remaining >= -GraceDays Then
            Return LicenceState.Grace
        End If
        Return LicenceState.Expired
    End Function

    ''' <summary>Days up to and including the end date. Zero on the last day, negative afterwards.</summary>
    Public Shared Function DaysLeft(grant As LicenceGrant, today As DateOnly) As Integer
        If grant Is Nothing Then Throw New ArgumentNullException(NameOf(grant))
        Return grant.ValidTo.DayNumber - today.DayNumber
    End Function
End Class
