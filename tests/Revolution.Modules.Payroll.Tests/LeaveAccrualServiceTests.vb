Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class LeaveAccrualServiceTests
    Private NotInheritable Class FakeAccrualSource
        Implements ILeaveAccrualSource

        Public Items As New List(Of LeaveAccrualSettingItem)()
        Public Inputs As New List(Of LeaveAccrualInput)()
        Public ReadOnly Saved As New List(Of Tuple(Of Integer, LeaveAccrualSetting))()
        Public ReadOnly Posted As New List(Of IReadOnlyList(Of LeaveAccrualEntry))()
        Public PostedMonth As Date
        Public FailWith As String

        Public Function ListSettings() As IReadOnlyList(Of LeaveAccrualSettingItem) Implements ILeaveAccrualSource.ListSettings
            Return Items
        End Function

        Public Sub SaveSetting(leaveTypeId As Integer, setting As LeaveAccrualSetting, userId As Integer?) Implements ILeaveAccrualSource.SaveSetting
            If FailWith IsNot Nothing Then Throw New PayrollRuleException(FailWith)
            Saved.Add(Tuple.Create(leaveTypeId, setting))
        End Sub

        Public Function ListPresent(leaveTypeId As Integer, month As Date) As IReadOnlyList(Of LeaveAccrualInput) Implements ILeaveAccrualSource.ListPresent
            Return Inputs
        End Function

        Public Function Post(leaveTypeId As Integer, month As Date, entries As IReadOnlyList(Of LeaveAccrualEntry), userId As Integer?) As Integer Implements ILeaveAccrualSource.Post
            If FailWith IsNot Nothing Then Throw New PayrollRuleException(FailWith)
            Posted.Add(entries)
            PostedMonth = month
            Return entries.Count
        End Function
    End Class

    Private NotInheritable Class RoleSession
        Implements ISessionContext

        Private ReadOnly _role As UserRole

        Public Sub New(role As UserRole)
            _role = role
        End Sub

        Public ReadOnly Property Current As UserSession Implements ISessionContext.Current
            Get
                Return New UserSession(7, "user7", _role, 1, PlanTier.Gold)
            End Get
        End Property

        Public ReadOnly Property IsAuthenticated As Boolean Implements ISessionContext.IsAuthenticated
            Get
                Return True
            End Get
        End Property

        Public Sub SignIn(session As UserSession) Implements ISessionContext.SignIn
        End Sub

        Public Sub SignOut() Implements ISessionContext.SignOut
        End Sub
    End Class

    Private Shared ReadOnly Today As New Date(2026, 11, 15)

    Private ReadOnly _source As New FakeAccrualSource()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _audit As New FakePayAudit()

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _source.Items.Add(New LeaveAccrualSettingItem(1, "EL", "Earned leave", New LeaveAccrualSetting(1D, 20D, RateRounding.Nearest, 30D)))
        _source.Items.Add(New LeaveAccrualSettingItem(2, "CL", "Casual leave", Nothing))
        _source.Inputs.Add(New LeaveAccrualInput(101, 26D, 0D))
        _source.Inputs.Add(New LeaveAccrualInput(102, 9D, 0D))
        _source.Inputs.Add(New LeaveAccrualInput(103, 26D, 30D))
        _source.Inputs.Add(New LeaveAccrualInput(104, 30D, 5D))
    End Sub

    Private Function Service(Optional role As UserRole = UserRole.Associate) As LeaveAccrualService
        Return New LeaveAccrualService(_source, _perms, _plan, _audit, New RoleSession(role), Function() Today)
    End Function

    <Fact>
    Public Sub Preview_WorksDaysPerRule_RoundsAndCaps()
        Dim r As OperationResult(Of LeaveAccrualPreview) = Service().Preview(1, New Date(2026, 10, 20))
        Assert.True(r.Succeeded)
        Assert.Equal(New Date(2026, 10, 1), r.Value.Month)
        Assert.Equal(4, r.Value.EmployeesChecked)
        Assert.Equal(2, r.Value.Entries.Count)
        Assert.Equal(1D, r.Value.Entries.Single(Function(e As LeaveAccrualEntry) e.EmployeeId = 101).Days)
        Assert.Equal(2D, r.Value.Entries.Single(Function(e As LeaveAccrualEntry) e.EmployeeId = 104).Days)
        Assert.Equal(3D, r.Value.TotalDays)
    End Sub

    <Fact>
    Public Sub Preview_NoRule_AsksForOne()
        Dim r As OperationResult(Of LeaveAccrualPreview) = Service().Preview(2, New Date(2026, 10, 1))
        Assert.False(r.Succeeded)
        Assert.Contains("Set the accrual rule", r.Message, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub Preview_UnknownType_AndNoPlan_AreRefused()
        Assert.False(Service().Preview(9, New Date(2026, 10, 1)).Succeeded)
        _plan.Disabled.Add(PlanFeature.FullAndFinal)
        Assert.False(Service().Preview(1, New Date(2026, 10, 1)).Succeeded)
        Assert.False(Service().ListSettings().Succeeded)
    End Sub

    <Fact>
    Public Sub Post_WritesTheEntries_AndAuditsWithoutDays()
        Dim r As OperationResult(Of Integer) = Service().Post(1, New Date(2026, 10, 20))
        Assert.True(r.Succeeded)
        Assert.Equal(2, r.Value)
        Assert.Equal(New Date(2026, 10, 1), _source.PostedMonth)
        Assert.Equal(2, _source.Posted.Single().Count)
        Dim entry As AuditEntry = _audit.Entries.Single()
        Assert.Equal("LeaveAccrual.Post", entry.Action)
        Assert.Equal("Month 2026-10, 2 employees.", entry.Details)
    End Sub

    <Fact>
    Public Sub Post_NeedsOwnerOrAssociate_AndEdit()
        Assert.False(Service(UserRole.Operator).Post(1, New Date(2026, 10, 1)).Succeeded)
        Assert.True(Service(UserRole.Owner).Post(1, New Date(2026, 10, 1)).Succeeded)
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(Service().Post(1, New Date(2026, 9, 1)).Succeeded)
        Assert.Single(_source.Posted)
    End Sub

    <Fact>
    Public Sub Post_FutureMonth_IsRefused()
        Assert.False(Service().Post(1, New Date(2026, 12, 1)).Succeeded)
        Assert.True(Service().Post(1, New Date(2026, 11, 1)).Succeeded)
    End Sub

    <Fact>
    Public Sub Post_SecondTimeForTheMonth_IsRefusedByTheDatabaseRule()
        _source.FailWith = "Leave for this month is already posted for this leave type."
        Dim r As OperationResult(Of Integer) = Service().Post(1, New Date(2026, 10, 1))
        Assert.False(r.Succeeded)
        Assert.Contains("already posted", r.Message, StringComparison.Ordinal)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub SaveSetting_StoresTheRule_AndAuditsWithoutNumbers()
        Dim r As OperationResult = Service().SaveSetting(2, 1D, 15D, RateRounding.Down, Nothing)
        Assert.True(r.Succeeded)
        Dim saved As Tuple(Of Integer, LeaveAccrualSetting) = _source.Saved.Single()
        Assert.Equal(2, saved.Item1)
        Assert.Equal(15D, saved.Item2.PerWorkedDays)
        Assert.Equal(RateRounding.Down, saved.Item2.Rounding)
        Assert.False(saved.Item2.MaxBalance.HasValue)
        Assert.Equal("Accrual rule saved.", _audit.Entries.Single().Details)
    End Sub

    <Fact>
    Public Sub SaveSetting_NeedsOwnerOrAssociate_AndGoodValues()
        Assert.False(Service(UserRole.Operator).SaveSetting(1, 1D, 20D, RateRounding.Nearest, Nothing).Succeeded)
        Assert.False(Service().SaveSetting(0, 1D, 20D, RateRounding.Nearest, Nothing).Succeeded)
        Assert.False(Service().SaveSetting(1, -1D, 20D, RateRounding.Nearest, Nothing).Succeeded)
        Assert.False(Service().SaveSetting(1, 1D, 0D, RateRounding.Nearest, Nothing).Succeeded)
        Assert.False(Service().SaveSetting(1, 1D, 20D, RateRounding.Nearest, -1D).Succeeded)
        Assert.False(Service().SaveSetting(1, 1D, 20D, CType(99, RateRounding), Nothing).Succeeded)
        Assert.Empty(_source.Saved)
    End Sub

    <Fact>
    Public Sub ListSettings_NeedsView()
        Assert.Equal(2, Service().ListSettings().Value.Count)
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(Service().ListSettings().Succeeded)
    End Sub
End Class
