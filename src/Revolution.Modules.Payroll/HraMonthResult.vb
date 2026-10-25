''' <summary>The three amounts compared for one month, and the least of them (never below zero), which is the exempt HRA.</summary>
Public NotInheritable Class HraMonthResult
    Public Sub New(month As Date, hraReceived As Decimal, rentOverShare As Decimal, cityLimit As Decimal, exempt As Decimal)
        Me.Month = month
        Me.HraReceived = hraReceived
        Me.RentOverShare = rentOverShare
        Me.CityLimit = cityLimit
        Me.Exempt = exempt
    End Sub

    Public ReadOnly Property Month As Date
    Public ReadOnly Property HraReceived As Decimal
    ''' <summary>Rent paid minus the share of salary (10 percent by the rule), floored at zero.</summary>
    Public ReadOnly Property RentOverShare As Decimal
    ''' <summary>Salary times the city percentage (50 metro, 40 other, from the city list).</summary>
    Public ReadOnly Property CityLimit As Decimal
    Public ReadOnly Property Exempt As Decimal
End Class
