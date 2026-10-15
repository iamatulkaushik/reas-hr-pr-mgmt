Imports Revolution.Modules.Payroll
Imports Xunit

Public Class EsicSheetBuilderTests
    Private Shared ReadOnly ExitCodes As Integer() = {2, 3, 4, 5, 6, 10}

    Private Shared Function Paid(Optional ip As String = "2018517470", Optional days As Integer = 26,
                                 Optional wages As Decimal = 16064D) As EsicRow
        Return New EsicRow(ip, "Ravi Kumar", days, wages, Nothing, Nothing)
    End Function

    Private Shared Function Departed(lastDay As Date?) As EsicRow
        Return New EsicRow("2018517471", "Sita Devi", 0, 0D, 2, lastDay)
    End Function

    <Fact>
    Public Sub Build_PaidRow_AllCellsText()
        Dim table As IReadOnlyList(Of String()) = EsicSheetBuilder.Build({Paid()}, ExitCodes, 9)
        Assert.Equal(2, table.Count)
        Assert.Equal({"2018517470", "RAVI KUMAR", "26", "16064", "0", ""}, table(1))
    End Sub

    <Fact>
    Public Sub Build_ExitRow_HasReasonAndDate()
        Dim table As IReadOnlyList(Of String()) = EsicSheetBuilder.Build({Departed(New Date(2026, 9, 12))}, ExitCodes, 9)
        Assert.Equal("2", table(1)(4))
        Assert.Equal("12/09/2026", table(1)(5))
    End Sub

    <Fact>
    Public Sub Validate_ShortIpNumber_Fails()
        Assert.NotEmpty(EsicValidator.Validate({Paid(ip:="123")}, ExitCodes, 9))
    End Sub

    <Fact>
    Public Sub Validate_ZeroDaysWithWages_Fails()
        Assert.NotEmpty(EsicValidator.Validate({Paid(days:=0)}, ExitCodes, 9))
    End Sub

    <Fact>
    Public Sub Validate_ZeroWagesWithoutReason_IsCodeZero()
        Dim row As New EsicRow("2018517470", "A", 0, 0D, Nothing, Nothing)
        Assert.Empty(EsicValidator.Validate({row}, ExitCodes, 9))
    End Sub

    <Fact>
    Public Sub Validate_ReasonOnPaidRow_Fails()
        Dim row As New EsicRow("2018517470", "A", 26, 16064D, 1, Nothing)
        Assert.NotEmpty(EsicValidator.Validate({row}, ExitCodes, 9))
    End Sub

    <Fact>
    Public Sub Validate_OutOfCoverage_OnlyAprilOrOctober()
        Dim row As New EsicRow("2018517470", "A", 0, 0D, 4, New Date(2026, 9, 30))
        Assert.NotEmpty(EsicValidator.Validate({row}, ExitCodes, 9))
        Assert.Empty(EsicValidator.Validate({row}, ExitCodes, 10))
    End Sub

    <Fact>
    Public Sub Validate_UnknownReasonCode_Fails()
        Dim row As New EsicRow("2018517470", "A", 0, 0D, 14, Nothing)
        Assert.NotEmpty(EsicValidator.Validate({row}, ExitCodes, 9))
    End Sub

    <Fact>
    Public Sub Header_MatchesTemplate()
        Assert.Equal(6, EsicSheetBuilder.Header.Length)
        Assert.StartsWith("IP Number", EsicSheetBuilder.Header(0), StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub Validate_ExitReasonWithoutLastDay_Fails()
        Assert.NotEmpty(EsicValidator.Validate({Departed(Nothing)}, ExitCodes, 9))
    End Sub

    <Fact>
    Public Sub Validate_LastDayOnPaidRow_Fails()
        Dim row As New EsicRow("2018517470", "A", 26, 16064D, Nothing, New Date(2026, 9, 12))
        Assert.NotEmpty(EsicValidator.Validate({row}, ExitCodes, 9))
    End Sub

    <Fact>
    Public Sub Validate_NonExitReasonWithLastDay_Fails()
        Dim row As New EsicRow("2018517470", "A", 0, 0D, 1, New Date(2026, 9, 12))
        Assert.NotEmpty(EsicValidator.Validate({row}, ExitCodes, 9))
    End Sub

    <Fact>
    Public Sub Build_InvalidRow_Throws()
        Assert.Throws(Of InvalidOperationException)(Function() EsicSheetBuilder.Build({Paid(ip:="1")}, ExitCodes, 9))
    End Sub

    <Fact>
    Public Sub Build_NoRows_Throws()
        Assert.Throws(Of InvalidOperationException)(Function() EsicSheetBuilder.Build(New List(Of EsicRow)(), ExitCodes, 9))
    End Sub
End Class
