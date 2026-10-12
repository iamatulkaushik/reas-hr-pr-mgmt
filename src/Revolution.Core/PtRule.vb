''' <summary>A state's professional tax rule in force on a date, with its slabs.</summary>
Public NotInheritable Class PtRule
    Public Sub New(stateId As Integer, effectiveFrom As Date, frequency As PtFrequency, specialMonth As Integer?, verified As Boolean,
                   sourceRef As String, slabs As IReadOnlyList(Of PtSlab))
        Me.StateId = stateId
        Me.EffectiveFrom = effectiveFrom
        Me.Frequency = frequency
        Me.SpecialMonth = specialMonth
        Me.Verified = verified
        Me.SourceRef = If(sourceRef, String.Empty)
        Me.Slabs = If(slabs, New List(Of PtSlab)())
    End Sub

    Public ReadOnly Property StateId As Integer
    Public ReadOnly Property EffectiveFrom As Date
    Public ReadOnly Property Frequency As PtFrequency
    Public ReadOnly Property SpecialMonth As Integer?
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property SourceRef As String
    Public ReadOnly Property Slabs As IReadOnlyList(Of PtSlab)
End Class
