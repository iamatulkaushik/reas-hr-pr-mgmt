''' <summary>Net pay and what could not be recovered, in whole rupees. TdsTaken is the income tax taken from this pay and TdsShortfall the part of the tax due that pay could not cover.</summary>
Public NotInheritable Class NetPayResult
    Public Sub New(netPay As Decimal, emiRecovered As Decimal, emiDeferred As Decimal, carryForwardOut As Decimal, Optional tdsTaken As Decimal = 0D, Optional tdsShortfall As Decimal = 0D)
        Me.TdsTaken = tdsTaken
        Me.TdsShortfall = tdsShortfall
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
    Public ReadOnly Property TdsTaken As Decimal
    ''' <summary>Tax due this period that pay could not cover. Not carried as a debt: the next month's projection sees the lower tax deducted and takes it then.</summary>
    Public ReadOnly Property TdsShortfall As Decimal
End Class
