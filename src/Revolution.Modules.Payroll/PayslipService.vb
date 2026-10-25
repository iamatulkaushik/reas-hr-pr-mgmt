Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Payslip data for the report helper. Needs the BasicPayroll feature and View on Payroll. Ids are masked by default. Full ids need Export on Payroll
''' (checked by the Employees module, which writes the batch reveal log before it decrypts) and an approved, locked or paid run. A run below Approved is
''' marked DRAFT. One audit entry per print job: run, number of payslips and whether full ids were printed. No amount and no id is audited.
''' </summary>
Public NotInheritable Class PayslipService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const MaxSlipsPerPrint As Integer = 5000

    Private ReadOnly _repository As IPayslipRepository
    Private ReadOnly _releaser As IPayslipIdReleaser
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService

    Public Sub New(repository As IPayslipRepository, releaser As IPayslipIdReleaser, permissions As IPermissionService,
                   entitlements As IEntitlementService, audit As IAuditService)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If releaser Is Nothing Then Throw New ArgumentNullException(NameOf(releaser))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        _repository = repository
        _releaser = releaser
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
    End Sub

    ''' <summary>Payslips of a run, or of one employee when employeeId is given.</summary>
    Public Function GetPayslips(runId As Integer, employeeId As Integer?, includeFullIds As Boolean) As OperationResult(Of IReadOnlyList(Of Payslip))
        If Not _entitlements.IsEnabled(PlanFeature.Payslips) Then Return Fail("Payslips are not part of your plan.")
        If Not _permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.View) Then Return Fail(NotAllowed)
        If runId <= 0 Then Return Fail("Pick a run.")
        If employeeId.HasValue AndAlso employeeId.Value <= 0 Then Return Fail("Pick an employee.")

        Dim rows As IReadOnlyList(Of PayslipRow)
        Try
            rows = _repository.GetPayslipRows(runId, employeeId)
        Catch ex As PayrollRuleException
            Return Fail(ex.Message)
        End Try
        If rows.Count = 0 Then Return Fail("There is no payslip for this selection.")
        If rows.Count > MaxSlipsPerPrint Then Return Fail($"At most {MaxSlipsPerPrint.ToString(CultureInfo.InvariantCulture)} payslips can be printed at once.")

        Dim status As PayrollRunStatus = rows(0).RunStatus
        Dim isDraft As Boolean = status < PayrollRunStatus.Approved
        Dim released As IReadOnlyDictionary(Of Integer, ReleasedIds) = Nothing
        If includeFullIds Then
            If isDraft Then Return Fail("Full ids can be printed only for an approved run.")
            Dim result As OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)) = _releaser.ReleaseForPayslips(rows.Select(Function(r) r.EmployeeId).ToList())
            If Not result.Succeeded Then Return Fail(result.Message)
            released = result.Value
        End If

        Dim slips As New List(Of Payslip)()
        For Each row As PayslipRow In rows
            Dim ids As PayslipIds
            Dim plain As ReleasedIds = Nothing
            If released IsNot Nothing AndAlso released.TryGetValue(row.EmployeeId, plain) Then
                ids = New PayslipIds(plain.Uan, plain.EsiIp, plain.AccountNumber, False)
            Else
                ids = New PayslipIds(IdMask.Last4(row.UanLast4), IdMask.Last4(row.EsiIpLast4), IdMask.Last4(row.AccountLast4), True)
            End If
            slips.Add(New Payslip(row, ids, RupeeWords.Convert(Decimal.Round(row.NetPay, 0, MidpointRounding.AwayFromZero)), isDraft))
        Next

        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, "Payslip.Print", "PayrollRun", runId.ToString(CultureInfo.InvariantCulture),
                                                                      $"{slips.Count.ToString(CultureInfo.InvariantCulture)} payslips, full ids {If(includeFullIds, "yes", "no")}."))
        If Not logged.Succeeded Then Return Fail(logged.Message)
        Return OperationResult(Of IReadOnlyList(Of Payslip)).Ok(slips)
    End Function

    Private Shared Function Fail(message As String) As OperationResult(Of IReadOnlyList(Of Payslip))
        Return OperationResult(Of IReadOnlyList(Of Payslip)).Fail(message)
    End Function
End Class
