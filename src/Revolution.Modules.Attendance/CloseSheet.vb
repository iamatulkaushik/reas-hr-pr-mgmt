''' <summary>Everything month close needs: each employee's summary and the employees that block the close.</summary>
Public NotInheritable Class CloseSheet
    Public Sub New(year As Integer, month As Integer, summaries As IReadOnlyDictionary(Of Integer, MonthSummary),
                   blockers As IReadOnlyList(Of String), isClosed As Boolean)
        Me.Year = year
        Me.Month = month
        Me.Summaries = summaries
        Me.Blockers = blockers
        Me.IsClosed = isClosed
    End Sub

    Public ReadOnly Property Year As Integer
    Public ReadOnly Property Month As Integer
    Public ReadOnly Property Summaries As IReadOnlyDictionary(Of Integer, MonthSummary)
    ''' <summary>One line per employee that cannot close, such as "E00012 Ravi: 3 day(s) are not marked."</summary>
    Public ReadOnly Property Blockers As IReadOnlyList(Of String)
    Public ReadOnly Property IsClosed As Boolean

    Public ReadOnly Property CanClose As Boolean
        Get
            Return Not IsClosed AndAlso Blockers.Count = 0 AndAlso Summaries.Count > 0
        End Get
    End Property
End Class
