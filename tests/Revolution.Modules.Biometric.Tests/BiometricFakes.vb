Imports Revolution.Core
Imports Revolution.Modules.Biometric

Friend NotInheritable Class FakeBiometricRepository
    Implements IBiometricRepository

    Public Devices As New List(Of DeviceItem)()
    Public Cards As New List(Of RfidCardItem)()
    Public Punches As New List(Of PunchRow)()
    Public ReadOnly Saved As New List(Of DeviceWrite)()
    Public ReadOnly Mapped As New List(Of Tuple(Of Integer, String, Integer?))()
    Public ReadOnly Unmapped As New List(Of Integer)()
    Public LastFilter As PunchFilter
    Public Resolve As New ResolveResult(3, 1)
    Public FailWith As String

    Private Sub MaybeFail()
        If FailWith IsNot Nothing Then Throw New BiometricRuleException(FailWith)
    End Sub

    Public Function ListDevices(includeInactive As Boolean) As IReadOnlyList(Of DeviceItem) Implements IBiometricRepository.ListDevices
        Return Devices.Where(Function(d) includeInactive OrElse d.IsActive).ToList()
    End Function

    Public Function SaveDevice(device As DeviceWrite, userId As Integer?) As Integer Implements IBiometricRepository.SaveDevice
        MaybeFail()
        Saved.Add(device)
        Return If(device.DeviceId, 500 + Saved.Count)
    End Function

    Public Function ListCards(employeeId As Integer?, includeRetired As Boolean) As IReadOnlyList(Of RfidCardItem) Implements IBiometricRepository.ListCards
        Return Cards
    End Function

    Public Sub MapCard(employeeId As Integer, cardNo As String, userId As Integer?) Implements IBiometricRepository.MapCard
        MaybeFail()
        Mapped.Add(Tuple.Create(employeeId, cardNo, userId))
    End Sub

    Public Sub UnmapCard(employeeId As Integer, userId As Integer?) Implements IBiometricRepository.UnmapCard
        MaybeFail()
        Unmapped.Add(employeeId)
    End Sub

    Public Function ListPunches(filter As PunchFilter) As IReadOnlyList(Of PunchRow) Implements IBiometricRepository.ListPunches
        LastFilter = filter
        Return Punches
    End Function

    Public Function ResolvePunches() As ResolveResult Implements IBiometricRepository.ResolvePunches
        Return Resolve
    End Function
End Class

Friend NotInheritable Class FakeBioPermissions
    Implements IPermissionService

    Public ReadOnly Allowed As New HashSet(Of PermissionAction)()

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Return moduleKey = ModuleKey.Biometric AndAlso Allowed.Contains(action)
    End Function
End Class

Friend NotInheritable Class FakeBioEntitlements
    Implements IEntitlementService

    Public BiometricEnabled As Boolean = True

    Public ReadOnly Property CurrentPlan As PlanTier Implements IEntitlementService.CurrentPlan
        Get
            Return PlanTier.Gold
        End Get
    End Property

    Public Function IsEnabled(feature As PlanFeature) As Boolean Implements IEntitlementService.IsEnabled
        Return feature <> PlanFeature.Biometric OrElse BiometricEnabled
    End Function
End Class

Friend NotInheritable Class FakeBioAudit
    Implements IAuditService

    Public ReadOnly Entries As New List(Of AuditEntry)()

    Public Function Record(entry As AuditEntry) As OperationResult Implements IAuditService.Record
        Entries.Add(entry)
        Return OperationResult.Ok()
    End Function
End Class

Friend NotInheritable Class FakeBioSession
    Implements ISessionContext

    Public ReadOnly Property Current As UserSession Implements ISessionContext.Current
        Get
            Return New UserSession(7, "associate1", UserRole.Associate, 1, PlanTier.Gold)
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

Friend NotInheritable Class FakePunchSource
    Implements IPunchSource

    Private ReadOnly _brand As String

    Public Sub New(brand As String)
        _brand = brand
    End Sub

    Public Punches As New List(Of RawPunch)()
    Public FailWith As String
    Public ReadOnly Pulls As New List(Of Tuple(Of String, Date?))()

    Public ReadOnly Property Brand As String Implements IPunchSource.Brand
        Get
            Return _brand
        End Get
    End Property

    Public Function Pull(device As DeviceEndpoint, since As Date?) As IReadOnlyList(Of RawPunch) Implements IPunchSource.Pull
        Pulls.Add(Tuple.Create(device.Code, since))
        If FailWith IsNot Nothing AndAlso device.Code = "BAD" Then Throw New PunchSourceException(FailWith)
        Return Punches
    End Function
End Class

Friend NotInheritable Class FakePunchSink
    Implements IPunchSink

    Public ReadOnly Calls As New List(Of Tuple(Of Integer, IReadOnlyList(Of RawPunch)))()
    Public FailWith As String

    Public Function Ingest(deviceId As Integer, punches As IReadOnlyList(Of RawPunch)) As PunchIngestResult Implements IPunchSink.Ingest
        If FailWith IsNot Nothing Then Throw New BiometricRuleException(FailWith)
        Calls.Add(Tuple.Create(deviceId, punches))
        Return New PunchIngestResult(punches.Count, punches.Count, 0)
    End Function
End Class
