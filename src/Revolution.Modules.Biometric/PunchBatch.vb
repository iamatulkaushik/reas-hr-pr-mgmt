''' <summary>Punches ready to store, and how many were thrown away and why (counted by reason, never by person).</summary>
Public NotInheritable Class PunchBatch
    Public Sub New(punches As IReadOnlyList(Of RawPunch), rejected As IReadOnlyDictionary(Of String, Integer))
        Me.Punches = punches
        Me.Rejected = rejected
    End Sub

    Public ReadOnly Property Punches As IReadOnlyList(Of RawPunch)
    Public ReadOnly Property Rejected As IReadOnlyDictionary(Of String, Integer)

    Public ReadOnly Property RejectedCount As Integer
        Get
            Return Rejected.Values.Sum()
        End Get
    End Property
End Class
