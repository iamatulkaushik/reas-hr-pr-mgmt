''' <summary>One employee row of the ESIC monthly contribution sheet.</summary>
Public NotInheritable Class EsicRow
    Public Sub New(ipNumber As String, ipName As String, daysPaid As Integer, totalWages As Decimal,
                   reasonCode As Integer?, lastWorkingDay As Date?)
        Me.IpNumber = ipNumber
        Me.IpName = ipName
        Me.DaysPaid = daysPaid
        Me.TotalWages = totalWages
        Me.ReasonCode = reasonCode
        Me.LastWorkingDay = lastWorkingDay
    End Sub

    Public ReadOnly Property IpNumber As String
    Public ReadOnly Property IpName As String
    Public ReadOnly Property DaysPaid As Integer
    Public ReadOnly Property TotalWages As Decimal
    ''' <summary>Required when days or wages are zero. Empty otherwise.</summary>
    Public ReadOnly Property ReasonCode As Integer?
    ''' <summary>Only when days are zero and the reason is an exit code.</summary>
    Public ReadOnly Property LastWorkingDay As Date?
End Class
