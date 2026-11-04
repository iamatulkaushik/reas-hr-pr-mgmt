''' <summary>What storing a batch gave: punches received, new ones stored, and punches still waiting for an employee on that device since the batch began.</summary>
Public NotInheritable Class PunchIngestResult
    Public Sub New(received As Integer, inserted As Integer, unmatched As Integer)
        Me.Received = received
        Me.Inserted = inserted
        Me.Unmatched = unmatched
    End Sub

    Public ReadOnly Property Received As Integer
    Public ReadOnly Property Inserted As Integer
    Public ReadOnly Property Unmatched As Integer
End Class
