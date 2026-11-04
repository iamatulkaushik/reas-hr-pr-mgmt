Imports Revolution.Modules.Payroll

''' <summary>What an arrears run would hold: one line per employee, the claims that go in it, and the claims left out with the reason (named by employee code, never by amount).</summary>
Public NotInheritable Class ArrearsRunPlan
    Public Sub New(lines As IReadOnlyList(Of RunLineResult), claimIds As IReadOnlyList(Of Integer), problems As IReadOnlyList(Of String), Optional recoveries As IReadOnlyList(Of String) = Nothing)
        If lines Is Nothing Then Throw New ArgumentNullException(NameOf(lines))
        If claimIds Is Nothing Then Throw New ArgumentNullException(NameOf(claimIds))
        If problems Is Nothing Then Throw New ArgumentNullException(NameOf(problems))
        Me.Lines = lines
        Me.ClaimIds = claimIds
        Me.Problems = problems
        Me.Recoveries = If(recoveries, New List(Of String)())
    End Sub

    Public ReadOnly Property Lines As IReadOnlyList(Of RunLineResult)
    Public ReadOnly Property ClaimIds As IReadOnlyList(Of Integer)
    Public ReadOnly Property Problems As IReadOnlyList(Of String)
    ''' <summary>Employee codes with an overpaid claim. They are not in the run: the regular run takes the amount as other deductions (Owner, 2026-11-03).</summary>
    Public ReadOnly Property Recoveries As IReadOnlyList(Of String)

    Public ReadOnly Property TotalGross As Decimal
        Get
            Return Lines.Sum(Function(l As RunLineResult) l.GrossEarnings)
        End Get
    End Property

    Public ReadOnly Property TotalNet As Decimal
        Get
            Return Lines.Sum(Function(l As RunLineResult) l.NetPay)
        End Get
    End Property
End Class
