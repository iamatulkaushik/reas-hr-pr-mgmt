''' <summary>Stores a batch of punches for one device, named by its code (agt.usp_Agent_Ingest, which calls att.usp_Punch_Ingest). A punch already stored is ignored.</summary>
Public Interface IPunchSink
    Function Ingest(deviceCode As String, punches As IReadOnlyList(Of RawPunch)) As PunchIngestResult
End Interface
