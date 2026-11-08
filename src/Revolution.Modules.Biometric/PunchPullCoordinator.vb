
''' <summary>
''' One pull round for the agent: for each active device, find the reader for its brand, pull the punches from a few minutes before the newest one stored, check them, and store them.
''' A device that cannot be read, a brand with no reader, or a batch the database refuses is reported for that device and the round goes on with the next (one dead device must not stop
''' the others). Duplicates are harmless: the database stores a punch once. No password, host or punch is written to any message. Pure orchestration, no I/O of its own.
''' </summary>
Public NotInheritable Class PunchPullCoordinator
    Public Const OverlapMinutes As Integer = 5

    Private ReadOnly _sources As IReadOnlyList(Of IPunchSource)
    Private ReadOnly _sink As IPunchSink

    Public Sub New(sources As IReadOnlyList(Of IPunchSource), sink As IPunchSink)
        If sources Is Nothing Then Throw New ArgumentNullException(NameOf(sources))
        If sink Is Nothing Then Throw New ArgumentNullException(NameOf(sink))
        _sources = sources
        _sink = sink
    End Sub

    ''' <param name="devices">The registered devices; inactive ones are skipped.</param>
    ''' <param name="now">The factory's local time now.</param>
    Public Function PullAll(devices As IReadOnlyList(Of DeviceItem), now As Date) As IReadOnlyList(Of DevicePullResult)
        If devices Is Nothing Then Throw New ArgumentNullException(NameOf(devices))
        Return PullAll(devices.Where(Function(d As DeviceItem) d.IsActive).Select(Function(d As DeviceItem) DeviceEndpoint.From(d)).ToList(), now)
    End Function

    ''' <summary>The agent's form: the server has already left out inactive devices.</summary>
    Public Function PullAll(devices As IReadOnlyList(Of DeviceEndpoint), now As Date) As IReadOnlyList(Of DevicePullResult)
        If devices Is Nothing Then Throw New ArgumentNullException(NameOf(devices))
        Dim results As New List(Of DevicePullResult)()
        For Each device As DeviceEndpoint In devices
            results.Add(PullOne(device, now))
        Next
        Return results
    End Function

    Private Function PullOne(device As DeviceEndpoint, now As Date) As DevicePullResult
        Dim reader As IPunchSource = _sources.FirstOrDefault(Function(s As IPunchSource) String.Equals(s.Brand, device.Brand, StringComparison.OrdinalIgnoreCase))
        If reader Is Nothing Then Return Failed(device, "No reader for brand " & device.Brand & ".")
        Dim since As Date? = If(device.LastPunchTime.HasValue, device.LastPunchTime.Value.AddMinutes(-OverlapMinutes), CType(Nothing, Date?))
        Dim raws As IReadOnlyList(Of RawPunch)
        Try
            raws = reader.Pull(device, since)
        Catch ex As PunchSourceException
            Return Failed(device, "The device could not be read: " & ex.Message)
        End Try
        Dim batch As PunchBatch = PunchBatchBuilder.Build(raws, now)
        If batch.Punches.Count = 0 Then Return New DevicePullResult(device.Code, raws.Count, batch.RejectedCount, 0, 0, Nothing)
        Try
            Dim stored As PunchIngestResult = _sink.Ingest(device.Code, batch.Punches)
            Return New DevicePullResult(device.Code, raws.Count, batch.RejectedCount, stored.Inserted, stored.Unmatched, Nothing)
        Catch ex As BiometricRuleException
            Return Failed(device, "The punches were not stored: " & ex.Message)
        End Try
    End Function

    Private Shared Function Failed(device As DeviceEndpoint, message As String) As DevicePullResult
        Return New DevicePullResult(device.Code, 0, 0, 0, 0, message)
    End Function
End Class
