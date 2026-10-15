Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Builds the ECR, ESIC workbook and bank files for an approved run. Checks permission, loads the run through
''' IPayrollFileSource, maps, validates, and records one audit entry per file. Audit text never holds an id or amount.
''' A run must be Approved, Locked or Paid. Wage month is the month of the period end date.
''' </summary>
Public NotInheritable Class PayrollFileService
    Private Const NotAllowed As String = "You are not allowed to do this."

    Private ReadOnly _source As IPayrollFileSource
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _audit As IAuditService

    Public Sub New(source As IPayrollFileSource, permissions As IPermissionService, audit As IAuditService)
        If source Is Nothing Then Throw New ArgumentNullException(NameOf(source))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        _source = source
        _permissions = permissions
        _audit = audit
    End Sub

    ''' <summary>ECR text for EPFO. Needs Export on Statutory. Ceiling comes from the EPF rate row.</summary>
    Public Function BuildEcr(runId As Integer, wageCeiling As Decimal) As OperationResult(Of FileOutput(Of String))
        Dim prepared As OperationResult(Of PayrollFileSnapshot) = Prepare(runId, ModuleKey.Statutory)
        If Not prepared.Succeeded Then
            Return OperationResult(Of FileOutput(Of String)).Fail(prepared.Message)
        End If
        Dim run As PayrollFileSnapshot = prepared.Value
        Dim mapped As MapResult(Of EcrMember) = EcrMapper.Map(run.Employees, wageCeiling, run.PeriodTo)
        Dim text As String = String.Empty
        Try
            text = EcrFileWriter.Build(mapped.Items, wageCeiling)
        Catch ex As InvalidOperationException
            Return OperationResult(Of FileOutput(Of String)).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = Log(ModuleKey.Statutory, "BuildEcr", run, mapped.Items.Count, mapped.Problems.Count)
        If Not logged.Succeeded Then
            Return OperationResult(Of FileOutput(Of String)).Fail(logged.Message)
        End If
        Return OperationResult(Of FileOutput(Of String)).Ok(New FileOutput(Of String)(text, $"ECR_{Stamp(run)}.txt", mapped.Problems))
    End Function

    ''' <summary>ESIC workbook (.xls): sheet 1 upload, sheet 2 instructions. Needs Export on Statutory.</summary>
    Public Function BuildEsic(runId As Integer, Optional lastDayCodes As IReadOnlyCollection(Of Integer) = Nothing) As OperationResult(Of FileOutput(Of IReadOnlyList(Of ExportSheet)))
        Dim prepared As OperationResult(Of PayrollFileSnapshot) = Prepare(runId, ModuleKey.Statutory)
        If Not prepared.Succeeded Then
            Return OperationResult(Of FileOutput(Of IReadOnlyList(Of ExportSheet))).Fail(prepared.Message)
        End If
        Dim run As PayrollFileSnapshot = prepared.Value
        Dim mapped As MapResult(Of EsicRow) = EsicMapper.Map(run.Employees)
        Dim sheets As IReadOnlyList(Of ExportSheet) = Nothing
        Try
            sheets = EsicWorkbookBuilder.Build(mapped.Items, If(lastDayCodes, EsicReasonCodes.LastWorkingDayRequired), run.PeriodTo.Month)
        Catch ex As InvalidOperationException
            Return OperationResult(Of FileOutput(Of IReadOnlyList(Of ExportSheet))).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = Log(ModuleKey.Statutory, "BuildEsic", run, mapped.Items.Count, mapped.Problems.Count)
        If Not logged.Succeeded Then
            Return OperationResult(Of FileOutput(Of IReadOnlyList(Of ExportSheet))).Fail(logged.Message)
        End If
        Return OperationResult(Of FileOutput(Of IReadOnlyList(Of ExportSheet))).Ok(
            New FileOutput(Of IReadOnlyList(Of ExportSheet))(sheets, $"ESIC_{Stamp(run)}.xls", mapped.Problems))
    End Function

    ''' <summary>Bank files for one bank. Needs Export on Payroll. Batch reference at most 11 characters.</summary>
    Public Function BuildBank(runId As Integer, writer As IBankFileWriter, debitAccount As String, companyBranchOrIfsc As String,
                              paymentDate As Date, batchReference As String, description As String,
                              Optional mode As PaymentMode = PaymentMode.Neft) As OperationResult(Of FileOutput(Of BankFiles))
        If writer Is Nothing Then
            Throw New ArgumentNullException(NameOf(writer))
        End If
        Dim prepared As OperationResult(Of PayrollFileSnapshot) = Prepare(runId, ModuleKey.Payroll)
        If Not prepared.Succeeded Then
            Return OperationResult(Of FileOutput(Of BankFiles)).Fail(prepared.Message)
        End If
        Dim run As PayrollFileSnapshot = prepared.Value
        Dim mapped As MapResult(Of BankPayee) = BankBatchMapper.Map(run.Employees, writer.IfscPrefix, mode, If(batchReference, String.Empty))
        Dim files As BankFiles = Nothing
        Try
            files = writer.Build(BankBatchMapper.ToBatch(debitAccount, companyBranchOrIfsc, paymentDate, If(batchReference, String.Empty), description, mapped.Items))
        Catch ex As InvalidOperationException
            Return OperationResult(Of FileOutput(Of BankFiles)).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = Log(ModuleKey.Payroll, "BuildBankFile", run, mapped.Items.Count, mapped.Problems.Count)
        If Not logged.Succeeded Then
            Return OperationResult(Of FileOutput(Of BankFiles)).Fail(logged.Message)
        End If
        Return OperationResult(Of FileOutput(Of BankFiles)).Ok(New FileOutput(Of BankFiles)(files, $"BANK_{Stamp(run)}", mapped.Problems))
    End Function

    Private Function Prepare(runId As Integer, moduleKey As ModuleKey) As OperationResult(Of PayrollFileSnapshot)
        If Not _permissions.IsAllowed(moduleKey, PermissionAction.Export) Then
            Return OperationResult(Of PayrollFileSnapshot).Fail(NotAllowed)
        End If
        If runId <= 0 Then
            Return OperationResult(Of PayrollFileSnapshot).Fail("Pick a payroll run.")
        End If
        Dim run As PayrollFileSnapshot = _source.Load(runId)
        If run Is Nothing Then
            Return OperationResult(Of PayrollFileSnapshot).Fail("Payroll run not found.")
        End If
        If run.Status < PayrollRunStatus.Approved Then
            Return OperationResult(Of PayrollFileSnapshot).Fail("The run must be approved before files are built.")
        End If
        Return OperationResult(Of PayrollFileSnapshot).Ok(run)
    End Function

    Private Function Log(moduleKey As ModuleKey, action As String, run As PayrollFileSnapshot, lines As Integer, leftOut As Integer) As OperationResult
        Dim details As String = $"Lines {lines.ToString(CultureInfo.InvariantCulture)}. Left out {leftOut.ToString(CultureInfo.InvariantCulture)}."
        Return _audit.Record(New AuditEntry(moduleKey, action, "PayrollRun", run.RunId.ToString(CultureInfo.InvariantCulture), details))
    End Function

    Private Shared Function Stamp(run As PayrollFileSnapshot) As String
        Return run.PeriodTo.ToString("yyyyMM", CultureInfo.InvariantCulture) & "_" & run.RunId.ToString(CultureInfo.InvariantCulture)
    End Function
End Class
