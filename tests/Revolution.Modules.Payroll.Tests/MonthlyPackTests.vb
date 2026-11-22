Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Friend NotInheritable Class FakeStatutoryPermissions
    Implements IPermissionService

    Public ReadOnly Allowed As New HashSet(Of PermissionAction)()

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Return moduleKey = ModuleKey.Statutory AndAlso Allowed.Contains(action)
    End Function
End Class

Friend NotInheritable Class FakePackRepository
    Implements IMonthlyPackRepository

    Public Figures As PackFigures
    Public ReadOnly Lwf As New List(Of PackStateAmount)()
    Public ReadOnly Filings As New List(Of PackFiling)()
    Public ReadOnly Due As New Dictionary(Of ComplianceKind, PackDue)()
    Public FailWith As String

    Public Function GetFigures(periodStart As Date) As PackFigures Implements IMonthlyPackRepository.GetFigures
        If FailWith IsNot Nothing Then Throw New PackRuleException(FailWith)
        Return Figures
    End Function

    Public Function LwfStates(periodStart As Date) As IReadOnlyList(Of PackStateAmount) Implements IMonthlyPackRepository.LwfStates
        Return Lwf
    End Function

    Public Function ListFilings(periodStart As Date) As IReadOnlyList(Of PackFiling) Implements IMonthlyPackRepository.ListFilings
        Return Filings.ToList()
    End Function

    Public Sub RecordFiling(periodStart As Date, kind As ComplianceKind, stateId As Integer?, status As PackStatus, amountPayable As Decimal?, employeeCount As Integer?,
                            filedOn As Date?, paidOn As Date?, referenceNo As String, userId As Integer?) Implements IMonthlyPackRepository.RecordFiling
        Dim existing = Filings.FirstOrDefault(Function(f) f.Kind = kind AndAlso Nullable.Equals(f.StateId, stateId))
        If status = PackStatus.Generated Then
            Filings.RemoveAll(Function(f) f Is existing)
            Filings.Add(New PackFiling(kind, stateId, PackStatus.Generated, amountPayable.Value, employeeCount.Value, Nothing, Nothing, Nothing))
            Return
        End If
        If existing Is Nothing Then Throw New PackRuleException("This item was never generated for the month.")
        Filings.Remove(existing)
        Filings.Add(New PackFiling(kind, stateId, status, existing.AmountPayable, existing.EmployeeCount, filedOn, paidOn, referenceNo))
    End Sub

    Public Function ResolveDue(kind As ComplianceKind, stateId As Integer?, periodStart As Date) As PackDue Implements IMonthlyPackRepository.ResolveDue
        Dim answer As PackDue = Nothing
        Return If(Due.TryGetValue(kind, answer), answer, New PackDue(Nothing, False, False))
    End Function
End Class

Friend NotInheritable Class FakePackFiles
    Implements IPackFiles

    Public EcrFailWith As String
    Public ReadOnly Problems As New List(Of String)()
    Public EcrRunId As Integer

    Public Function Ecr(runId As Integer, periodTo As Date) As OperationResult(Of FileOutput(Of String)) Implements IPackFiles.Ecr
        If EcrFailWith IsNot Nothing Then Return OperationResult(Of FileOutput(Of String)).Fail(EcrFailWith)
        EcrRunId = runId
        Return OperationResult(Of FileOutput(Of String)).Ok(New FileOutput(Of String)("ECR text", "ECR_202610.txt", Problems))
    End Function

    Public Function Esic(runId As Integer) As OperationResult(Of FileOutput(Of IReadOnlyList(Of ExportSheet))) Implements IPackFiles.Esic
        Dim sheets As IReadOnlyList(Of ExportSheet) = New List(Of ExportSheet) From {New ExportSheet("Upload", New List(Of String()) From {New String() {"IP"}})}
        Return OperationResult(Of FileOutput(Of IReadOnlyList(Of ExportSheet))).Ok(New FileOutput(Of IReadOnlyList(Of ExportSheet))(sheets, "ESIC_202610.xls", New List(Of String)()))
    End Function
End Class

Friend NotInheritable Class FakePackWriter
    Implements IPackFileWriter

    Public ReadOnly Saved As New List(Of String)()
    Public FailName As String

    Public Function SaveText(folder As String, fileName As String, text As String) As OperationResult(Of String) Implements IPackFileWriter.SaveText
        Return Save(folder, fileName)
    End Function

    Public Function SaveSheets(folder As String, fileName As String, sheets As IReadOnlyList(Of ExportSheet)) As OperationResult(Of String) Implements IPackFileWriter.SaveSheets
        Return Save(folder, fileName)
    End Function

    Private Function Save(folder As String, fileName As String) As OperationResult(Of String)
        If FailName IsNot Nothing AndAlso fileName.StartsWith(FailName, StringComparison.Ordinal) Then Return OperationResult(Of String).Fail("Disk full.")
        Saved.Add(fileName)
        Return OperationResult(Of String).Ok(folder & "\" & fileName)
    End Function
End Class

Public Class MonthlyPackTests
    Private Shared ReadOnly October As New Date(2026, 10, 1)
    Private Const Folder As String = "C:\Packs"

    Private ReadOnly _repo As New FakePackRepository()
    Private ReadOnly _files As New FakePackFiles()
    Private ReadOnly _writer As New FakePackWriter()
    Private ReadOnly _perms As New FakeStatutoryPermissions()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _audit As New FakePayAudit()
    Private ReadOnly _service As MonthlyPackService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Export)
        _perms.Allowed.Add(PermissionAction.Edit)
        _repo.Figures = Approved()
        _service = New MonthlyPackService(_repo, _files, _writer, _perms, _plan, _audit, New FakePaySession(), Function() New Date(2026, 11, 20))
    End Sub

    Private Shared Function Approved(Optional epfEmployee As Decimal = 1000D) As PackFigures
        Return New PackFigures("Acme", 41, PayrollRunStatus.Approved, New Date(2026, 10, 31), epfEmployee, 833D, 167D, 50D, 100D, 20, 200D, 500D, 18,
                               New List(Of PackStateAmount) From {New PackStateAmount(6, "Haryana", 2400D, 12)})
    End Function

    Private Shared Function Unapproved(status As PayrollRunStatus?) As PackFigures
        Return New PackFigures("Acme", If(status.HasValue, 41, CType(Nothing, Integer?)), status, If(status.HasValue, New Date(2026, 10, 31), CType(Nothing, Date?)),
                               0D, 0D, 0D, 0D, 0D, 0, 0D, 0D, 0, New List(Of PackStateAmount)())
    End Function

    Private Function Pack() As MonthlyPack
        Dim r = _service.GetPack(October)
        Assert.True(r.Succeeded, r.Message)
        Return r.Value
    End Function

    Private Function LineOf(kind As ComplianceKind) As MonthlyPackLine
        Return Pack().Lines.Single(Function(l) l.Kind = kind AndAlso Not (kind = ComplianceKind.ProfessionalTax AndAlso l.StateId.GetValueOrDefault(-1) <> 6))
    End Function

    ' ---- reading the pack ----

    <Fact>
    Public Sub NoRun_EveryLineIsNotReady_WithTheReason()
        _repo.Figures = Unapproved(Nothing)
        Dim p = Pack()
        Assert.Equal("There is no payroll for this month yet.", p.PayrollMessage)
        Assert.All(p.Lines, Sub(l) Assert.Equal(PackStatus.NotReady, l.Status))
        Assert.Contains(p.Lines, Function(l) l.Kind = ComplianceKind.Epf)
        Assert.DoesNotContain(p.Lines, Function(l) l.Kind = ComplianceKind.ProfessionalTax)
    End Sub

    <Theory>
    <InlineData(PayrollRunStatus.Draft)>
    <InlineData(PayrollRunStatus.Processed)>
    <InlineData(PayrollRunStatus.Submitted)>
    Public Sub RunBeforeApproval_IsNotReady(status As PayrollRunStatus)
        _repo.Figures = Unapproved(status)
        Dim epf = LineOf(ComplianceKind.Epf)
        Assert.Equal(PackStatus.NotReady, epf.Status)
        Assert.Equal("Payroll for this month is not approved yet.", epf.Message)
        Assert.False(epf.AmountPayable.HasValue)
    End Sub

    <Fact>
    Public Sub ApprovedRun_GivesReadyLines_WithAmountsAndParts()
        Dim epf = LineOf(ComplianceKind.Epf)
        Assert.Equal(PackStatus.Ready, epf.Status)
        Assert.Equal(2150D, epf.AmountPayable.Value)
        Assert.Equal(20, epf.EmployeeCount)
        Assert.Equal(5, epf.Components.Count)
        Dim esic = LineOf(ComplianceKind.Esic)
        Assert.Equal(700D, esic.AmountPayable.Value)
        Assert.Equal(18, esic.EmployeeCount)
        Dim pt = LineOf(ComplianceKind.ProfessionalTax)
        Assert.Equal(2400D, pt.AmountPayable.Value)
        Assert.Equal("Haryana", pt.StateName)
        Assert.Equal(PackStatus.Ready, pt.Status)
    End Sub

    <Fact>
    Public Sub Tds_IsNotReady_BecausePayrollDoesNotCalculateItYet()
        Dim tds = LineOf(ComplianceKind.Tds)
        Assert.Equal(PackStatus.NotReady, tds.Status)
        Assert.Contains("not calculated", tds.Message, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub LabourWelfare_AppearsOnlyForStatesDueInTheMonth_AndIsNotReady()
        Assert.DoesNotContain(Pack().Lines, Function(l) l.Kind = ComplianceKind.LabourWelfare)
        _repo.Lwf.Add(New PackStateAmount(6, "Haryana", 0D, 0))
        Dim lwf = LineOf(ComplianceKind.LabourWelfare)
        Assert.Equal(PackStatus.NotReady, lwf.Status)
        Assert.Equal(6, lwf.StateId.Value)
    End Sub

    <Fact>
    Public Sub PlanDecidesWhichLinesAppear()
        _plan.Disabled.Add(PlanFeature.EpfEsi)
        _plan.Disabled.Add(PlanFeature.ProfessionalTax)
        _plan.Disabled.Add(PlanFeature.Tds)
        Assert.Empty(Pack().Lines)
    End Sub

    <Fact>
    Public Sub DueDate_Verified_Unverified_AndMissing()
        _repo.Due(ComplianceKind.Epf) = New PackDue(New Date(2026, 11, 15), True, True)
        _repo.Due(ComplianceKind.Esic) = New PackDue(Nothing, True, False)
        Dim p = Pack()
        Dim epf = p.Lines.Single(Function(l) l.Kind = ComplianceKind.Epf)
        Assert.Equal(New Date(2026, 11, 15), epf.DueDate.Value)
        Assert.Equal(String.Empty, epf.DueNote)
        Dim esic = p.Lines.Single(Function(l) l.Kind = ComplianceKind.Esic)
        Assert.False(esic.DueDate.HasValue)
        Assert.Equal("Date not verified", esic.DueNote)
        Assert.Equal("Date not set", p.Lines.Single(Function(l) l.Kind = ComplianceKind.Tds).DueNote)
    End Sub

    <Fact>
    Public Sub GetPack_NeedsView_AndAFirstOfTheMonth()
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.GetPack(October).Succeeded)
        _perms.Allowed.Add(PermissionAction.View)
        Assert.False(_service.GetPack(New Date(2026, 10, 5)).Succeeded)
    End Sub

    <Fact>
    Public Sub RepositoryRuleBreak_BecomesAMessage()
        _repo.FailWith = "The month must start on the first day."
        Dim r = _service.GetPack(October)
        Assert.False(r.Succeeded)
        Assert.Equal("The month must start on the first day.", r.Message)
    End Sub

    ' ---- generating ----

    <Fact>
    Public Sub Generate_Epf_WritesTheEcr_RecordsIt_AndAudits()
        Dim r = _service.Generate(October, LineOf(ComplianceKind.Epf), Folder)
        Assert.True(r.Succeeded, r.Message)
        Assert.Equal("ECR_202610.txt", _writer.Saved.Single())
        Assert.Equal(41, _files.EcrRunId)
        Assert.Equal(PackStatus.Generated, LineOf(ComplianceKind.Epf).Status)
        Assert.Equal(2150D, _repo.Filings.Single().AmountPayable)
        Assert.Contains(_audit.Entries, Function(e) e.Action = "GeneratePackLine")
    End Sub

    <Fact>
    Public Sub Generate_Esic_WritesTheWorkbook()
        Assert.True(_service.Generate(October, LineOf(ComplianceKind.Esic), Folder).Succeeded)
        Assert.Equal("ESIC_202610.xls", _writer.Saved.Single())
    End Sub

    <Fact>
    Public Sub Generate_ProfessionalTax_WritesASummaryWorkbook()
        Assert.True(_service.Generate(October, LineOf(ComplianceKind.ProfessionalTax), Folder).Succeeded)
        Assert.Equal("Pack_ProfessionalTax_Haryana_2026-10.xls", _writer.Saved.Single())
    End Sub

    <Fact>
    Public Sub Generate_ReportsSkippedEmployees()
        _files.Problems.Add("E004 has no UAN")
        Dim r = _service.Generate(October, LineOf(ComplianceKind.Epf), Folder)
        Assert.True(r.Succeeded)
        Assert.Contains("1 employee(s) skipped", r.Value, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub Generate_Refusals()
        Dim epf = LineOf(ComplianceKind.Epf)
        _perms.Allowed.Remove(PermissionAction.Export)
        Assert.False(_service.Generate(October, epf, Folder).Succeeded)
        _perms.Allowed.Add(PermissionAction.Export)
        Assert.False(_service.Generate(October, epf, "  ").Succeeded)
        Assert.False(_service.Generate(October, LineOf(ComplianceKind.Tds), Folder).Succeeded)
        Assert.Empty(_writer.Saved)
    End Sub

    <Fact>
    Public Sub Generate_AFiledLine_IsRefused()
        Dim epf = LineOf(ComplianceKind.Epf)
        Assert.True(_service.Generate(October, epf, Folder).Succeeded)
        Assert.True(_service.Mark(October, LineOf(ComplianceKind.Epf), PackStatus.Filed, "ACK1", New Date(2026, 11, 10)).Succeeded)
        Dim again = _service.Generate(October, LineOf(ComplianceKind.Epf), Folder)
        Assert.False(again.Succeeded)
        Assert.Equal("This item is already filed or paid.", again.Message)
    End Sub

    <Fact>
    Public Sub Generate_FailureOfTheFileWriter_RecordsNothing()
        _writer.FailName = "ECR"
        Dim r = _service.Generate(October, LineOf(ComplianceKind.Epf), Folder)
        Assert.False(r.Succeeded)
        Assert.Empty(_repo.Filings)
    End Sub

    <Fact>
    Public Sub Generate_AuditFailure_IsReported()
        _audit.Fail = True
        Assert.False(_service.Generate(October, LineOf(ComplianceKind.Epf), Folder).Succeeded)
    End Sub

    <Fact>
    Public Sub ChangedFigures_MakeTheLineChanged_AndItCanBeGeneratedAgain()
        Assert.True(_service.Generate(October, LineOf(ComplianceKind.Epf), Folder).Succeeded)
        _repo.Figures = Approved(1100D)
        Dim epf = LineOf(ComplianceKind.Epf)
        Assert.Equal(PackStatus.Changed, epf.Status)
        Assert.True(_service.Generate(October, epf, Folder).Succeeded)
        Assert.Equal(PackStatus.Generated, LineOf(ComplianceKind.Epf).Status)
    End Sub

    ' ---- generate all ----

    <Fact>
    Public Sub GenerateAll_MakesTheReadyLines_AndASummary()
        Dim r = _service.GenerateAll(October, Folder)
        Assert.True(r.Succeeded, r.Message)
        Assert.Equal({"ECR_202610.txt", "ESIC_202610.xls", "Pack_ProfessionalTax_Haryana_2026-10.xls", "Pack_2026-10.xls"}, _writer.Saved)
        Assert.Equal(4, r.Value.Count)
        Assert.Equal(3, _repo.Filings.Count)
    End Sub

    <Fact>
    Public Sub GenerateAll_ListsAFailedLine_ButStillMakesTheRest()
        _writer.FailName = "ESIC"
        Dim r = _service.GenerateAll(October, Folder)
        Assert.True(r.Succeeded)
        Assert.Contains(r.Value, Function(x) x.StartsWith("Not generated - ESIC", StringComparison.Ordinal))
        Assert.Equal(2, _repo.Filings.Count)
    End Sub

    <Fact>
    Public Sub GenerateAll_NothingReady_Fails()
        _repo.Figures = Unapproved(PayrollRunStatus.Processed)
        Dim r = _service.GenerateAll(October, Folder)
        Assert.False(r.Succeeded)
        Assert.Equal("Nothing is ready to generate.", r.Message)
    End Sub

    <Fact>
    Public Sub GenerateAll_SkipsFiledLines()
        Assert.True(_service.Generate(October, LineOf(ComplianceKind.Epf), Folder).Succeeded)
        Assert.True(_service.Mark(October, LineOf(ComplianceKind.Epf), PackStatus.Paid, "CH9", New Date(2026, 11, 12)).Succeeded)
        _writer.Saved.Clear()
        Assert.True(_service.GenerateAll(October, Folder).Succeeded)
        Assert.DoesNotContain("ECR_202610.txt", _writer.Saved)
    End Sub

    ' ---- marking ----

    <Fact>
    Public Sub Mark_Filed_KeepsTheReferenceAndTheDate()
        Assert.True(_service.Generate(October, LineOf(ComplianceKind.Epf), Folder).Succeeded)
        Dim r = _service.Mark(October, LineOf(ComplianceKind.Epf), PackStatus.Filed, "  ACK123  ", New Date(2026, 11, 10))
        Assert.True(r.Succeeded, r.Message)
        Dim epf = LineOf(ComplianceKind.Epf)
        Assert.Equal(PackStatus.Filed, epf.Status)
        Assert.Equal("ACK123", epf.Reference)
        Assert.Equal(New Date(2026, 11, 10), _repo.Filings.Single().FiledOn.Value)
        Assert.Contains(_audit.Entries, Function(e) e.Action = "MarkPackLine")
    End Sub

    <Fact>
    Public Sub Mark_Paid_StoresThePaidDate()
        Assert.True(_service.Generate(October, LineOf(ComplianceKind.Epf), Folder).Succeeded)
        Assert.True(_service.Mark(October, LineOf(ComplianceKind.Epf), PackStatus.Paid, "CHALLAN7", New Date(2026, 11, 12)).Succeeded)
        Assert.Equal(New Date(2026, 11, 12), _repo.Filings.Single().PaidOn.Value)
        Assert.Equal(PackStatus.Paid, LineOf(ComplianceKind.Epf).Status)
    End Sub

    <Fact>
    Public Sub Mark_Refusals()
        Dim epf = LineOf(ComplianceKind.Epf)
        Assert.Equal("Generate this item before marking it.", _service.Mark(October, epf, PackStatus.Filed, "A", New Date(2026, 11, 10)).Message)
        Assert.True(_service.Generate(October, epf, Folder).Succeeded)
        epf = LineOf(ComplianceKind.Epf)
        Assert.False(_service.Mark(October, epf, PackStatus.Ready, "A", New Date(2026, 11, 10)).Succeeded)
        Assert.False(_service.Mark(October, epf, PackStatus.Filed, "  ", New Date(2026, 11, 10)).Succeeded)
        Assert.False(_service.Mark(October, epf, PackStatus.Filed, New String("X"c, 41), New Date(2026, 11, 10)).Succeeded)
        Assert.False(_service.Mark(October, epf, PackStatus.Filed, "A", New Date(2026, 11, 21)).Succeeded)
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.Mark(October, epf, PackStatus.Filed, "A", New Date(2026, 11, 10)).Succeeded)
    End Sub

    <Fact>
    Public Sub Mark_AChangedLine_AsksForANewGeneration()
        Assert.True(_service.Generate(October, LineOf(ComplianceKind.Epf), Folder).Succeeded)
        _repo.Figures = Approved(1100D)
        Dim r = _service.Mark(October, LineOf(ComplianceKind.Epf), PackStatus.Filed, "A", New Date(2026, 11, 10))
        Assert.False(r.Succeeded)
        Assert.Contains("Generate it again", r.Message, StringComparison.Ordinal)
    End Sub
End Class
