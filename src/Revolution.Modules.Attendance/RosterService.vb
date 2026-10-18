Imports System.Globalization
Imports Revolution.Core

''' <summary>Shifts, holidays, weekly off and shift assignment. Needs Attendance Edit to change, View to read. Shifts need the ShiftOtLop plan feature.</summary>
Public NotInheritable Class RosterService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const NotInPlan As String = "Shifts are not part of your plan."
    Private Const MaxNameLength As Integer = 100
    Private Const DateFormat As String = "dd-MM-yyyy"
    Private Const MinYear As Integer = 1900
    Private Const MaxYear As Integer = 9999

    Private ReadOnly _repository As IRosterRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _today As Func(Of Date)

    Public Sub New(repository As IRosterRepository, permissions As IPermissionService, entitlements As IEntitlementService,
                   audit As IAuditService, today As Func(Of Date))
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If today Is Nothing Then Throw New ArgumentNullException(NameOf(today))
        _repository = repository
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _today = today
    End Sub

    Public Function ListShifts() As OperationResult(Of IReadOnlyList(Of ShiftItem))
        If Not CanView() Then Return OperationResult(Of IReadOnlyList(Of ShiftItem)).Fail(NotAllowed)
        If Not _entitlements.IsEnabled(PlanFeature.ShiftOtLop) Then Return OperationResult(Of IReadOnlyList(Of ShiftItem)).Fail(NotInPlan)
        Return OperationResult(Of IReadOnlyList(Of ShiftItem)).Ok(_repository.ListShifts())
    End Function

    Public Function SaveShift(item As ShiftItem) As OperationResult(Of Integer)
        If item Is Nothing Then Throw New ArgumentNullException(NameOf(item))
        If Not CanEdit() Then Return OperationResult(Of Integer).Fail(NotAllowed)
        If Not _entitlements.IsEnabled(PlanFeature.ShiftOtLop) Then Return OperationResult(Of Integer).Fail(NotInPlan)
        If item.Name.Trim().Length = 0 OrElse item.Name.Trim().Length > MaxNameLength Then
            Return OperationResult(Of Integer).Fail($"Shift name is required, up to {MaxNameLength} characters.")
        End If
        Try
            item.ToDefinition()
        Catch ex As ArgumentException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        If Not [Enum].IsDefined(GetType(OvertimeRounding), item.OvertimeStep) Then
            Return OperationResult(Of Integer).Fail("Overtime step must be 1, 15, 30 or 60 minutes.")
        End If
        Dim id As Integer
        Try
            id = _repository.SaveShift(item)
        Catch ex As AttendanceRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim audited = _audit.Record(New AuditEntry(ModuleKey.Attendance, If(item.ShiftId.HasValue, "UpdateShift", "CreateShift"), "Shift",
                                                   id.ToString(CultureInfo.InvariantCulture), item.Name.Trim()))
        If Not audited.Succeeded Then Return OperationResult(Of Integer).Fail(audited.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    Public Function ListHolidays(year As Integer) As OperationResult(Of IReadOnlyList(Of HolidayItem))
        If Not CanView() Then Return OperationResult(Of IReadOnlyList(Of HolidayItem)).Fail(NotAllowed)
        If year < MinYear OrElse year > MaxYear Then Return OperationResult(Of IReadOnlyList(Of HolidayItem)).Fail("Year is not valid.")
        Return OperationResult(Of IReadOnlyList(Of HolidayItem)).Ok(_repository.ListHolidays(year))
    End Function

    Public Function SaveHoliday(day As Date, name As String) As OperationResult
        If Not CanEdit() Then Return OperationResult.Fail(NotAllowed)
        Dim cleaned = If(name, String.Empty).Trim()
        If cleaned.Length = 0 OrElse cleaned.Length > MaxNameLength Then
            Return OperationResult.Fail($"Holiday name is required, up to {MaxNameLength} characters.")
        End If
        If day.Year < MinYear OrElse day.Year > MaxYear Then Return OperationResult.Fail("Date is not valid.")
        Dim stored = Run(Sub() _repository.SaveHoliday(New HolidayItem(day, cleaned)))
        If Not stored.Succeeded Then Return stored
        Return _audit.Record(New AuditEntry(ModuleKey.Attendance, "SaveHoliday", "Holiday", day.ToString(DateFormat, CultureInfo.InvariantCulture), cleaned))
    End Function

    Public Function DeleteHoliday(day As Date) As OperationResult
        If Not CanEdit() Then Return OperationResult.Fail(NotAllowed)
        Dim stored = Run(Sub() _repository.DeleteHoliday(day))
        If Not stored.Succeeded Then Return stored
        Return _audit.Record(New AuditEntry(ModuleKey.Attendance, "DeleteHoliday", "Holiday", day.ToString(DateFormat, CultureInfo.InvariantCulture), "Removed"))
    End Function

    Public Function ListWeeklyOff() As OperationResult(Of IReadOnlyList(Of WeeklyOffRule))
        If Not CanView() Then Return OperationResult(Of IReadOnlyList(Of WeeklyOffRule)).Fail(NotAllowed)
        Return OperationResult(Of IReadOnlyList(Of WeeklyOffRule)).Ok(_repository.ListWeeklyOff())
    End Function

    Public Function SaveWeeklyOff(effectiveFrom As Date, daysMask As Integer) As OperationResult
        If Not CanEdit() Then Return OperationResult.Fail(NotAllowed)
        If daysMask < 0 OrElse daysMask > WeeklyOffRule.MaximumMask Then Return OperationResult.Fail("Weekly off days are not valid.")
        Dim stored = Run(Sub() _repository.SaveWeeklyOff(New WeeklyOffRule(effectiveFrom, daysMask)))
        If Not stored.Succeeded Then Return stored
        Return _audit.Record(New AuditEntry(ModuleKey.Attendance, "SaveWeeklyOff", "WeeklyOffRule",
                                            effectiveFrom.ToString(DateFormat, CultureInfo.InvariantCulture), $"Mask {daysMask}"))
    End Function

    Public Function ListEmployeeShifts(employeeId As Integer) As OperationResult(Of IReadOnlyList(Of EmployeeShiftItem))
        If Not CanView() Then Return OperationResult(Of IReadOnlyList(Of EmployeeShiftItem)).Fail(NotAllowed)
        If Not _entitlements.IsEnabled(PlanFeature.ShiftOtLop) Then Return OperationResult(Of IReadOnlyList(Of EmployeeShiftItem)).Fail(NotInPlan)
        If employeeId <= 0 Then Return OperationResult(Of IReadOnlyList(Of EmployeeShiftItem)).Fail("Pick an employee.")
        Return OperationResult(Of IReadOnlyList(Of EmployeeShiftItem)).Ok(_repository.ListEmployeeShifts(employeeId))
    End Function

    ''' <summary>Assigns a shift from a date. The date cannot be in the past more than the database allows: it must be after the current start.</summary>
    Public Function AssignShift(employeeId As Integer, shiftId As Integer, effectiveFrom As Date) As OperationResult
        If Not CanEdit() Then Return OperationResult.Fail(NotAllowed)
        If Not _entitlements.IsEnabled(PlanFeature.ShiftOtLop) Then Return OperationResult.Fail(NotInPlan)
        If employeeId <= 0 OrElse shiftId <= 0 Then Return OperationResult.Fail("Pick an employee and a shift.")
        If effectiveFrom.Date < _today().Date.AddYears(-1) Then Return OperationResult.Fail("The start date is more than a year back.")
        Dim stored = Run(Sub() _repository.AssignShift(employeeId, shiftId, effectiveFrom))
        If Not stored.Succeeded Then Return stored
        Return _audit.Record(New AuditEntry(ModuleKey.Attendance, "AssignShift", "EmployeeShift",
                                            employeeId.ToString(CultureInfo.InvariantCulture),
                                            $"Shift {shiftId} from {effectiveFrom.ToString(DateFormat, CultureInfo.InvariantCulture)}"))
    End Function

    Private Function Run(work As Action) As OperationResult
        Try
            work()
        Catch ex As AttendanceRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return OperationResult.Ok()
    End Function

    Private Function CanView() As Boolean
        Return _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.View)
    End Function

    Private Function CanEdit() As Boolean
        Return _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Edit)
    End Function
End Class
