''' <summary>A state's professional tax rule in force on a date, with its slabs.</summary>
Public NotInheritable Class PtRule
    Public Sub New(stateId As Integer, effectiveFrom As Date, frequency As PtFrequency, specialMonth As Integer?, verified As Boolean,
                   sourceRef As String, slabs As IReadOnlyList(Of PtSlab),
                   Optional dueMonths As IReadOnlyList(Of Integer) = Nothing)
        Me.StateId = stateId
        Me.EffectiveFrom = effectiveFrom
        Me.Frequency = frequency
        Me.SpecialMonth = specialMonth
        Me.Verified = verified
        Me.SourceRef = If(sourceRef, String.Empty)
        Me.Slabs = If(slabs, New List(Of PtSlab)())
        Me.DueMonths = If(dueMonths, New List(Of Integer)())
    End Sub

    Public ReadOnly Property StateId As Integer
    Public ReadOnly Property EffectiveFrom As Date
    Public ReadOnly Property Frequency As PtFrequency
    Public ReadOnly Property SpecialMonth As Integer?
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property SourceRef As String
    Public ReadOnly Property Slabs As IReadOnlyList(Of PtSlab)
    ''' <summary>The calendar months (1 to 12) in which tax is due under a half-yearly or yearly rule. Empty for a monthly rule. The slab TaxAmount is per due month.</summary>
    Public ReadOnly Property DueMonths As IReadOnlyList(Of Integer)

    ''' <summary>True when tax is deducted in this calendar month: always for a monthly rule, otherwise only in a listed due month.</summary>
    Public Function IsDue(month As Integer) As Boolean
        If month < 1 OrElse month > 12 Then
            Throw New ArgumentOutOfRangeException(NameOf(month))
        End If
        Return Frequency = PtFrequency.Monthly OrElse DueMonths.Contains(month)
    End Function
End Class
