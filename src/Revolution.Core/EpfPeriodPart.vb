''' <summary>One part of a pay period that is under a single EPF rate row. Dates are inclusive. Days is the calendar days in the part.</summary>
Public NotInheritable Class EpfPeriodPart
    Public Sub New(fromDate As Date, toDate As Date, rate As EpfRate)
        If rate Is Nothing Then Throw New ArgumentNullException(NameOf(rate))
        If toDate.Date < fromDate.Date Then Throw New ArgumentException("The part ends before it starts.", NameOf(toDate))
        Me.FromDate = fromDate.Date
        Me.ToDate = toDate.Date
        Me.Rate = rate
    End Sub

    Public ReadOnly Property FromDate As Date
    Public ReadOnly Property ToDate As Date
    Public ReadOnly Property Rate As EpfRate

    Public ReadOnly Property Days As Integer
        Get
            Return CInt((ToDate - FromDate).TotalDays) + 1
        End Get
    End Property
End Class
