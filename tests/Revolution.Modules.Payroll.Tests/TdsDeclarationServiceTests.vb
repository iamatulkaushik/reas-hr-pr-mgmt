Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class TdsDeclarationServiceTests
    Private ReadOnly _repo As New FakeTdsRepository()
    Private ReadOnly _crypto As New FakeTdsCrypto()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _audit As New FakePayAudit()
    Private ReadOnly _service As TdsDeclarationService
    Private ReadOnly _noItems As IReadOnlyList(Of TdsItem) = New List(Of TdsItem)()
    Private ReadOnly _noRents As IReadOnlyList(Of TdsRentInput) = New List(Of TdsRentInput)()

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Create)
        _perms.Allowed.Add(PermissionAction.Edit)
        _service = New TdsDeclarationService(_repo, _crypto, _perms, _plan, _audit, New FakePaySession())
    End Sub

    Private Shared Function Header(status As TdsDeclarationStatus) As TdsDeclarationHeader
        Return New TdsDeclarationHeader(5, 101, 2026, TdsRegime.Old, status)
    End Function

    Private Shared Function BigRent(pan As String) As IReadOnlyList(Of TdsRentInput)
        Return New List(Of TdsRentInput) From {New TdsRentInput(202604, 202703, 15000D, "Pune", "A Kumar", "Friend", Nothing, pan)}
    End Function

    <Fact>
    Public Sub PlanWithoutTds_RefusesEverything()
        _plan.Disabled.Add(PlanFeature.Tds)
        Assert.False(_service.GetDeclaration(101, 2026).Succeeded)
        Assert.False(_service.Save(101, 2026, TdsRegime.New, _noItems, _noRents, Nothing).Succeeded)
        Assert.False(_service.SetStatus(101, 2026, TdsDeclarationStatus.Submitted).Succeeded)
        Assert.Empty(_repo.Calls)
    End Sub

    <Fact>
    Public Sub Get_NoDeclaration_IsSuccessWithNothing()
        Dim r = _service.GetDeclaration(101, 2026)
        Assert.True(r.Succeeded)
        Assert.Null(r.Value)
    End Sub

    <Fact>
    Public Sub Get_ReturnsHeaderAndParts()
        _repo.Existing = Header(TdsDeclarationStatus.Draft)
        _repo.Items = New List(Of TdsItem) From {New TdsItem("80C", "", "LIC", 100D, Nothing, Nothing)}
        Dim r = _service.GetDeclaration(101, 2026)
        Assert.Equal(5, r.Value.Header.DeclarationId)
        Assert.Equal(1, r.Value.Items.Count)
    End Sub

    <Fact>
    Public Sub Get_WithoutView_IsRefused()
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.GetDeclaration(101, 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub Save_New_NeedsCreate()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.Save(101, 2026, TdsRegime.New, _noItems, _noRents, Nothing).Succeeded)
        Assert.Empty(_repo.Calls)
    End Sub

    <Fact>
    Public Sub Save_Existing_NeedsEdit()
        _repo.Existing = Header(TdsDeclarationStatus.Draft)
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.Save(101, 2026, TdsRegime.New, _noItems, _noRents, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub Save_WritesPartsInOrder_AndAudits()
        Dim r = _service.Save(101, 2026, TdsRegime.Old, New List(Of TdsItem) From {New TdsItem("80C", "", "LIC", 100D, Nothing, Nothing)}, _noRents, Nothing)
        Assert.True(r.Succeeded)
        Assert.Equal(1, r.Value)
        Assert.Equal("SaveDeclaration,ReplaceItems,ReplaceRents,ClearPrevEmployer", String.Join(",", _repo.Calls))
        Assert.Equal("TdsDeclaration.Add", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub Save_WithPrevEmployer_SavesIt()
        Dim prev As New TdsPrevEmployer("ABC Ltd", Nothing, 100000D, 0D, 0D, 0D, 5000D)
        Assert.True(_service.Save(101, 2026, TdsRegime.New, _noItems, _noRents, prev).Succeeded)
        Assert.Contains("SavePrevEmployer", _repo.Calls)
        Assert.DoesNotContain("ClearPrevEmployer", _repo.Calls)
    End Sub

    <Fact>
    Public Sub Save_EncryptsLandlordPan_WithDeclarationAndRentInContext()
        Assert.True(_service.Save(101, 2026, TdsRegime.Old, _noItems, BigRent("abcpk 1234-f"), Nothing).Succeeded)
        Dim written As TdsRentWrite = _repo.RentsWritten(0)
        Assert.Equal("234F", written.PanLast4)
        Assert.Equal("enc:ABCPK1234F", System.Text.Encoding.UTF8.GetString(written.PanEnc))
        Assert.Equal(CByte(3), written.KeyVersion)
        Assert.Equal("1:1:LandlordPAN:1", _crypto.Contexts(0))
    End Sub

    <Fact>
    Public Sub Save_RentWithoutPan_StoresNoCipher()
        Dim small As New List(Of TdsRentInput) From {New TdsRentInput(202604, 202606, 5000D, "Pune", "A Kumar", "Friend", Nothing, Nothing)}
        Assert.True(_service.Save(101, 2026, TdsRegime.Old, _noItems, small, Nothing).Succeeded)
        Assert.Null(_repo.RentsWritten(0).PanEnc)
        Assert.Empty(_crypto.Contexts)
    End Sub

    <Fact>
    Public Sub Save_InvalidData_WritesNothing()
        Dim r = _service.Save(101, 2026, TdsRegime.Old, _noItems, BigRent(Nothing), Nothing)
        Assert.False(r.Succeeded)
        Assert.Empty(_repo.Calls)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Save_BadRegime_Fails()
        Assert.False(_service.Save(101, 2026, CType(9, TdsRegime), _noItems, _noRents, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub Save_Locked_ReturnsDatabaseMessage_NoAudit()
        _repo.Existing = Header(TdsDeclarationStatus.Locked)
        _repo.SaveFailure = "The declaration is locked."
        Dim r = _service.Save(101, 2026, TdsRegime.Old, _noItems, _noRents, Nothing)
        Assert.False(r.Succeeded)
        Assert.Equal("The declaration is locked.", r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Save_AuditText_HoldsNoAmountOrPan()
        _service.Save(101, 2026, TdsRegime.Old, _noItems, BigRent("ABCPK1234F"), Nothing)
        Dim details As String = _audit.Entries(0).Details
        Assert.DoesNotContain("15000", details)
        Assert.DoesNotContain("ABCPK", details)
    End Sub

    <Fact>
    Public Sub SetStatus_Submit_NeedsEdit()
        _repo.Existing = Header(TdsDeclarationStatus.Draft)
        Assert.True(_service.SetStatus(101, 2026, TdsDeclarationStatus.Submitted).Succeeded)
        Assert.Equal(TdsDeclarationStatus.Submitted, _repo.LastStatus)
    End Sub

    <Fact>
    Public Sub SetStatus_Lock_NeedsApprove()
        _repo.Existing = Header(TdsDeclarationStatus.Submitted)
        Assert.False(_service.SetStatus(101, 2026, TdsDeclarationStatus.Locked).Succeeded)
        _perms.Allowed.Add(PermissionAction.Approve)
        Assert.True(_service.SetStatus(101, 2026, TdsDeclarationStatus.Locked).Succeeded)
    End Sub

    <Fact>
    Public Sub SetStatus_Reopen_NeedsReopen()
        _repo.Existing = Header(TdsDeclarationStatus.Locked)
        Assert.False(_service.SetStatus(101, 2026, TdsDeclarationStatus.Submitted).Succeeded)
        _perms.Allowed.Add(PermissionAction.Reopen)
        Assert.True(_service.SetStatus(101, 2026, TdsDeclarationStatus.Submitted).Succeeded)
    End Sub

    <Fact>
    Public Sub SetStatus_NoDeclaration_Fails()
        Assert.False(_service.SetStatus(101, 2026, TdsDeclarationStatus.Submitted).Succeeded)
    End Sub
End Class
