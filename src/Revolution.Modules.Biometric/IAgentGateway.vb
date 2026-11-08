''' <summary>
''' What the agent needs from the server and nothing more (ADR 0013, change request 0023): the active devices of its company with the newest stored punch time, and a way to store a batch for one
''' device by code. The server side is two procedures, agt.usp_Agent_Devices(@AgentKey) and agt.usp_Agent_Ingest(@AgentKey, @DeviceCode, @PunchesJson). Failures come as BiometricRuleException with a
''' message that tells nothing about keys or companies.
''' </summary>
Public Interface IAgentGateway
    Inherits IPunchSink

    Function ListDevices() As IReadOnlyList(Of DeviceEndpoint)
End Interface
