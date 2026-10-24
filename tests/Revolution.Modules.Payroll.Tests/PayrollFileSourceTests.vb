Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Friend NotInheritable Class FakeRunRepository
    Implements IPayrollFileRepository

    Public Run As PayrollFileRun
    Public Rows As New List(Of PayrollFileRow)()
    Public RowCalls As Integer

    Public Function GetRun(runId As Integer) As PayrollFileRun Implements IPayrollFileRepository.GetRun
        Return Run
    End Function

    Public Function GetRows(runId As Integer) As IReadOnlyList(Of PayrollFileRow) Implements IPayrollFileRepository.GetRows
        RowCalls += 1
        Return Rows
    End Function
End Class

Friend NotInheritable Class FakeIdReleaser
    Implements IExportIdReleaser

    Public Calls As Integer
    Public LastKind As PayrollFileKind
    Public LastIds As List(Of Integer) = New List(Of Integer)()
    Public FailWith As String = String.Empty
    Public Ids As New Dictionary(Of Integer, ReleasedIds)()

    Public Function Release(kind As PayrollFileKind, employeeIds As IReadOnlyCollection(Of Integer)) As OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)) Implements IExportIdReleaser.Release
        Calls += 1
        LastKind = kind
        LastIds = employeeIds.ToList()
        If FailWith.Length > 0 Then Return OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)).Fail(FailWith)
        Return OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)).Ok(Ids)
    End Function
End Class

Public Class PayrollFileSourceTests

    Private ReadOnly _repository As New FakeRunRepository()
    Private ReadOnly _releaser As New FakeIdReleaser()

    Public Sub New()
        _repository.Run = New PayrollFileRun(7, New Date(2026, 9, 26), New Date(2026, 10, 25), PayrollRunStatus.Approved)
    End Sub

    Private Function CreateSource() As PayrollFileSource
        Return New PayrollFileSource(_repository, _releaser)
    End Function

    Private Shared Function Row(id As Integer, Optional epf As Boolean = False, Optional esi As Boolean = False,
                                Optional bank As Boolean = False, Optional hasAccount As Boolean = False,
                                Optional net As Decimal = 10000D, Optional exitDate As Date? = Nothing) As PayrollFileRow
        Return New PayrollFileRow With {
            .EmployeeID = id, .EmployeeCode = "E" & id.ToString("00000"), .Name = "Name " & id, .NetPay = net,
            .IsEpfMember = epf, .IsEsiMember = esi, .PaysByBank = bank, .HasAccount = hasAccount, .Ifsc = If(bank, "SBIN0001234", String.Empty),
            .ExitDate = exitDate}
    End Function

    <Fact>
    Public Sub RunNotFound_Fails_AndReleasesNothing()
        _repository.Run = Nothing

        Dim result = CreateSource().Load(7, PayrollFileKind.Ecr)

        Assert.False(result.Succeeded)
        Assert.Equal("Payroll run not found.", result.Message)
        Assert.Equal(0, _releaser.Calls)
    End Sub

    <Theory>
    <InlineData(PayrollRunStatus.Draft)>
    <InlineData(PayrollRunStatus.Processed)>
    <InlineData(PayrollRunStatus.Submitted)>
    Public Sub RunNotApproved_Fails_BeforeAnyIdIsReleased(status As PayrollRunStatus)
        _repository.Run = New PayrollFileRun(7, New Date(2026, 9, 26), New Date(2026, 10, 25), status)
        _repository.Rows.Add(Row(1, epf:=True))

        Dim result = CreateSource().Load(7, PayrollFileKind.Ecr)

        Assert.False(result.Succeeded)
        Assert.Equal(0, _releaser.Calls)
        Assert.Equal(0, _repository.RowCalls)
    End Sub

    <Theory>
    <InlineData(PayrollRunStatus.Approved)>
    <InlineData(PayrollRunStatus.Locked)>
    <InlineData(PayrollRunStatus.Paid)>
    Public Sub ApprovedLockedAndPaidRuns_Load(status As PayrollRunStatus)
        _repository.Run = New PayrollFileRun(7, New Date(2026, 9, 26), New Date(2026, 10, 25), status)

        Dim result = CreateSource().Load(7, PayrollFileKind.Ecr)

        Assert.True(result.Succeeded)
        Assert.Equal(status, result.Value.Status)
    End Sub

    <Fact>
    Public Sub Ecr_ReleasesIdsForEpfMembersOnly()
        _repository.Rows.Add(Row(1, epf:=True))
        _repository.Rows.Add(Row(2))
        _repository.Rows.Add(Row(3, epf:=True, esi:=True))
        _releaser.Ids(1) = New ReleasedIds With {.Uan = "100000000001"}
        _releaser.Ids(3) = New ReleasedIds With {.Uan = "100000000003"}

        Dim result = CreateSource().Load(7, PayrollFileKind.Ecr)

        Assert.True(result.Succeeded)
        Assert.Equal(PayrollFileKind.Ecr, _releaser.LastKind)
        Assert.Equal(New List(Of Integer) From {1, 3}, _releaser.LastIds)
        Assert.Equal("100000000001", result.Value.Employees(0).Uan)
        Assert.Equal(String.Empty, result.Value.Employees(1).Uan)
        Assert.Equal("100000000003", result.Value.Employees(2).Uan)
    End Sub

    <Fact>
    Public Sub Esic_ReleasesIdsForEsiMembersOnly()
        _repository.Rows.Add(Row(1, epf:=True))
        _repository.Rows.Add(Row(2, esi:=True))
        _releaser.Ids(2) = New ReleasedIds With {.Uan = "100000000002", .EsiIp = "1234567890"}

        Dim result = CreateSource().Load(7, PayrollFileKind.Esic)

        Assert.Equal(PayrollFileKind.Esic, _releaser.LastKind)
        Assert.Equal(New List(Of Integer) From {2}, _releaser.LastIds)
        Assert.Equal("1234567890", result.Value.Employees(1).EsiIpNumber)
    End Sub

    <Fact>
    Public Sub Bank_ReleasesAccountsForBankPayeesWithNetPayOnly()
        _repository.Rows.Add(Row(1, bank:=True, hasAccount:=True))
        _repository.Rows.Add(Row(2, bank:=True, hasAccount:=True, net:=0D))
        _repository.Rows.Add(Row(3, bank:=True, hasAccount:=False))
        _repository.Rows.Add(Row(4, bank:=False, hasAccount:=True))
        _releaser.Ids(1) = New ReleasedIds With {.AccountNumber = "111122223333"}

        Dim result = CreateSource().Load(7, PayrollFileKind.BankFile)

        Assert.Equal(PayrollFileKind.BankFile, _releaser.LastKind)
        Assert.Equal(New List(Of Integer) From {1}, _releaser.LastIds)
        Assert.Equal("111122223333", result.Value.Employees(0).AccountNumber)
        Assert.Equal("SBIN0001234", result.Value.Employees(0).Ifsc)
        Assert.True(result.Value.Employees(0).PaysByBank)
    End Sub

    <Fact>
    Public Sub NobodyNeedsAnId_ReleasesNothing_AndStillLoads()
        _repository.Rows.Add(Row(1))
        _repository.Rows.Add(Row(2, esi:=True))

        Dim result = CreateSource().Load(7, PayrollFileKind.Ecr)

        Assert.True(result.Succeeded)
        Assert.Equal(0, _releaser.Calls)
        Assert.Equal(2, result.Value.Employees.Count)
    End Sub

    <Fact>
    Public Sub ReleaseFailure_GivesNoSnapshot_AndPassesTheMessage()
        _repository.Rows.Add(Row(1, epf:=True))
        _releaser.FailWith = "The export could not be logged, so no ids were released. Try again or contact support."

        Dim result = CreateSource().Load(7, PayrollFileKind.Ecr)

        Assert.False(result.Succeeded)
        Assert.Equal(_releaser.FailWith, result.Message)
    End Sub

    <Fact>
    Public Sub LastWorkingDay_IsTheExitDate_OnlyInsideThePayPeriod()
        _repository.Rows.Add(Row(1, esi:=True, exitDate:=New Date(2026, 10, 12)))
        _repository.Rows.Add(Row(2, esi:=True, exitDate:=New Date(2026, 9, 20)))
        _repository.Rows.Add(Row(3, esi:=True))
        _releaser.Ids(1) = New ReleasedIds With {.EsiIp = "1234567890"}
        _releaser.Ids(2) = New ReleasedIds With {.EsiIp = "1234567891"}
        _releaser.Ids(3) = New ReleasedIds With {.EsiIp = "1234567892"}

        Dim employees = CreateSource().Load(7, PayrollFileKind.Esic).Value.Employees

        Assert.Equal(New Date(2026, 10, 12), employees(0).LastWorkingDay)
        Assert.Null(employees(1).LastWorkingDay)
        Assert.Null(employees(2).LastWorkingDay)
        Assert.Null(employees(0).EsiReasonCode)
    End Sub

    <Fact>
    Public Sub Snapshot_CarriesThePeriodAndTheLineFigures()
        Dim line As PayrollFileRow = Row(1)
        line.PaidDays = 26D
        line.GrossEarnings = 15000D
        line.EpfWage = 15000D
        line.EpfEmployee = 1800D
        line.EpsOnHigherWage = True
        _repository.Rows.Add(line)

        Dim result = CreateSource().Load(7, PayrollFileKind.Ecr)

        Assert.Equal(New Date(2026, 10, 25), result.Value.PeriodTo)
        Assert.Equal(7, result.Value.RunId)
        Assert.Equal(26D, result.Value.Employees(0).PaidDays)
        Assert.Equal(1800D, result.Value.Employees(0).EpfEmployee)
        Assert.True(result.Value.Employees(0).EpsOnHigherWage)
        Assert.Equal("E00001", result.Value.Employees(0).EmployeeCode)
    End Sub
End Class
