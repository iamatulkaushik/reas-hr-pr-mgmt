Imports Revolution.Core

''' <summary>
''' The real IPayrollFileSource. Order matters (ADR 0019): find the run, refuse before approval, and only then release ids,
''' and only for the employees the file needs. Nothing is released when nobody needs an id.
''' Last working day is the exit date when it falls inside the pay period. The ESIC reason code has no source yet and stays empty (0).
''' </summary>
Public NotInheritable Class PayrollFileSource
    Implements IPayrollFileSource

    Private Const NotFound As String = "Payroll run not found."
    Private Const NotApproved As String = "The run must be approved before files are built."

    Private ReadOnly _repository As IPayrollFileRepository
    Private ReadOnly _releaser As IExportIdReleaser

    Public Sub New(repository As IPayrollFileRepository, releaser As IExportIdReleaser)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If releaser Is Nothing Then Throw New ArgumentNullException(NameOf(releaser))
        _repository = repository
        _releaser = releaser
    End Sub

    Public Function Load(runId As Integer, kind As PayrollFileKind) As OperationResult(Of PayrollFileSnapshot) Implements IPayrollFileSource.Load
        If Not [Enum].IsDefined(GetType(PayrollFileKind), kind) Then Throw New ArgumentOutOfRangeException(NameOf(kind))
        Dim run As PayrollFileRun = _repository.GetRun(runId)
        If run Is Nothing Then Return OperationResult(Of PayrollFileSnapshot).Fail(NotFound)
        If run.Status < PayrollRunStatus.Approved Then Return OperationResult(Of PayrollFileSnapshot).Fail(NotApproved)

        Dim rows As IReadOnlyList(Of PayrollFileRow) = _repository.GetRows(runId)
        Dim needed As List(Of Integer) = rows.Where(Function(r) NeedsIds(kind, r)).Select(Function(r) r.EmployeeID).ToList()
        Dim released As IReadOnlyDictionary(Of Integer, ReleasedIds) = New Dictionary(Of Integer, ReleasedIds)()
        If needed.Count > 0 Then
            Dim result As OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)) = _releaser.Release(kind, needed)
            If Not result.Succeeded Then Return OperationResult(Of PayrollFileSnapshot).Fail(result.Message)
            released = result.Value
        End If

        Dim employees As List(Of PayrollFileEmployee) = rows.Select(Function(r) ToEmployee(r, run, released)).ToList()
        Return OperationResult(Of PayrollFileSnapshot).Ok(New PayrollFileSnapshot(run.RunId, run.PeriodFrom, run.PeriodTo, run.Status, employees))
    End Function

    Private Shared Function NeedsIds(kind As PayrollFileKind, row As PayrollFileRow) As Boolean
        Select Case kind
            Case PayrollFileKind.Ecr
                Return row.IsEpfMember
            Case PayrollFileKind.Esic
                Return row.IsEsiMember
            Case Else
                Return row.PaysByBank AndAlso row.HasAccount AndAlso row.NetPay > 0D
        End Select
    End Function

    Private Shared Function ToEmployee(row As PayrollFileRow, run As PayrollFileRun, released As IReadOnlyDictionary(Of Integer, ReleasedIds)) As PayrollFileEmployee
        Dim ids As ReleasedIds = Nothing
        released.TryGetValue(row.EmployeeID, ids)
        Dim lastDay As Date? = Nothing
        If row.ExitDate.HasValue AndAlso row.ExitDate.Value.Date >= run.PeriodFrom.Date AndAlso row.ExitDate.Value.Date <= run.PeriodTo.Date Then
            lastDay = row.ExitDate.Value.Date
        End If
        Return New PayrollFileEmployee With {
            .EmployeeID = row.EmployeeID,
            .EmployeeCode = row.EmployeeCode,
            .Name = row.Name,
            .PaidDays = row.PaidDays,
            .GrossEarnings = row.GrossEarnings,
            .EpfWage = row.EpfWage,
            .EsiWage = row.EsiWage,
            .EpfEmployee = row.EpfEmployee,
            .EpfEmployerEpf = row.EpfEmployerEpf,
            .EpfEmployerEps = row.EpfEmployerEps,
            .Edli = row.Edli,
            .EsiEmployee = row.EsiEmployee,
            .EsiEmployer = row.EsiEmployer,
            .NetPay = row.NetPay,
            .IsEpfMember = row.IsEpfMember,
            .Uan = If(ids Is Nothing, String.Empty, ids.Uan),
            .EpsOnHigherWage = row.EpsOnHigherWage,
            .IsEsiMember = row.IsEsiMember,
            .EsiIpNumber = If(ids Is Nothing, String.Empty, ids.EsiIp),
            .PaysByBank = row.PaysByBank,
            .Ifsc = row.Ifsc,
            .AccountNumber = If(ids Is Nothing, String.Empty, ids.AccountNumber),
            .EsiReasonCode = Nothing,
            .LastWorkingDay = lastDay}
    End Function
End Class
