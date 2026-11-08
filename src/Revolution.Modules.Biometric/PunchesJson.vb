Imports System.Globalization
Imports System.Text.Json

''' <summary>The punches of one batch as the JSON array att.usp_Punch_Ingest reads: deviceUserId, cardNo, punchTime (yyyy-MM-ddTHH:mm:ss), verifyMode, deviceStatus. Missing values are left out. Pure.</summary>
Public NotInheritable Class PunchesJson
    Private Sub New()
    End Sub

    Public Shared Function Build(punches As IReadOnlyList(Of RawPunch)) As String
        If punches Is Nothing Then Throw New ArgumentNullException(NameOf(punches))
        Dim items As New List(Of Dictionary(Of String, Object))()
        For Each p As RawPunch In punches
            Dim item As New Dictionary(Of String, Object)()
            If p.DeviceUserId.HasValue Then item("deviceUserId") = p.DeviceUserId.Value
            If Not String.IsNullOrWhiteSpace(p.CardNo) Then item("cardNo") = p.CardNo.Trim()
            item("punchTime") = p.PunchTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
            If p.VerifyMode.HasValue Then item("verifyMode") = p.VerifyMode.Value
            If p.DeviceStatus.HasValue Then item("deviceStatus") = p.DeviceStatus.Value
            items.Add(item)
        Next
        Return JsonSerializer.Serialize(items)
    End Function
End Class
