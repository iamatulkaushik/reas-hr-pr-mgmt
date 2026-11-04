Imports System.Globalization
Imports System.Text.RegularExpressions
Imports Revolution.Core

''' <summary>
''' Devices, RFID cards and punches for the signed-in company (Phase 8, ADR 0012 and 0013). Needs the Biometric plan feature. Permissions on Biometric: View to look, Create to add a device
''' or map a card, Edit to change a device, unmap a card or match waiting punches. The Associate maps cards (Create and Edit). The audit text holds no card number and no punch.
''' </summary>
Public NotInheritable Class BiometricService
    Public Const MaxReportDays As Integer = 31
    Private Const NotAllowed As String = "You are not allowed to do this."

    Private Shared ReadOnly CodePattern As New Regex("^[A-Za-z0-9_-]{1,30}$", RegexOptions.Compiled Or RegexOptions.CultureInvariant)

    Private ReadOnly _repository As IBiometricRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext

    Public Sub New(repository As IBiometricRepository, permissions As IPermissionService, entitlements As IEntitlementService, audit As IAuditService, session As ISessionContext)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        _repository = repository
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _session = session
    End Sub

    Public Function ListDevices(includeInactive As Boolean) As OperationResult(Of IReadOnlyList(Of DeviceItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of DeviceItem)).Fail(allowed.Message)
        Return OperationResult(Of IReadOnlyList(Of DeviceItem)).Ok(_repository.ListDevices(includeInactive))
    End Function

    ''' <summary>Adds a device (DeviceId Nothing, needs Create) or changes one (needs Edit). Returns its id.</summary>
    Public Function SaveDevice(device As DeviceWrite) As OperationResult(Of Integer)
        If device Is Nothing Then Return OperationResult(Of Integer).Fail("Give the device.")
        Dim allowed As OperationResult = CheckAccess(If(device.DeviceId.HasValue, PermissionAction.Edit, PermissionAction.Create))
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim problem As String = ValidateDevice(device)
        If problem.Length > 0 Then Return OperationResult(Of Integer).Fail(problem)
        Dim clean As New DeviceWrite With {.DeviceId = device.DeviceId, .FactoryId = device.FactoryId, .Code = device.Code.Trim(), .Name = device.Name.Trim(),
                                           .Brand = device.Brand.Trim(), .Host = device.Host.Trim(), .Port = device.Port, .IsActive = device.IsActive}
        Dim id As Integer
        Try
            id = _repository.SaveDevice(clean, CurrentUserId())
        Catch ex As BiometricRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Biometric, If(device.DeviceId.HasValue, "Device.Change", "Device.Add"), "Device",
                                                                      id.ToString(CultureInfo.InvariantCulture), "Device saved."))
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    Public Function ListCards(employeeId As Integer?, includeRetired As Boolean) As OperationResult(Of IReadOnlyList(Of RfidCardItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of RfidCardItem)).Fail(allowed.Message)
        Return OperationResult(Of IReadOnlyList(Of RfidCardItem)).Ok(_repository.ListCards(employeeId, includeRetired))
    End Function

    ''' <summary>Maps one RFID card to one employee. The employee's old card is retired. A card on another active employee is refused.</summary>
    Public Function MapCard(employeeId As Integer, cardNo As String) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Create)
        If Not allowed.Succeeded Then Return allowed
        If employeeId <= 0 Then Return OperationResult.Fail("Pick an employee.")
        Dim card As String = If(cardNo, String.Empty).Trim()
        If card.Length = 0 OrElse card.Length > PunchBatchBuilder.MaxCardLength OrElse Not card.All(Function(c As Char) Char.IsLetterOrDigit(c)) Then
            Return OperationResult.Fail("The card number must be 1 to 30 letters and digits.")
        End If
        Try
            _repository.MapCard(employeeId, card, CurrentUserId())
        Catch ex As BiometricRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Biometric, "Card.Map", "Employee", employeeId.ToString(CultureInfo.InvariantCulture), "Card mapped."))
    End Function

    Public Function UnmapCard(employeeId As Integer) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        If employeeId <= 0 Then Return OperationResult.Fail("Pick an employee.")
        Try
            _repository.UnmapCard(employeeId, CurrentUserId())
        Catch ex As BiometricRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Biometric, "Card.Unmap", "Employee", employeeId.ToString(CultureInfo.InvariantCulture), "Card retired."))
    End Function

    ''' <summary>Stored punches for at most 31 days.</summary>
    Public Function ListPunches(filter As PunchFilter) As OperationResult(Of IReadOnlyList(Of PunchRow))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of PunchRow)).Fail(allowed.Message)
        If filter Is Nothing Then Return OperationResult(Of IReadOnlyList(Of PunchRow)).Fail("Give the dates.")
        If filter.ToDate.Date < filter.FromDate.Date Then Return OperationResult(Of IReadOnlyList(Of PunchRow)).Fail("The last date cannot be before the first date.")
        If (filter.ToDate.Date - filter.FromDate.Date).TotalDays + 1 > MaxReportDays Then
            Return OperationResult(Of IReadOnlyList(Of PunchRow)).Fail($"Pick at most {MaxReportDays.ToString(CultureInfo.InvariantCulture)} days.")
        End If
        Return OperationResult(Of IReadOnlyList(Of PunchRow)).Ok(_repository.ListPunches(filter))
    End Function

    ''' <summary>Matches waiting punches to employees, after a card was mapped or a biometric id was set.</summary>
    Public Function ResolvePunches() As OperationResult(Of ResolveResult)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return OperationResult(Of ResolveResult).Fail(allowed.Message)
        Dim result As ResolveResult = _repository.ResolvePunches()
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Biometric, "Punch.Resolve", "Punch", "-",
                                                                      $"{result.Resolved.ToString(CultureInfo.InvariantCulture)} matched, {result.StillUnmatched.ToString(CultureInfo.InvariantCulture)} waiting."))
        If Not logged.Succeeded Then Return OperationResult(Of ResolveResult).Fail(logged.Message)
        Return OperationResult(Of ResolveResult).Ok(result)
    End Function

    Private Shared Function ValidateDevice(device As DeviceWrite) As String
        If device.FactoryId <= 0 Then Return "Pick the location."
        If Not CodePattern.IsMatch(If(device.Code, String.Empty).Trim()) Then Return "The device code must be 1 to 30 letters, digits, hyphens or underscores."
        If String.IsNullOrWhiteSpace(device.Name) OrElse device.Name.Trim().Length > 100 Then Return "Give a device name of at most 100 characters."
        If String.IsNullOrWhiteSpace(device.Brand) OrElse device.Brand.Trim().Length > 30 Then Return "Give the brand, at most 30 characters."
        If String.IsNullOrWhiteSpace(device.Host) OrElse device.Host.Trim().Length > 100 Then Return "Give the device address, at most 100 characters."
        If device.Port < 1 OrElse device.Port > 65535 Then Return "The port must be from 1 to 65535."
        Return String.Empty
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.Biometric) Then Return OperationResult.Fail("Biometric attendance is not part of your plan.")
        If Not _permissions.IsAllowed(ModuleKey.Biometric, action) Then Return OperationResult.Fail(NotAllowed)
        Return OperationResult.Ok()
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user As UserSession = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class
