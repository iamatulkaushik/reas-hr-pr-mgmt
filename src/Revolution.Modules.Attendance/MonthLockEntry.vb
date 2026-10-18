''' <summary>One close or reopen of a month, from att.MonthLockEvent.</summary>
Public NotInheritable Class MonthLockEntry
    Public Sub New(isReopen As Boolean, userId As Integer?, reason As String, eventUtc As DateTime)
        Me.IsReopen = isReopen
        Me.UserId = userId
        Me.Reason = If(reason, String.Empty)
        Me.EventUtc = eventUtc
    End Sub

    Public ReadOnly Property IsReopen As Boolean
    Public ReadOnly Property UserId As Integer?
    Public ReadOnly Property Reason As String
    Public ReadOnly Property EventUtc As DateTime
End Class
