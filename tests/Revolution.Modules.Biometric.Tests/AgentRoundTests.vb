Imports Revolution.Modules.Biometric
Imports Xunit

Public Class AgentRoundTests
    Private Shared ReadOnly Now_ As New Date(2026, 11, 7, 18, 0, 0)

    Private ReadOnly _gateway As New FakeAgentGateway()
    Private ReadOnly _essl As New FakePunchSource("ESSL")

    Private Function Readers() As IReadOnlyList(Of IPunchSource)
        Return New List(Of IPunchSource) From {_essl}
    End Function

    Public Sub New()
        _essl.Punches.Add(New RawPunch(12, Nothing, Now_.AddHours(-2)))
    End Sub

    <Fact>
    Public Sub Round_PullsEveryDevice_AndStoresByCode()
        _gateway.Devices.Add(New DeviceEndpoint(0, "G1", "ESSL", "10.0.0.5", 4370, New Date(2026, 11, 7, 12, 0, 0)))
        _gateway.Devices.Add(New DeviceEndpoint(0, "G2", "ESSL", "10.0.0.6", 4370))
        Dim r As AgentRoundResult = AgentRound.Run(_gateway, Readers(), Now_)
        Assert.Null(r.Error)
        Assert.Equal(2, r.Devices.Count)
        Assert.Equal({"G1", "G2"}, _gateway.Calls.Select(Function(c) c.Item1).ToArray())
        Assert.Equal(New Date(2026, 11, 7, 11, 55, 0), _essl.Pulls(0).Item2.Value)
        Assert.False(_essl.Pulls(1).Item2.HasValue)
    End Sub

    <Fact>
    Public Sub ServerRefusal_FailsTheRound_WithoutSayingMore()
        _gateway.ListFails = "The agent could not be verified."
        Dim r As AgentRoundResult = AgentRound.Run(_gateway, Readers(), Now_)
        Assert.Equal("The agent could not be verified.", r.Error)
        Assert.Empty(r.Devices)
        Assert.Equal("Round failed: The agent could not be verified.", r.Lines.Single())
    End Sub

    <Fact>
    Public Sub NoDevices_SaysSo()
        Assert.Equal("No active devices.", AgentRound.Run(_gateway, Readers(), Now_).Lines.Single())
    End Sub

    <Fact>
    Public Sub OneDeadDevice_IsReported_TheOthersGoOn()
        _essl.FailWith = "Timed out."
        _gateway.Devices.Add(New DeviceEndpoint(0, "BAD", "ESSL", "10.0.0.9", 4370))
        _gateway.Devices.Add(New DeviceEndpoint(0, "G2", "ESSL", "10.0.0.6", 4370))
        Dim lines As IReadOnlyList(Of String) = AgentRound.Run(_gateway, Readers(), Now_).Lines
        Assert.Equal(2, lines.Count)
        Assert.StartsWith("BAD: The device could not be read", lines(0), StringComparison.Ordinal)
        Assert.StartsWith("G2: pulled 1, stored 1", lines(1), StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub LogLines_NeverHoldHostsOrPunches()
        _gateway.Devices.Add(New DeviceEndpoint(0, "G1", "Mantra", "10.0.0.5", 4370))
        Dim text As String = String.Join(" ", AgentRound.Run(_gateway, Readers(), Now_).Lines)
        Assert.DoesNotContain("10.0.0.5", text, StringComparison.Ordinal)
        Assert.Contains("Mantra", text, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub PunchesJson_HasTheFieldsTheProcedureReads()
        Dim json As String = PunchesJson.Build(New List(Of RawPunch) From {
            New RawPunch(12, Nothing, New Date(2026, 11, 7, 8, 1, 5), CByte(1), CByte(0)), New RawPunch(Nothing, " 0A1B ", New Date(2026, 11, 7, 17, 0, 0))})
        Assert.Contains("""deviceUserId"":12", json, StringComparison.Ordinal)
        Assert.Contains("""punchTime"":""2026-11-07T08:01:05""", json, StringComparison.Ordinal)
        Assert.Contains("""cardNo"":""0A1B""", json, StringComparison.Ordinal)
        Assert.Contains("""verifyMode"":1", json, StringComparison.Ordinal)
        Assert.StartsWith("[{", json, StringComparison.Ordinal)
        Assert.Equal("[]", PunchesJson.Build(New List(Of RawPunch)()))
    End Sub
End Class
