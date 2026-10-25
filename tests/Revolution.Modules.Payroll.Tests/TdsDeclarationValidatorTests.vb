Imports Revolution.Modules.Payroll
Imports Xunit

Public Class TdsDeclarationValidatorTests
    Private Shared Function Rent(fromKey As Integer, toKey As Integer, amount As Decimal, Optional landlord As String = "A Kumar", Optional pan As String = Nothing) As TdsRentInput
        Return New TdsRentInput(fromKey, toKey, amount, "Pune", landlord, "Friend", Nothing, pan)
    End Function

    Private Shared Function Rents(ParamArray list As TdsRentInput()) As IReadOnlyList(Of TdsRentInput)
        Return New List(Of TdsRentInput)(list)
    End Function

    <Fact>
    Public Sub Items_NegativeAmount_Fails()
        Dim items As New List(Of TdsItem) From {New TdsItem("80C", "", "LIC", -1D, Nothing, Nothing)}
        Assert.False(TdsDeclarationValidator.ValidateItems(items).Succeeded)
    End Sub

    <Fact>
    Public Sub Items_NoSection_Fails()
        Dim items As New List(Of TdsItem) From {New TdsItem(" ", "", "LIC", 100D, Nothing, Nothing)}
        Assert.False(TdsDeclarationValidator.ValidateItems(items).Succeeded)
    End Sub

    <Fact>
    Public Sub Items_Valid_Passes()
        Dim items As New List(Of TdsItem) From {New TdsItem("80C", "", "LIC", 100D, "R1", 90D)}
        Assert.True(TdsDeclarationValidator.ValidateItems(items).Succeeded)
    End Sub

    <Fact>
    Public Sub Item_EffectiveAmount_UsesVerifiedWhenSet()
        Assert.Equal(90D, New TdsItem("80C", "", "LIC", 100D, "R1", 90D).EffectiveAmount)
        Assert.Equal(100D, New TdsItem("80C", "", "LIC", 100D, Nothing, Nothing).EffectiveAmount)
    End Sub

    <Fact>
    Public Sub Rents_InsideYear_Passes()
        Assert.True(TdsDeclarationValidator.ValidateRents(Rents(Rent(202604, 202703, 5000D)), 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub Rents_BeforeApril_Fails()
        Assert.False(TdsDeclarationValidator.ValidateRents(Rents(Rent(202603, 202703, 5000D)), 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub Rents_AfterMarch_Fails()
        Assert.False(TdsDeclarationValidator.ValidateRents(Rents(Rent(202604, 202704, 5000D)), 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub Rents_Overlap_Fails()
        Assert.False(TdsDeclarationValidator.ValidateRents(Rents(Rent(202604, 202609, 5000D), Rent(202609, 202703, 5000D)), 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub Rents_TouchingRanges_Pass()
        Assert.True(TdsDeclarationValidator.ValidateRents(Rents(Rent(202604, 202609, 5000D), Rent(202610, 202703, 5000D)), 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub Rents_BadMonth_Fails()
        Assert.False(TdsDeclarationValidator.ValidateRents(Rents(Rent(202613, 202614, 5000D)), 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub Rents_ZeroRent_Fails()
        Assert.False(TdsDeclarationValidator.ValidateRents(Rents(Rent(202604, 202703, 0D)), 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub Rents_YearlyAboveThreshold_NeedsPan()
        Assert.False(TdsDeclarationValidator.ValidateRents(Rents(Rent(202604, 202703, 10000D)), 2026).Succeeded)
        Assert.True(TdsDeclarationValidator.ValidateRents(Rents(Rent(202604, 202703, 10000D, "A Kumar", "ABCPK1234F")), 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub Rents_ExactlyAtThreshold_NoPanNeeded()
        Assert.True(TdsDeclarationValidator.ValidateRents(Rents(Rent(202604, 202607, 25000D)), 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub Rents_SameLandlordTwoRows_AddedTogether()
        Dim list As IReadOnlyList(Of TdsRentInput) = Rents(Rent(202604, 202609, 9000D, "a kumar"), Rent(202610, 202703, 9000D, "A KUMAR"))
        Assert.False(TdsDeclarationValidator.ValidateRents(list, 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub Rents_BadPanFormat_Fails()
        Assert.False(TdsDeclarationValidator.ValidateRents(Rents(Rent(202604, 202703, 5000D, "A Kumar", "12345")), 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub Prev_Nothing_Passes()
        Assert.True(TdsDeclarationValidator.ValidatePrevEmployer(Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub Prev_BadTan_Fails()
        Assert.False(TdsDeclarationValidator.ValidatePrevEmployer(New TdsPrevEmployer("ABC Ltd", "123", 1000D, 0D, 0D, 0D, 0D)).Succeeded)
    End Sub

    <Fact>
    Public Sub Prev_GoodTan_AnyCase_Passes()
        Assert.True(TdsDeclarationValidator.ValidatePrevEmployer(New TdsPrevEmployer("ABC Ltd", "delh12345a", 1000D, 0D, 0D, 0D, 0D)).Succeeded)
    End Sub

    <Fact>
    Public Sub Prev_NoName_Fails()
        Assert.False(TdsDeclarationValidator.ValidatePrevEmployer(New TdsPrevEmployer(" ", Nothing, 1000D, 0D, 0D, 0D, 0D)).Succeeded)
    End Sub

    <Fact>
    Public Sub MonthCount_IncludesBothEnds()
        Assert.Equal(12, TdsDeclarationValidator.MonthCount(202604, 202703))
        Assert.Equal(1, TdsDeclarationValidator.MonthCount(202610, 202610))
    End Sub
End Class
