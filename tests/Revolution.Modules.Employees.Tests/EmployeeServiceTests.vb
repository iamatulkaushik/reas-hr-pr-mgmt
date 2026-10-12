Imports Revolution.Core
Imports Revolution.Modules.Employees
Imports Xunit

Public Class EmployeeServiceTests
    Private ReadOnly _repo As New FakeEmployeeRepository()
    Private ReadOnly _crypto As New FakeFieldCrypto()
    Private ReadOnly _perms As New FakePermissions()
    Private ReadOnly _audit As New FakeAudit()
    Private ReadOnly _service As EmployeeService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Create)
        _perms.Allowed.Add(PermissionAction.Edit)
        Dim ids As New EmployeeIdentifierService(_repo, _crypto, _perms, _audit, New FakeSession())
        _service = New EmployeeService(_repo, ids, _perms, _audit, New FakeSession(), Function() New Date(2026, 10, 12))
    End Sub

    <Fact>
    Public Sub Create_Allowed_SavesEmployeeAndEncryptedIdsAndAudits()
        Dim r = _service.Create(EmployeeTestData.Draft(), EmployeeTestData.Placement())
        Assert.True(r.Succeeded)
        Assert.Equal("EMP-00101", r.Value.EmpCode)
        Assert.Equal(String.Empty, r.Value.IdWarning)
        Assert.Equal(IdChangeKind.Replaced, _repo.Writes.Single().Pan.Kind)
        Assert.Equal(2, _audit.Entries.Count)
    End Sub

    <Fact>
    Public Sub Create_AuditNeverHoldsAnId()
        _service.Create(EmployeeTestData.Draft(), EmployeeTestData.Placement())
        For Each e In _audit.Entries
            Assert.DoesNotContain(EmployeeTestData.Pan, e.Details)
            Assert.DoesNotContain(EmployeeTestData.Aadhaar, e.Details)
            Assert.DoesNotContain(EmployeeTestData.Uan, e.Details)
        Next
    End Sub

    <Fact>
    Public Sub Create_WithoutCreatePermission_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.Create(EmployeeTestData.Draft(), EmployeeTestData.Placement()).Succeeded)
        Assert.Empty(_repo.Employees)
    End Sub

    <Fact>
    Public Sub Create_CreateWithoutEdit_StillSavesIds()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Dim r = _service.Create(EmployeeTestData.Draft(), EmployeeTestData.Placement())
        Assert.True(r.Succeeded)
        Assert.Equal(String.Empty, r.Value.IdWarning)
        Assert.Single(_repo.Writes)
    End Sub

    <Fact>
    Public Sub Create_InvalidInput_ListsEveryProblemAndSavesNothing()
        Dim d = EmployeeTestData.Draft()
        d.FullName = ""
        Dim p = EmployeeTestData.Placement()
        p.Gender = ""
        Dim r = _service.Create(d, p)
        Assert.False(r.Succeeded)
        Assert.Contains("name", r.Message)
        Assert.Contains("gender", r.Message)
        Assert.Empty(_repo.Employees)
    End Sub

    <Fact>
    Public Sub Create_NoIdsTyped_SkipsTheIdStep()
        Dim d = EmployeeTestData.Draft()
        d.Pan = ""
        d.Aadhaar = ""
        d.Uan = ""
        d.EsiIp = ""
        Assert.True(_service.Create(d, EmployeeTestData.Placement()).Succeeded)
        Assert.Empty(_repo.Writes)
    End Sub

    <Fact>
    Public Sub Create_DuplicateId_KeepsEmployeeAndReturnsWarning()
        _repo.Hashes(Convert.ToHexString(_crypto.BlindIndex(3, FieldContext.Pan, EmployeeTestData.Pan))) = 555
        Dim r = _service.Create(EmployeeTestData.Draft(), EmployeeTestData.Placement())
        Assert.True(r.Succeeded)
        Assert.Single(_repo.Employees)
        Assert.Contains("another employee", r.Value.IdWarning)
    End Sub

    <Fact>
    Public Sub Create_AuditFails_SaysTheEmployeeWasStillCreated()
        _audit.FailWith = "disk full"
        Dim r = _service.Create(EmployeeTestData.Draft(), EmployeeTestData.Placement())
        Assert.False(r.Succeeded)
        Assert.Contains("EMP-00101", r.Message)
        Assert.Single(_repo.Employees)
    End Sub

    <Fact>
    Public Sub Update_StaleRowVersion_IsRefused()
        Dim id = _service.Create(EmployeeTestData.Draft(), EmployeeTestData.Placement()).Value.EmployeeId
        Assert.True(_service.Update(id, New Byte() {1}, EmployeeTestData.Draft(), EmployeeTestData.Placement()).Succeeded)
        Dim r = _service.Update(id, New Byte() {1}, EmployeeTestData.Draft(), EmployeeTestData.Placement())
        Assert.False(r.Succeeded)
        Assert.Contains("changed by someone else", r.Message)
    End Sub

    <Fact>
    Public Sub Update_DoesNotTouchIds()
        Dim id = _service.Create(EmployeeTestData.Draft(), EmployeeTestData.Placement()).Value.EmployeeId
        _service.Update(id, Nothing, EmployeeTestData.Draft(), EmployeeTestData.Placement())
        Assert.Single(_repo.Writes)
    End Sub

    <Fact>
    Public Sub Update_WithoutEditPermission_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.Update(101, Nothing, EmployeeTestData.Draft(), EmployeeTestData.Placement()).Succeeded)
    End Sub

    <Fact>
    Public Sub RecordExit_Valid_MarksLeftAndAudits()
        Dim id = _service.Create(EmployeeTestData.Draft(), EmployeeTestData.Placement()).Value.EmployeeId
        Assert.True(_service.RecordExit(id, New Date(2026, 10, 1), EmployeeStatus.Left).Succeeded)
        Assert.False(_repo.Employees(id).IsActive)
        Assert.Equal("Exit", _audit.Entries.Last().Action)
        Assert.Equal("Left on 01-10-2026", _audit.Entries.Last().Details)
    End Sub

    <Fact>
    Public Sub RecordExit_BeforeJoinDate_IsRefused()
        Dim id = _service.Create(EmployeeTestData.Draft(), EmployeeTestData.Placement()).Value.EmployeeId
        Assert.False(_service.RecordExit(id, New Date(2026, 3, 31), EmployeeStatus.Left).Succeeded)
        Assert.True(_repo.Employees(id).IsActive)
    End Sub

    <Theory>
    <InlineData(EmployeeStatus.Active)>
    <InlineData(EmployeeStatus.Notice)>
    Public Sub RecordExit_WithAnActiveStatus_IsRefused(status As EmployeeStatus)
        Dim id = _service.Create(EmployeeTestData.Draft(), EmployeeTestData.Placement()).Value.EmployeeId
        Assert.False(_service.RecordExit(id, New Date(2026, 10, 1), status).Succeeded)
    End Sub

    <Fact>
    Public Sub RecordExit_AlreadyLeft_IsRefused()
        Dim id = _service.Create(EmployeeTestData.Draft(), EmployeeTestData.Placement()).Value.EmployeeId
        _service.RecordExit(id, New Date(2026, 10, 1), EmployeeStatus.Left)
        Assert.False(_service.RecordExit(id, New Date(2026, 10, 2), EmployeeStatus.Retired).Succeeded)
    End Sub

    <Fact>
    Public Sub RecordExit_UnknownEmployee_IsRefused()
        Assert.False(_service.RecordExit(9999, New Date(2026, 10, 1), EmployeeStatus.Left).Succeeded)
    End Sub

    <Fact>
    Public Sub Reactivate_ActiveEmployee_IsRefused_AndLeftEmployee_Works()
        Dim id = _service.Create(EmployeeTestData.Draft(), EmployeeTestData.Placement()).Value.EmployeeId
        Assert.False(_service.Reactivate(id).Succeeded)
        _service.RecordExit(id, New Date(2026, 10, 1), EmployeeStatus.Left)
        Assert.True(_service.Reactivate(id).Succeeded)
        Assert.True(_repo.Employees(id).IsActive)
    End Sub

    <Fact>
    Public Sub Get_Unknown_ReturnsNotFound()
        Dim r = _service.Get(9999)
        Assert.False(r.Succeeded)
        Assert.Equal("Employee not found.", r.Message)
    End Sub

    <Fact>
    Public Sub Get_WithoutViewPermission_IsRefused()
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.Get(101).Succeeded)
    End Sub

    <Fact>
    Public Sub List_TooLongSearch_IsRefused()
        Assert.False(_service.List(New String("a"c, 101), Nothing, True).Succeeded)
        Assert.True(_service.List("  ravi  ", Nothing, True).Succeeded)
    End Sub
End Class
