Imports Revolution.Core

''' <summary>Leave earned for a period: worked days times the setting's ratio, rounded as the setting says, then cut so the balance does not pass the cap. Pure.</summary>
Public NotInheritable Class LeaveAccrualCalculator
    Private Sub New()
    End Sub

    ''' <param name="workedDays">Days actually worked in the period (present days, not paid holidays).</param>
    ''' <param name="currentBalance">Balance before this accrual, used only for the cap.</param>
    ''' <returns>Whole days to add to the ledger. Never negative.</returns>
    Public Shared Function Accrue(workedDays As Decimal, currentBalance As Decimal, setting As LeaveAccrualSetting) As Decimal
        If setting Is Nothing Then Throw New ArgumentNullException(NameOf(setting))
        If workedDays < 0D Then Throw New ArgumentOutOfRangeException(NameOf(workedDays), "Worked days cannot be negative.")
        If currentBalance < 0D Then Throw New ArgumentOutOfRangeException(NameOf(currentBalance), "The balance cannot be negative.")
        Dim earned As Decimal = Rupee.Round(workedDays * setting.EarnedDays / setting.PerWorkedDays, setting.Rounding)
        If Not setting.MaxBalance.HasValue Then Return earned
        Return Math.Max(0D, Math.Min(earned, setting.MaxBalance.Value - currentBalance))
    End Function
End Class
