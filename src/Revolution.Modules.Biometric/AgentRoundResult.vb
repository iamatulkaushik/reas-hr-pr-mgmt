''' <summary>What one pull round did. Error is Nothing when the server could be asked. Lines are safe to log: device codes and counts only, never hosts, keys or punches.</summary>
Public NotInheritable Class AgentRoundResult
    Public Sub New(devices As IReadOnlyList(Of DevicePullResult), [error] As String)
        Me.Devices = devices
        Me.Error = [error]
    End Sub

    Public ReadOnly Property Devices As IReadOnlyList(Of DevicePullResult)
    Public ReadOnly Property [Error] As String

    Public ReadOnly Property Lines As IReadOnlyList(Of String)
        Get
            If Me.Error IsNot Nothing Then Return New List(Of String) From {"Round failed: " & Me.Error}
            If Devices.Count = 0 Then Return New List(Of String) From {"No active devices."}
            Return Devices.Select(Function(d As DevicePullResult) Describe(d)).ToList()
        End Get
    End Property

    Private Shared Function Describe(d As DevicePullResult) As String
        If Not d.Succeeded Then Return d.DeviceCode & ": " & d.Error
        Return $"{d.DeviceCode}: pulled {d.Pulled}, stored {d.Inserted}, rejected {d.Rejected}, waiting for an employee {d.Unmatched}."
    End Function
End Class
