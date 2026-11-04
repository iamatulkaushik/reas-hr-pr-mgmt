Imports System.Globalization

''' <summary>
''' Checks the punches a device gave before they are stored: one of user id or card number, a user id above zero, a card number of at most 30 letters and digits, and a time that
''' is believable (not more than a day after now, not more than three years before it: a device that lost its clock reports dates like 2000-01-01). Identical punches in one pull are
''' kept once. Rejects are counted by reason. Pure.
''' </summary>
Public NotInheritable Class PunchBatchBuilder
    Public Const MaxCardLength As Integer = 30
    Public Const MaxFutureDays As Integer = 1
    Public Const MaxPastDays As Integer = 1095

    Public Const NoWho As String = "No user id or card number."
    Public Const BadUser As String = "User id is not above zero."
    Public Const BadCard As String = "Card number is not valid."
    Public Const BadTime As String = "Punch time is not believable."

    Private Sub New()
    End Sub

    ''' <param name="now">The factory's local time now, on the same clock as the device times.</param>
    Public Shared Function Build(raws As IReadOnlyList(Of RawPunch), now As Date) As PunchBatch
        If raws Is Nothing Then Throw New ArgumentNullException(NameOf(raws))
        Dim kept As New List(Of RawPunch)()
        Dim seen As New HashSet(Of String)()
        Dim rejected As New Dictionary(Of String, Integer)()
        For Each raw As RawPunch In raws
            Dim reason As String = Check(raw, now)
            If reason.Length > 0 Then
                rejected(reason) = If(rejected.ContainsKey(reason), rejected(reason), 0) + 1
                Continue For
            End If
            Dim card As String = Clean(raw.CardNo)
            Dim whole As Date = New Date(raw.PunchTime.Year, raw.PunchTime.Month, raw.PunchTime.Day, raw.PunchTime.Hour, raw.PunchTime.Minute, raw.PunchTime.Second)
            Dim key As String = String.Join("|", If(raw.DeviceUserId.HasValue, raw.DeviceUserId.Value.ToString(CultureInfo.InvariantCulture), "-"), card, whole.Ticks.ToString(CultureInfo.InvariantCulture))
            If seen.Add(key) Then kept.Add(New RawPunch(raw.DeviceUserId, If(card.Length = 0, Nothing, card), whole, raw.VerifyMode, raw.DeviceStatus))
        Next
        Return New PunchBatch(kept, rejected)
    End Function

    Private Shared Function Check(raw As RawPunch, now As Date) As String
        If raw Is Nothing Then Return NoWho
        Dim card As String = Clean(raw.CardNo)
        If Not raw.DeviceUserId.HasValue AndAlso card.Length = 0 Then Return NoWho
        If raw.DeviceUserId.HasValue AndAlso raw.DeviceUserId.Value <= 0 Then Return BadUser
        If card.Length > 0 AndAlso (card.Length > MaxCardLength OrElse Not card.All(Function(c As Char) Char.IsLetterOrDigit(c))) Then Return BadCard
        If raw.PunchTime > now.AddDays(MaxFutureDays) OrElse raw.PunchTime < now.AddDays(-MaxPastDays) Then Return BadTime
        Return String.Empty
    End Function

    Private Shared Function Clean(card As String) As String
        Return If(card, String.Empty).Trim()
    End Function
End Class
