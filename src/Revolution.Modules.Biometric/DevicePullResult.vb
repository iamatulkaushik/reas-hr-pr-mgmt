''' <summary>What pulling one device gave. Error is Nothing when the pull worked.</summary>
Public NotInheritable Class DevicePullResult
    Public Sub New(deviceCode As String, pulled As Integer, rejected As Integer, inserted As Integer, unmatched As Integer, [error] As String)
        Me.DeviceCode = deviceCode
        Me.Pulled = pulled
        Me.Rejected = rejected
        Me.Inserted = inserted
        Me.Unmatched = unmatched
        Me.Error = [error]
    End Sub

    Public ReadOnly Property DeviceCode As String
    Public ReadOnly Property Pulled As Integer
    Public ReadOnly Property Rejected As Integer
    Public ReadOnly Property Inserted As Integer
    Public ReadOnly Property Unmatched As Integer
    Public ReadOnly Property [Error] As String
    Public ReadOnly Property Succeeded As Boolean
        Get
            Return Me.Error Is Nothing
        End Get
    End Property
End Class
