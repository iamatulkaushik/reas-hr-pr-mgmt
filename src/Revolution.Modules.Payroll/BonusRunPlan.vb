''' <summary>What a bonus run would hold: a row for every employee with pay in the year, the run lines for those with something to pay, the percentage range of the rule, and any problem that stops the run.</summary>
Public NotInheritable Class BonusRunPlan
    Public Sub New(rows As IReadOnlyList(Of BonusRow), lines As IReadOnlyList(Of RunLineResult), pct As Decimal, minPct As Decimal, maxPct As Decimal, Optional problems As IReadOnlyList(Of String) = Nothing)
        If rows Is Nothing Then Throw New ArgumentNullException(NameOf(rows))
        If lines Is Nothing Then Throw New ArgumentNullException(NameOf(lines))
        Me.Rows = rows
        Me.Lines = lines
        Me.Pct = pct
        Me.MinPct = minPct
        Me.MaxPct = maxPct
        Me.Problems = If(problems, New List(Of String)())
    End Sub

    Public ReadOnly Property Rows As IReadOnlyList(Of BonusRow)
    Public ReadOnly Property Lines As IReadOnlyList(Of RunLineResult)
    Public ReadOnly Property Pct As Decimal
    Public ReadOnly Property MinPct As Decimal
    Public ReadOnly Property MaxPct As Decimal
    ''' <summary>Employees whose bonus could not be worked out, each starting with the employee code. The run is not made while there are any.</summary>
    Public ReadOnly Property Problems As IReadOnlyList(Of String)

    ''' <summary>Paid out: the bonus less provident fund and income tax.</summary>
    Public ReadOnly Property TotalAmount As Decimal
        Get
            Return Lines.Sum(Function(l As RunLineResult) l.NetPay)
        End Get
    End Property

    Public ReadOnly Property TotalGross As Decimal
        Get
            Return Lines.Sum(Function(l As RunLineResult) l.GrossEarnings)
        End Get
    End Property
End Class
