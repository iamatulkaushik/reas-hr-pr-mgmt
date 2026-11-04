Imports Revolution.Modules.Biometric
Imports Xunit

Public Class PunchPullCoordinatorTests
    Private Shared ReadOnly Now_ As New Date(2026, 11, 4, 18, 0, 0)

    Private ReadOnly _essl As New FakePunchSource("ESSL")
    Private ReadOnly _sink As New FakePunchSink()
    Private ReadOnly _coordinator As PunchPullCoordinator

    Public Sub New()
        _coordinator = New PunchPullCoordinator(New List(Of IPunchSource) From {_essl}, _sink)
        _essl.Punches.Add(New RawPunch(12, Nothing, Now_.AddHours(-2)))
        _essl.Punches.Add(New RawPunch(13, Nothing, Now_.AddHours(-1)))
    End Sub

    Private Shared Function Device(id As Integer, code As String, Optional brand As String = "ESSL", Optional active As Boolean = True, Optional last As Date? = Nothing) As DeviceItem
        Return New DeviceItem(id, 1, "Main plant", code, "Gate " & code, brand, "192.168.1." & id, 4370, active, last, Nothing)
    End Function

    <Fact>
    Public Sub FirstPull_TakesEverything_AndStoresIt()
        Dim r As IReadOnlyList(Of DevicePullResult) = _coordinator.PullAll(New List(Of DeviceItem) From {Device(1, "G1")}, Now_)
        Assert.True(r.Single().Succeeded)
        Assert.Equal(2, r.Single().Pulled)
        Assert.Equal(2, r.Single().Inserted)
        Assert.False(_essl.Pulls.Single().Item2.HasValue)
        Assert.Equal(1, _sink.Calls.Single().Item1)
    End Sub

    <Fact>
    Public Sub NextPull_StartsAFewMinutesBeforeTheNewestStoredPunch()
        Dim last As New Date(2026, 11, 4, 16, 30, 0)
        _coordinator.PullAll(New List(Of DeviceItem) From {Device(1, "G1", last:=last)}, Now_)
        Assert.Equal(last.AddMinutes(-PunchPullCoordinator.OverlapMinutes), _essl.Pulls.Single().Item2.Value)
    End Sub

    <Fact>
    Public Sub InactiveDevices_AreSkipped()
        Dim r As IReadOnlyList(Of DevicePullResult) = _coordinator.PullAll(New List(Of DeviceItem) From {Device(1, "G1", active:=False)}, Now_)
        Assert.Empty(r)
        Assert.Empty(_essl.Pulls)
    End Sub

    <Fact>
    Public Sub OneDeadDevice_DoesNotStopTheOthers()
        _essl.FailWith = "Timed out."
        Dim r As IReadOnlyList(Of DevicePullResult) = _coordinator.PullAll(New List(Of DeviceItem) From {Device(1, "BAD"), Device(2, "G2")}, Now_)
        Assert.False(r(0).Succeeded)
        Assert.Contains("could not be read", r(0).Error, StringComparison.Ordinal)
        Assert.True(r(1).Succeeded)
        Assert.Equal(2, _sink.Calls.Single().Item1)
    End Sub

    <Fact>
    Public Sub BrandWithNoReader_IsReported_WithoutTheHost()
        Dim r As IReadOnlyList(Of DevicePullResult) = _coordinator.PullAll(New List(Of DeviceItem) From {Device(1, "G1", "Mantra")}, Now_)
        Assert.False(r.Single().Succeeded)
        Assert.Contains("Mantra", r.Single().Error, StringComparison.Ordinal)
        Assert.DoesNotContain("192.168", r.Single().Error, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub BrandIsMatchedWithoutCase()
        Dim r As IReadOnlyList(Of DevicePullResult) = _coordinator.PullAll(New List(Of DeviceItem) From {Device(1, "G1", "essl")}, Now_)
        Assert.True(r.Single().Succeeded)
    End Sub

    <Fact>
    Public Sub RejectedPunches_AreCounted_AndNotSent()
        _essl.Punches.Clear()
        _essl.Punches.Add(New RawPunch(Nothing, Nothing, Now_.AddHours(-1)))
        _essl.Punches.Add(New RawPunch(12, Nothing, New Date(2000, 1, 1)))
        Dim r As IReadOnlyList(Of DevicePullResult) = _coordinator.PullAll(New List(Of DeviceItem) From {Device(1, "G1")}, Now_)
        Assert.True(r.Single().Succeeded)
        Assert.Equal(2, r.Single().Pulled)
        Assert.Equal(2, r.Single().Rejected)
        Assert.Empty(_sink.Calls)
    End Sub

    <Fact>
    Public Sub DatabaseRefusal_IsReportedForThatDevice()
        _sink.FailWith = "Device not found or not active."
        Dim r As IReadOnlyList(Of DevicePullResult) = _coordinator.PullAll(New List(Of DeviceItem) From {Device(1, "G1")}, Now_)
        Assert.False(r.Single().Succeeded)
        Assert.Contains("not stored", r.Single().Error, StringComparison.Ordinal)
    End Sub
End Class
