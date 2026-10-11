Imports Revolution.Core
Imports Revolution.Modules.Attendance

Friend NotInheritable Class FakeAttendanceRepository
    Implements IAttendanceRepository

    Public ReadOnly Locked As New HashSet(Of Integer)()
    Public ReadOnly Saved As New List(Of DayRecord)()
    Public ReadOnly Days As New List(Of DayRecord)()
    Public ClosedKey As Integer
    Public ReopenedKey As Integer
    Public ReadOnly History As New List(Of String)()
    Public FailWith As String

    Public Function ListLockedMonths() As ISet(Of Integer) Implements IAttendanceRepository.ListLockedMonths
        Return Locked
    End Function

    Public Function ListMonth(employeeId As Integer, year As Integer, month As Integer) As IReadOnlyList(Of DayRecord) Implements IAttendanceRepository.ListMonth
        Return Days
    End Function

    Public Sub SaveDay(employeeId As Integer, record As DayRecord, shiftId As Integer?, workedMinutes As Integer, userId As Integer?) Implements IAttendanceRepository.SaveDay
        ThrowIfFailing()
        Saved.Add(record)
    End Sub

    Public Sub CloseMonth(monthKey As Integer, userId As Integer?) Implements IAttendanceRepository.CloseMonth
        ThrowIfFailing()
        ClosedKey = monthKey
        History.Add($"close:{monthKey}")
        Locked.Add(monthKey)
    End Sub

    Public Sub ReopenMonth(monthKey As Integer, reason As String, userId As Integer?) Implements IAttendanceRepository.ReopenMonth
        ThrowIfFailing()
        ReopenedKey = monthKey
        Locked.Remove(monthKey)
        History.Add($"reopen:{monthKey}:{userId}:{reason}")
    End Sub

    Private Sub ThrowIfFailing()
        If FailWith IsNot Nothing Then
            Throw New AttendanceRuleException(FailWith)
        End If
    End Sub
End Class

Friend NotInheritable Class FakePermissions
    Implements IPermissionService

    Public ReadOnly Allowed As New HashSet(Of PermissionAction)()

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Return moduleKey = ModuleKey.Attendance AndAlso Allowed.Contains(action)
    End Function
End Class

Friend NotInheritable Class FakeEntitlements
    Implements IEntitlementService

    Public ShiftOtEnabled As Boolean = True

    Public ReadOnly Property CurrentPlan As PlanTier Implements IEntitlementService.CurrentPlan
        Get
            Return PlanTier.Gold
        End Get
    End Property

    Public Function IsEnabled(feature As PlanFeature) As Boolean Implements IEntitlementService.IsEnabled
        Return feature <> PlanFeature.ShiftOtLop OrElse ShiftOtEnabled
    End Function
End Class

''' <summary>Mimics AuditService on a plan without FullAuditTrail: accepts the call, keeps nothing.</summary>
Friend NotInheritable Class SilentAudit
    Implements IAuditService

    Public Function Record(entry As AuditEntry) As OperationResult Implements IAuditService.Record
        Return OperationResult.Ok()
    End Function
End Class

Friend NotInheritable Class FakeAudit
    Implements IAuditService

    Public ReadOnly Entries As New List(Of AuditEntry)()

    Public Function Record(entry As AuditEntry) As OperationResult Implements IAuditService.Record
        Entries.Add(entry)
        Return OperationResult.Ok()
    End Function
End Class

Friend NotInheritable Class FakeSession
    Implements ISessionContext

    Public ReadOnly Property Current As UserSession Implements ISessionContext.Current
        Get
            Return New UserSession(7, "owner1", UserRole.Owner, 1, PlanTier.Gold)
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
