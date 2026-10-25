''' <summary>One entry of a run's status history.</summary>
Public NotInheritable Class PayrollRunEventItem
    Public Sub New(eventId As Long, fromStatus As Integer, toStatus As Integer, action As String, userId As Integer?, reason As String, eventUtc As Date)
        Me.EventId = eventId
        Me.FromStatus = fromStatus
        Me.ToStatus = toStatus
        Me.Action = action
        Me.UserId = userId
        Me.Reason = reason
        Me.EventUtc = eventUtc
    End Sub

    Public ReadOnly Property EventId As Long
    ''' <summary>0 when the run was created.</summary>
    Public ReadOnly Property FromStatus As Integer
    ''' <summary>0 when a draft was deleted.</summary>
    Public ReadOnly Property ToStatus As Integer
    Public ReadOnly Property Action As String
    Public ReadOnly Property UserId As Integer?
    Public ReadOnly Property Reason As String
    Public ReadOnly Property EventUtc As Date
End Class
