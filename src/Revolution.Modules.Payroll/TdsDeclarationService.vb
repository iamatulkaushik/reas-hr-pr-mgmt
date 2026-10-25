Imports System.Globalization
Imports Revolution.Core
Imports Revolution.Modules.Employees

''' <summary>
''' TDS declaration use cases (Form 124). Needs the Tds plan feature and Payroll permissions: View to read, Create for a new declaration, Edit to change one
''' or to submit it, Approve to lock it, Reopen to take it back. The landlord PAN is encrypted here (ADR 0011) and shown as its last four characters.
''' A save writes the header, items, rent rows and previous employer one after the other; each step replaces its part whole, so a failed save can be run again.
''' Audit text holds no amount and no PAN.
''' </summary>
Public NotInheritable Class TdsDeclarationService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const LandlordPanField As String = "LandlordPAN"

    Private ReadOnly _repository As ITdsRepository
    Private ReadOnly _crypto As IFieldCrypto
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext

    Public Sub New(repository As ITdsRepository, crypto As IFieldCrypto, permissions As IPermissionService, entitlements As IEntitlementService,
                   audit As IAuditService, session As ISessionContext)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If crypto Is Nothing Then Throw New ArgumentNullException(NameOf(crypto))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        _repository = repository
        _crypto = crypto
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _session = session
    End Sub

    ''' <summary>The declaration with items, rent and previous employer, or a success with Nothing when the employee has none for the year.</summary>
    Public Function GetDeclaration(employeeId As Integer, financialYear As Integer) As OperationResult(Of TdsDeclarationBundle)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of TdsDeclarationBundle).Fail(allowed.Message)
        Try
            Dim header As TdsDeclarationHeader = _repository.GetDeclaration(employeeId, financialYear)
            If header Is Nothing Then Return OperationResult(Of TdsDeclarationBundle).Ok(Nothing)
            Return OperationResult(Of TdsDeclarationBundle).Ok(New TdsDeclarationBundle(header, _repository.ListItems(header.DeclarationId),
                                                                                         _repository.ListRents(header.DeclarationId), _repository.GetPrevEmployer(header.DeclarationId)))
        Catch ex As PayrollRuleException
            Return OperationResult(Of TdsDeclarationBundle).Fail(ex.Message)
        End Try
    End Function

    ''' <summary>Saves the whole declaration. Returns the declaration id.</summary>
    Public Function Save(employeeId As Integer, financialYear As Integer, regime As TdsRegime, items As IReadOnlyList(Of TdsItem),
                         rents As IReadOnlyList(Of TdsRentInput), prev As TdsPrevEmployer) As OperationResult(Of Integer)
        Dim user As UserSession = _session.Current
        If user Is Nothing Then Return OperationResult(Of Integer).Fail(NotAllowed)
        Dim existing As TdsDeclarationHeader
        Try
            existing = _repository.GetDeclaration(employeeId, financialYear)
        Catch ex As PayrollRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim allowed As OperationResult = CheckAccess(If(existing Is Nothing, PermissionAction.Create, PermissionAction.Edit))
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim check As OperationResult = ValidateAll(employeeId, financialYear, regime, items, rents, prev)
        If Not check.Succeeded Then Return OperationResult(Of Integer).Fail(check.Message)
        Dim id As Integer
        Try
            id = _repository.SaveDeclaration(employeeId, financialYear, regime, user.UserId)
            _repository.ReplaceItems(id, items, user.UserId)
            _repository.ReplaceRents(id, Protect(rents, user.CompanyId, id), user.UserId)
            If prev Is Nothing Then
                _repository.ClearPrevEmployer(id, user.UserId)
            Else
                _repository.SavePrevEmployer(id, prev, user.UserId)
            End If
        Catch ex As PayrollRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, If(existing Is Nothing, "TdsDeclaration.Add", "TdsDeclaration.Edit"), "TdsDeclaration",
                                                                      id.ToString(CultureInfo.InvariantCulture), $"Employee {employeeId.ToString(CultureInfo.InvariantCulture)}, year {financialYear.ToString(CultureInfo.InvariantCulture)}."))
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    ''' <summary>Moves the declaration: Submitted needs Edit, Locked needs Approve, back to Draft or Submitted needs Reopen.</summary>
    Public Function SetStatus(employeeId As Integer, financialYear As Integer, status As TdsDeclarationStatus) As OperationResult
        Dim user As UserSession = _session.Current
        If user Is Nothing Then Return OperationResult.Fail(NotAllowed)
        Dim header As TdsDeclarationHeader
        Try
            header = _repository.GetDeclaration(employeeId, financialYear)
        Catch ex As PayrollRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        If header Is Nothing Then Return OperationResult.Fail("Declaration not found.")
        Dim allowed As OperationResult = CheckAccess(ActionFor(header.Status, status))
        If Not allowed.Succeeded Then Return allowed
        Try
            _repository.SetStatus(header.DeclarationId, status, user.UserId)
        Catch ex As PayrollRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Payroll, "TdsDeclaration.Status", "TdsDeclaration", header.DeclarationId.ToString(CultureInfo.InvariantCulture),
                                            $"From {header.Status} to {status}."))
    End Function

    Private Shared Function ActionFor(current As TdsDeclarationStatus, target As TdsDeclarationStatus) As PermissionAction
        If target = TdsDeclarationStatus.Locked Then Return PermissionAction.Approve
        Return If(target < current, PermissionAction.Reopen, PermissionAction.Edit)
    End Function

    Private Shared Function ValidateAll(employeeId As Integer, financialYear As Integer, regime As TdsRegime, items As IReadOnlyList(Of TdsItem),
                                        rents As IReadOnlyList(Of TdsRentInput), prev As TdsPrevEmployer) As OperationResult
        If employeeId <= 0 Then Return OperationResult.Fail("Pick an employee.")
        If financialYear < 2000 OrElse financialYear > 2200 Then Return OperationResult.Fail("The financial year is not valid.")
        If regime <> TdsRegime.Old AndAlso regime <> TdsRegime.New Then Return OperationResult.Fail("Pick the tax regime.")
        Dim checks As OperationResult = TdsDeclarationValidator.ValidateItems(items)
        If Not checks.Succeeded Then Return checks
        checks = TdsDeclarationValidator.ValidateRents(rents, financialYear)
        If Not checks.Succeeded Then Return checks
        Return TdsDeclarationValidator.ValidatePrevEmployer(prev)
    End Function

    Private Function Protect(rents As IReadOnlyList(Of TdsRentInput), companyId As Integer, declarationId As Integer) As IReadOnlyList(Of TdsRentWrite)
        Dim result As New List(Of TdsRentWrite)()
        For index As Integer = 0 To rents.Count - 1
            Dim rent As TdsRentInput = rents(index)
            Dim number As Integer = index + 1
            If String.IsNullOrWhiteSpace(rent.LandlordPan) Then
                result.Add(New TdsRentWrite(number, rent, Nothing, Nothing, _crypto.CurrentKeyVersion))
            Else
                Dim pan As String = rent.LandlordPan.Replace(" ", String.Empty).Replace("-", String.Empty).Trim().ToUpperInvariant()
                Dim context As String = FieldContext.Build(companyId, declarationId, LandlordPanField & ":" & number.ToString(CultureInfo.InvariantCulture))
                result.Add(New TdsRentWrite(number, rent, _crypto.Encrypt(pan, context), pan.Substring(pan.Length - 4), _crypto.CurrentKeyVersion))
            End If
        Next
        Return result
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.Tds) Then
            Return OperationResult.Fail("TDS is not part of your plan.")
        End If
        If Not _permissions.IsAllowed(ModuleKey.Payroll, action) Then
            Return OperationResult.Fail(NotAllowed)
        End If
        Return OperationResult.Ok()
    End Function
End Class
