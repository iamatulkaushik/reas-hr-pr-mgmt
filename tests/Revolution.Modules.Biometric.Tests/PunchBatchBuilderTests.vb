Imports Revolution.Modules.Biometric
Imports Xunit

Public Class PunchBatchBuilderTests
    Private Shared ReadOnly Now_ As New Date(2026, 11, 4, 18, 0, 0)

    Private Shared Function P(user As Integer?, card As String, time As Date) As RawPunch
        Return New RawPunch(user, card, time)
    End Function

    <Fact>
    Public Sub GoodPunches_AreKept_WithCardsTrimmed()
        Dim batch As PunchBatch = PunchBatchBuilder.Build(New List(Of RawPunch) From {P(12, Nothing, Now_.AddHours(-3)), P(Nothing, " 0A1B2C ", Now_.AddHours(-2))}, Now_)
        Assert.Equal(2, batch.Punches.Count)
        Assert.Equal("0A1B2C", batch.Punches(1).CardNo)
        Assert.Equal(0, batch.RejectedCount)
    End Sub

    <Fact>
    Public Sub NeitherUserNorCard_IsRejected_AndCountedByReason()
        Dim batch As PunchBatch = PunchBatchBuilder.Build(New List(Of RawPunch) From {P(Nothing, Nothing, Now_.AddHours(-1)), P(Nothing, "  ", Now_.AddHours(-1))}, Now_)
        Assert.Empty(batch.Punches)
        Assert.Equal(2, batch.Rejected(PunchBatchBuilder.NoWho))
    End Sub

    <Fact>
    Public Sub BadUserIdAndBadCard_AreRejected()
        Dim batch As PunchBatch = PunchBatchBuilder.Build(New List(Of RawPunch) From {P(0, Nothing, Now_.AddHours(-1)), P(-4, Nothing, Now_.AddHours(-1)),
            P(Nothing, "12 34", Now_.AddHours(-1)), P(Nothing, New String("A"c, 31), Now_.AddHours(-1))}, Now_)
        Assert.Empty(batch.Punches)
        Assert.Equal(2, batch.Rejected(PunchBatchBuilder.BadUser))
        Assert.Equal(2, batch.Rejected(PunchBatchBuilder.BadCard))
    End Sub

    <Fact>
    Public Sub UnbelievableTimes_AreRejected()
        Dim batch As PunchBatch = PunchBatchBuilder.Build(New List(Of RawPunch) From {
            P(1, Nothing, New Date(2000, 1, 1)), P(1, Nothing, Now_.AddDays(2)), P(1, Nothing, Now_.AddDays(-1096)), P(1, Nothing, Now_.AddHours(20))}, Now_)
        Assert.Equal(1, batch.Punches.Count)
        Assert.Equal(3, batch.Rejected(PunchBatchBuilder.BadTime))
    End Sub

    <Fact>
    Public Sub SamePunchTwice_IsKeptOnce_EvenWithMillisecondsApart()
        Dim t As Date = Now_.AddHours(-5)
        Dim batch As PunchBatch = PunchBatchBuilder.Build(New List(Of RawPunch) From {P(12, Nothing, t), P(12, Nothing, t.AddMilliseconds(400)), P(13, Nothing, t)}, Now_)
        Assert.Equal(2, batch.Punches.Count)
        Assert.Equal(0, batch.RejectedCount)
    End Sub

    <Fact>
    Public Sub SameTimeButDifferentCard_AreTwoPunches()
        Dim t As Date = Now_.AddHours(-5)
        Dim batch As PunchBatch = PunchBatchBuilder.Build(New List(Of RawPunch) From {P(Nothing, "AAA", t), P(Nothing, "BBB", t)}, Now_)
        Assert.Equal(2, batch.Punches.Count)
    End Sub

    <Fact>
    Public Sub DeviceCodesAreKept()
        Dim raw As New RawPunch(12, Nothing, Now_.AddHours(-1), CByte(1), CByte(0))
        Dim batch As PunchBatch = PunchBatchBuilder.Build(New List(Of RawPunch) From {raw}, Now_)
        Assert.Equal(CByte(1), batch.Punches.Single().VerifyMode.Value)
        Assert.Equal(CByte(0), batch.Punches.Single().DeviceStatus.Value)
    End Sub

    <Fact>
    Public Sub NothingGiven_GivesAnEmptyBatch()
        Dim batch As PunchBatch = PunchBatchBuilder.Build(New List(Of RawPunch)(), Now_)
        Assert.Empty(batch.Punches)
        Assert.Equal(0, batch.RejectedCount)
    End Sub
End Class
