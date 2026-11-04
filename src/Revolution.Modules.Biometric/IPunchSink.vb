''' <summary>Stores a batch of punches for one device (att.usp_Punch_Ingest, reached through the agent wrapper). A punch already stored is ignored.</summary>
Public Interface IPunchSink
    Function Ingest(deviceId As Integer, punches As IReadOnlyList(Of RawPunch)) As PunchIngestResult
End Interface
