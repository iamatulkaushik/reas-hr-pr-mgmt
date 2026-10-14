''' <summary>Net pay and what could not be recovered, in whole rupees.</summary>
Public NotInheritable Class NetPayResult
    Public Sub New(netPay As Decimal, emiRecovered As Decimal, emiDeferred As Decimal, carryForwardOut As Decimal)
        Me.NetPay = netPay
        Me.EmiRecovered = emiRecovered
        Me.EmiDeferred = emiDeferred
        Me.CarryForwardOut = carryForwardOut
    End Sub

    Public ReadOnly Property NetPay As Decimal
    Public ReadOnly Property EmiRecovered As Decimal
    ''' <summary>Scheduled EMI not taken this period because of the cap. It stays due.</summary>
    Public ReadOnly Property EmiDeferred As Decimal
    ''' <summary>Shortfall carried to the next period when deductions exceed pay.</summary>
    Public ReadOnly Property CarryForwardOut As Decimal
End Class
