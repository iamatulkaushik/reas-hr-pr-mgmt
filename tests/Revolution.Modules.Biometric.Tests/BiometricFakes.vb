Imports Revolution.Core
Imports Revolution.Modules.Attendance
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
    Public PunchTimes As New List(Of EmployeePunchTime)()
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

    Public Function ListPunchTimes(fromTime As Date, toTime As Date) As IReadOnlyList(Of EmployeePunchTime) Implements IBiometricRepository.ListPunchTimes
        Return PunchTimes.Where(Function(p) p.PunchTime >= fromTime AndAlso p.PunchTime < toTime).ToList()
    End Function
End Class

Friend NotInheritable Class FakeBioPermissions
    Implements IPermissionService

    Public ReadOnly Allowed As New HashSet(Of PermissionAction)()

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Return (moduleKey = ModuleKey.Biometric OrElse moduleKey = ModuleKey.Attendance) AndAlso Allowed.Contains(action)
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

Friend NotInheritable Class FakeRoster
    Implements IRosterRepository

    Public ReadOnly Shifts As New List(Of ShiftItem)()
    Public ReadOnly Holidays As New List(Of HolidayItem)()
    Public ReadOnly WeeklyOff As New List(Of WeeklyOffRule)()
    Public ReadOnly Assignments As New Dictionary(Of Integer, List(Of EmployeeShiftItem))()

    Public Function ListShifts() As IReadOnlyList(Of ShiftItem) Implements IRosterRepository.ListShifts
        Return Shifts
    End Function

    Public Function SaveShift(item As ShiftItem) As Integer Implements IRosterRepository.SaveShift
        Throw New NotSupportedException()
    End Function

    Public Function ListHolidays(year As Integer) As IReadOnlyList(Of HolidayItem) Implements IRosterRepository.ListHolidays
        Return Holidays.Where(Function(h) h.Day.Year = year).ToList()
    End Function

    Public Sub SaveHoliday(item As HolidayItem) Implements IRosterRepository.SaveHoliday
        Throw New NotSupportedException()
    End Sub

    Public Sub DeleteHoliday(day As Date) Implements IRosterRepository.DeleteHoliday
        Throw New NotSupportedException()
    End Sub

    Public Function ListWeeklyOff() As IReadOnlyList(Of WeeklyOffRule) Implements IRosterRepository.ListWeeklyOff
        Return WeeklyOff
    End Function

    Public Sub SaveWeeklyOff(rule As WeeklyOffRule) Implements IRosterRepository.SaveWeeklyOff
        Throw New NotSupportedException()
    End Sub

    Public Function ListEmployeeShifts(employeeId As Integer) As IReadOnlyList(Of EmployeeShiftItem) Implements IRosterRepository.ListEmployeeShifts
        Return If(Assignments.ContainsKey(employeeId), Assignments(employeeId), New List(Of EmployeeShiftItem)())
    End Function

    Public Sub AssignShift(employeeId As Integer, shiftId As Integer, effectiveFrom As Date) Implements IRosterRepository.AssignShift
        Throw New NotSupportedException()
    End Sub
End Class

Friend NotInheritable Class FakeAttendanceRepository
    Implements IAttendanceRepository

    Public ReadOnly Locked As New HashSet(Of Integer)()
    Public ReadOnly Marked As New Dictionary(Of Integer, List(Of DayRecord))()
    Public ReadOnly Saved As New List(Of Tuple(Of Integer, DayRecord, Integer?, Integer))()

    Public Function ListLockedMonths() As ISet(Of Integer) Implements IAttendanceRepository.ListLockedMonths
        Return Locked
    End Function

    Public Function ListMonth(employeeId As Integer, year As Integer, month As Integer) As IReadOnlyList(Of DayRecord) Implements IAttendanceRepository.ListMonth
        Return If(Marked.ContainsKey(employeeId), Marked(employeeId), New List(Of DayRecord)()).Where(Function(d) d.Day.Year = year AndAlso d.Day.Month = month).ToList()
    End Function

    Public Sub SaveDay(employeeId As Integer, record As DayRecord, shiftId As Integer?, workedMinutes As Integer, userId As Integer?) Implements IAttendanceRepository.SaveDay
        Saved.Add(Tuple.Create(employeeId, record, shiftId, workedMinutes))
    End Sub

    Public Sub CloseMonth(monthKey As Integer, userId As Integer?) Implements IAttendanceRepository.CloseMonth
        Throw New NotSupportedException()
    End Sub

    Public Sub ReopenMonth(monthKey As Integer, reason As String, userId As Integer?) Implements IAttendanceRepository.ReopenMonth
        Throw New NotSupportedException()
    End Sub
End Class
