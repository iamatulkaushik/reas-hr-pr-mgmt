Imports Revolution.Core

''' <summary>One wage month of an arrears claim: what was paid, what should have been paid, and the rate rows to use for that month's contributions.</summary>
Public NotInheritable Class ArrearsMonthInput
    Public Sub New(wageMonth As Date, paid As WageBreakdown, due As WageBreakdown, rates As RunRates, dailyWage As Decimal, stayEsiEligible As Boolean, Optional epfOnActualWage As Boolean = False, Optional pensionOnActualWage As Boolean = False)
        If paid Is Nothing Then Throw New ArgumentNullException(NameOf(paid))
        If due Is Nothing Then Throw New ArgumentNullException(NameOf(due))
        If rates Is Nothing Then Throw New ArgumentNullException(NameOf(rates))
        Me.WageMonth = New Date(wageMonth.Year, wageMonth.Month, 1)
        Me.Paid = paid
        Me.Due = due
        Me.Rates = rates
        Me.DailyWage = dailyWage
        Me.StayEsiEligible = stayEsiEligible
        Me.EpfOnActualWage = epfOnActualWage
        Me.PensionOnActualWage = pensionOnActualWage
    End Sub

    ''' <summary>First day of the wage month the arrears belong to.</summary>
    Public ReadOnly Property WageMonth As Date
    Public ReadOnly Property Paid As WageBreakdown
    Public ReadOnly Property Due As WageBreakdown
    ''' <summary>Rate rows for this month. The caller picks the rate date (open decision G).</summary>
    Public ReadOnly Property Rates As RunRates
    Public ReadOnly Property DailyWage As Decimal
    Public ReadOnly Property StayEsiEligible As Boolean
    Public ReadOnly Property EpfOnActualWage As Boolean
    Public ReadOnly Property PensionOnActualWage As Boolean
End Class
