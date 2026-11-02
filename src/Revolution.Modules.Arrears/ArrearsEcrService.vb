Imports System.Globalization
Imports Revolution.Core
Imports Revolution.Modules.Payroll

''' <summary>
''' The arrear ECR for one wage month, from the open arrears claims. Needs Export on Statutory and the BasicPayroll plan feature. The UAN is released by the Employees export
''' (ADR 0019) only for the employees in the file. An employee with no valid UAN is left out and named in Problems by code. A month with an overpayment cannot go
''' in the file, because EPFO takes arrear amounts as positive: the build is refused and the codes are named. The audit text holds no id and no amount.
''' </summary>
Public NotInheritable Class ArrearsEcrService
    Private Const UanLength As Integer = 12

    Private ReadOnly _repository As IArrearsRepository
    Private ReadOnly _releaser As IExportIdReleaser
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService

    Public Sub New(repository As IArrearsRepository, releaser As IExportIdReleaser, permissions As IPermissionService,
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

    ''' <param name="wageMonth">Any date in the wage month.</param>
    Public Function Build(wageMonth As Date) As OperationResult(Of FileOutput(Of String))
        If Not _entitlements.IsEnabled(PlanFeature.BasicPayroll) Then Return OperationResult(Of FileOutput(Of String)).Fail("Payroll is not part of your plan.")
        If Not _permissions.IsAllowed(ModuleKey.Statutory, PermissionAction.Export) Then Return OperationResult(Of FileOutput(Of String)).Fail("You are not allowed to do this.")
        Dim month As New Date(wageMonth.Year, wageMonth.Month, 1)
        Dim lines As List(Of ArrearsEcrLine) = _repository.EcrLines(month).Where(Function(l As ArrearsEcrLine) Not l.IsEmpty).ToList()
        If lines.Count = 0 Then Return OperationResult(Of FileOutput(Of String)).Fail("No arrears are claimed for " & month.ToString("MMM yyyy", CultureInfo.InvariantCulture) & ".")
        Dim overpaid As List(Of String) = lines.Where(Function(l As ArrearsEcrLine) l.HasNegative).Select(Function(l As ArrearsEcrLine) l.EmpCode).ToList()
        If overpaid.Count > 0 Then
            Return OperationResult(Of FileOutput(Of String)).Fail("An overpayment cannot go in the arrear ECR: " & String.Join(", ", overpaid) & ". Recover it through the payroll run and leave it out of this month's claims.")
        End If
        Dim released As OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)) = _releaser.Release(PayrollFileKind.Ecr, lines.Select(Function(l As ArrearsEcrLine) l.EmployeeId).ToList())
        If Not released.Succeeded Then Return OperationResult(Of FileOutput(Of String)).Fail(released.Message)
        Dim rows As New List(Of ArrearsEcrRow)()
        Dim problems As New List(Of String)()
        For Each line As ArrearsEcrLine In lines
            Dim ids As ReleasedIds = Nothing
            If Not released.Value.TryGetValue(line.EmployeeId, ids) OrElse Not ValidUan(ids.Uan) Then
                problems.Add(line.EmpCode & ": no valid UAN, left out.")
                Continue For
            End If
            rows.Add(New ArrearsEcrRow(ids.Uan, line.FullName, line.EpfWages, line.EpsWages, line.EdliWages, line.EmployeeEpf, line.EmployerEps, line.EmployerEpf))
        Next
        If rows.Count = 0 Then Return OperationResult(Of FileOutput(Of String)).Fail("No employee in these claims has a valid UAN.")
        Dim text As String
        Try
            text = ArrearsEcrWriter.Build(rows)
        Catch ex As InvalidOperationException
            Return OperationResult(Of FileOutput(Of String)).Fail(ex.Message)
        End Try
        Dim stamp As String = month.ToString("yyyyMM", CultureInfo.InvariantCulture)
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Statutory, "BuildArrearEcr", "ArrearsEcr", stamp,
                                                                      $"Lines {rows.Count.ToString(CultureInfo.InvariantCulture)}. Left out {problems.Count.ToString(CultureInfo.InvariantCulture)}."))
        If Not logged.Succeeded Then Return OperationResult(Of FileOutput(Of String)).Fail(logged.Message)
        Return OperationResult(Of FileOutput(Of String)).Ok(New FileOutput(Of String)(text, $"ECR_ARREAR_{stamp}.txt", problems))
    End Function

    Private Shared Function ValidUan(uan As String) As Boolean
        Return Not String.IsNullOrWhiteSpace(uan) AndAlso uan.Length = UanLength AndAlso uan.All(Function(c As Char) Char.IsDigit(c))
    End Function
End Class
