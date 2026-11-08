''' <summary>One round of the agent: ask the server for the active devices, pull each with the reader for its brand, store the punches. A server that cannot be reached or refuses fails the whole round and the agent tries again at the next one. Pure orchestration.</summary>
Public NotInheritable Class AgentRound
    Private Sub New()
    End Sub

    Public Shared Function Run(gateway As IAgentGateway, readers As IReadOnlyList(Of IPunchSource), now As Date) As AgentRoundResult
        If gateway Is Nothing Then Throw New ArgumentNullException(NameOf(gateway))
        If readers Is Nothing Then Throw New ArgumentNullException(NameOf(readers))
        Dim devices As IReadOnlyList(Of DeviceEndpoint)
        Try
            devices = gateway.ListDevices()
        Catch ex As BiometricRuleException
            Return New AgentRoundResult(New List(Of DevicePullResult)(), ex.Message)
        End Try
        Dim coordinator As New PunchPullCoordinator(readers, gateway)
        Return New AgentRoundResult(coordinator.PullAll(devices, now), Nothing)
    End Function
End Class
