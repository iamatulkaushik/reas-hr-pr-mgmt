Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' The Monthly Pack (ADR 0027): one line per statutory item for the open company and a month, with status, amount, employees and due date; files for the ready lines;
''' and the Associate's record of what was filed and paid. Figures come only from the month's approved, locked or paid payroll.
''' Lines: EPF and ESIC (plan feature EpfEsi), professional tax per state with an amount (ProfessionalTax), TDS (Tds), and labour welfare for a state whose rule falls due in the month.
''' TDS and labour welfare are always Not ready for now, with the reason: payroll does not calculate tax deducted yet, and the Haryana labour welfare wage definition and due date are open (CA pack part G).
''' Needs View on Statutory to read, Export to make files and Edit to mark Filed or Paid. Audit text never holds an amount or an id.
''' </summary>
Public NotInheritable Class MonthlyPackService
    Implements IMonthlyPackService

    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const MaxReferenceLength As Integer = 40
    Private Const NoPayroll As String = "There is no payroll for this month yet."
    Private Const NotApproved As String = "Payroll for this month is not approved yet."
    Private Const TdsNotInPayroll As String = "Tax deducted from salary is not calculated in payroll yet."
    Private Const LwfOpen As String = "Labour welfare is not calculated yet: its wage definition and due date are still open."
    Private Const DateNotVerified As String = "Date not verified"
    Private Const DateNotSet As String = "Date not set"
    Private Const MonthFormat As String = "yyyy-MM"

    Private ReadOnly _repository As IMonthlyPackRepository
    Private ReadOnly _files As IPackFiles
    Private ReadOnly _writer As IPackFileWriter
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _today As Func(Of Date)

    Public Sub New(repository As IMonthlyPackRepository, files As IPackFiles, writer As IPackFileWriter, permissions As IPermissionService,
                   entitlements As IEntitlementService, audit As IAuditService, session As ISessionContext, today As Func(Of Date))
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If files Is Nothing Then Throw New ArgumentNullException(NameOf(files))
        If writer Is Nothing Then Throw New ArgumentNullException(NameOf(writer))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        If today Is Nothing Then Throw New ArgumentNullException(NameOf(today))
        _repository = repository
        _files = files
        _writer = writer
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _session = session
        _today = today
    End Sub

    Public Function GetPack(periodStart As Date) As OperationResult(Of MonthlyPack) Implements IMonthlyPackService.GetPack
        Dim gate = Check(PermissionAction.View, periodStart)
        If Not gate.Succeeded Then Return OperationResult(Of MonthlyPack).Fail(gate.Message)
        Try
            Return OperationResult(Of MonthlyPack).Ok(BuildPack(periodStart.Date))
        Catch ex As PackRuleException
            Return OperationResult(Of MonthlyPack).Fail(ex.Message)
        End Try
    End Function

    Public Function Generate(periodStart As Date, line As MonthlyPackLine, folder As String) As OperationResult(Of String) Implements IMonthlyPackService.Generate
        If line Is Nothing Then Throw New ArgumentNullException(NameOf(line))
        Dim gate = Check(PermissionAction.Export, periodStart)
        If Not gate.Succeeded Then Return OperationResult(Of String).Fail(gate.Message)
        If String.IsNullOrWhiteSpace(folder) Then Return OperationResult(Of String).Fail("Choose a folder.")
        Try
            Dim figures = _repository.GetFigures(periodStart.Date)
            Dim pack = BuildPack(periodStart.Date, figures)
            Dim current = pack.Lines.FirstOrDefault(Function(l) Same(l, line))
            If current Is Nothing Then Return OperationResult(Of String).Fail("This item is not part of the month's pack.")
            Return GenerateOne(periodStart.Date, current, figures, pack, folder)
        Catch ex As PackRuleException
            Return OperationResult(Of String).Fail(ex.Message)
        End Try
    End Function

    ''' <summary>
    ''' Makes every Ready or Changed line, then a summary workbook of the whole pack. Each item of the list is a path written, or "Not generated - title: reason" for a line that failed.
    ''' Fails only when nothing at all could be made.
    ''' </summary>
    Public Function GenerateAll(periodStart As Date, folder As String) As OperationResult(Of IReadOnlyList(Of String)) Implements IMonthlyPackService.GenerateAll
        Dim gate = Check(PermissionAction.Export, periodStart)
        If Not gate.Succeeded Then Return OperationResult(Of IReadOnlyList(Of String)).Fail(gate.Message)
        If String.IsNullOrWhiteSpace(folder) Then Return OperationResult(Of IReadOnlyList(Of String)).Fail("Choose a folder.")
        Dim results As New List(Of String)()
        Dim made As Integer = 0
        Try
            Dim figures = _repository.GetFigures(periodStart.Date)
            Dim pack = BuildPack(periodStart.Date, figures)
            Dim targets = pack.Lines.Where(Function(l) l.Status = PackStatus.Ready OrElse l.Status = PackStatus.Changed).ToList()
            If targets.Count = 0 Then Return OperationResult(Of IReadOnlyList(Of String)).Fail("Nothing is ready to generate.")
            For Each target In targets
                Dim one = GenerateOne(periodStart.Date, target, figures, pack, folder)
                If one.Succeeded Then
                    made += 1
                    results.Add(one.Value)
                Else
                    results.Add($"Not generated - {target.Title}: {one.Message}")
                End If
            Next
            Dim refreshed = BuildPack(periodStart.Date)
            Dim summary = _writer.SaveSheets(folder, $"Pack_{periodStart.ToString(MonthFormat, CultureInfo.InvariantCulture)}.xls", MonthlyPackSheetBuilder.Build(refreshed))
            If summary.Succeeded Then results.Add(summary.Value) Else results.Add($"Not generated - summary workbook: {summary.Message}")
        Catch ex As PackRuleException
            If made = 0 Then Return OperationResult(Of IReadOnlyList(Of String)).Fail(ex.Message)
            results.Add($"Stopped: {ex.Message}")
        End Try
        If made = 0 Then Return OperationResult(Of IReadOnlyList(Of String)).Fail(results(0))
        Return OperationResult(Of IReadOnlyList(Of String)).Ok(results)
    End Function

    ''' <summary>Marks a generated line Filed or Paid with the acknowledgement or challan reference and the date. The date cannot be in the future.</summary>
    Public Function Mark(periodStart As Date, line As MonthlyPackLine, status As PackStatus, reference As String, onDate As Date) As OperationResult Implements IMonthlyPackService.Mark
        If line Is Nothing Then Throw New ArgumentNullException(NameOf(line))
        Dim gate = Check(PermissionAction.Edit, periodStart)
        If Not gate.Succeeded Then Return gate
        If status <> PackStatus.Filed AndAlso status <> PackStatus.Paid Then Return OperationResult.Fail("A line is marked Filed or Paid.")
        Dim ref = If(reference, String.Empty).Trim()
        If ref.Length = 0 Then Return OperationResult.Fail("Enter the acknowledgement or challan number.")
        If ref.Length > MaxReferenceLength Then Return OperationResult.Fail("The reference is too long (40 characters at most).")
        If onDate.Date > _today().Date Then Return OperationResult.Fail("The date cannot be in the future.")
        Try
            Dim pack = BuildPack(periodStart.Date)
            Dim current = pack.Lines.FirstOrDefault(Function(l) Same(l, line))
            If current Is Nothing Then Return OperationResult.Fail("This item is not part of the month's pack.")
            If current.Status = PackStatus.Changed Then Return OperationResult.Fail("The figures changed since this item was generated. Generate it again first.")
            If current.Status < PackStatus.Generated Then Return OperationResult.Fail("Generate this item before marking it.")
            _repository.RecordFiling(periodStart.Date, current.Kind, current.StateId, status, Nothing, Nothing,
                                     If(status = PackStatus.Filed, onDate.Date, CType(Nothing, Date?)), If(status = PackStatus.Paid, onDate.Date, CType(Nothing, Date?)), ref, CurrentUserId())
        Catch ex As PackRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Statutory, "MarkPackLine", "StatutoryFiling", Key(periodStart, line), status.ToString()))
    End Function

    ' ---- building the pack ----

    Private Function BuildPack(periodStart As Date) As MonthlyPack
        Return BuildPack(periodStart, _repository.GetFigures(periodStart))
    End Function

    Private Function BuildPack(periodStart As Date, figures As PackFigures) As MonthlyPack
        Dim filings = _repository.ListFilings(periodStart)
        Dim reason As String = If(Not figures.RunId.HasValue, NoPayroll, If(figures.HasApprovedRun, String.Empty, NotApproved))
        Dim lines As New List(Of MonthlyPackLine)()
        If _entitlements.IsEnabled(PlanFeature.EpfEsi) Then
            lines.Add(Line(periodStart, ComplianceKind.Epf, "EPF", Nothing, String.Empty, figures.EpfTotal, figures.EpfCount, reason, filings,
                           New List(Of PackAmount) From {New PackAmount("Employee share", figures.EpfEmployee), New PackAmount("Employer share to EPF", figures.EpfEmployerEpf),
                                                        New PackAmount("Employer share to EPS", figures.EpfEmployerEps), New PackAmount("EDLI", figures.Edli),
                                                        New PackAmount("Admin charges", figures.EpfAdmin)}))
            lines.Add(Line(periodStart, ComplianceKind.Esic, "ESIC", Nothing, String.Empty, figures.EsiTotal, figures.EsiCount, reason, filings,
                           New List(Of PackAmount) From {New PackAmount("Employee share", figures.EsiEmployee), New PackAmount("Employer share", figures.EsiEmployer)}))
        End If
        If _entitlements.IsEnabled(PlanFeature.ProfessionalTax) AndAlso figures.HasApprovedRun Then
            For Each state In figures.ProfessionalTax
                lines.Add(Line(periodStart, ComplianceKind.ProfessionalTax, $"Professional tax - {state.StateName}", state.StateId, state.StateName, state.Amount, state.EmployeeCount,
                               String.Empty, filings, Nothing))
            Next
        End If
        If _entitlements.IsEnabled(PlanFeature.Tds) Then
            lines.Add(Line(periodStart, ComplianceKind.Tds, "Tax deducted from salary (TDS)", Nothing, String.Empty, Nothing, 0, TdsNotInPayroll, filings, Nothing))
        End If
        For Each state In _repository.LwfStates(periodStart)
            lines.Add(Line(periodStart, ComplianceKind.LabourWelfare, $"Labour welfare - {state.StateName}", state.StateId, state.StateName, Nothing, 0, LwfOpen, filings, Nothing))
        Next
        Return New MonthlyPack(figures.CompanyName, periodStart, lines, reason)
    End Function

    ''' <summary>One line: Not ready with the reason when the payroll is not usable; otherwise Ready, the recorded status, or Changed when the figures differ from what was generated.</summary>
    Private Function Line(periodStart As Date, kind As ComplianceKind, title As String, stateId As Integer?, stateName As String, amount As Decimal?, employees As Integer,
                          notReadyReason As String, filings As IReadOnlyList(Of PackFiling), components As IReadOnlyList(Of PackAmount)) As MonthlyPackLine
        Dim filing = filings.FirstOrDefault(Function(f) f.Kind = kind AndAlso Nullable.Equals(f.StateId, stateId))
        Dim status As PackStatus
        Dim message As String = notReadyReason
        If notReadyReason.Length > 0 Then
            status = PackStatus.NotReady
        ElseIf filing Is Nothing Then
            status = PackStatus.Ready
        ElseIf filing.AmountPayable <> amount.GetValueOrDefault() OrElse filing.EmployeeCount <> employees Then
            status = PackStatus.Changed
            message = "The figures changed since this item was generated."
        Else
            status = filing.Status
        End If
        Dim due = _repository.ResolveDue(kind, stateId, periodStart)
        Dim dueNote As String = If(due.DueDate.HasValue, String.Empty, If(due.HasRule AndAlso Not due.IsVerified, DateNotVerified, DateNotSet))
        Return New MonthlyPackLine(kind, title, stateId, stateName, status, If(status = PackStatus.NotReady, CType(Nothing, Decimal?), amount), employees,
                                   due.DueDate, dueNote, If(filing Is Nothing, String.Empty, filing.ReferenceNo), message, components)
    End Function

    ' ---- making files ----

    Private Function GenerateOne(periodStart As Date, line As MonthlyPackLine, figures As PackFigures, pack As MonthlyPack, folder As String) As OperationResult(Of String)
        If line.Status = PackStatus.NotReady Then Return OperationResult(Of String).Fail(line.Message)
        If line.Status = PackStatus.Filed OrElse line.Status = PackStatus.Paid Then Return OperationResult(Of String).Fail("This item is already filed or paid.")
        If Not line.AmountPayable.HasValue Then Return OperationResult(Of String).Fail("There is no amount for this item.")
        Dim stamp = periodStart.ToString(MonthFormat, CultureInfo.InvariantCulture)
        Dim written As OperationResult(Of String)
        Dim note As String = String.Empty
        Select Case line.Kind
            Case ComplianceKind.Epf
                Dim built = _files.Ecr(figures.RunId.Value, figures.PeriodTo.Value)
                If Not built.Succeeded Then Return OperationResult(Of String).Fail(built.Message)
                written = _writer.SaveText(folder, built.Value.FileName, built.Value.Content)
                If built.Value.Problems.Count > 0 Then note = $" ({built.Value.Problems.Count.ToString(CultureInfo.InvariantCulture)} employee(s) skipped; check the ECR in Payroll export)"
            Case ComplianceKind.Esic
                Dim built = _files.Esic(figures.RunId.Value)
                If Not built.Succeeded Then Return OperationResult(Of String).Fail(built.Message)
                written = _writer.SaveSheets(folder, built.Value.FileName, built.Value.Content)
                If built.Value.Problems.Count > 0 Then note = $" ({built.Value.Problems.Count.ToString(CultureInfo.InvariantCulture)} employee(s) skipped; check the ESIC sheet in Payroll export)"
            Case Else
                Dim single As New MonthlyPack(pack.CompanyName, periodStart, New List(Of MonthlyPackLine) From {line}, pack.PayrollMessage)
                Dim part = If(line.StateName.Length > 0, "_" & line.StateName.Replace(" ", String.Empty), String.Empty)
                written = _writer.SaveSheets(folder, $"Pack_{line.Kind}{part}_{stamp}.xls", MonthlyPackSheetBuilder.Build(single))
        End Select
        If Not written.Succeeded Then Return OperationResult(Of String).Fail(written.Message)
        Try
            _repository.RecordFiling(periodStart, line.Kind, line.StateId, PackStatus.Generated, line.AmountPayable, line.EmployeeCount, Nothing, Nothing, Nothing, CurrentUserId())
        Catch ex As PackRuleException
            Return OperationResult(Of String).Fail(ex.Message)
        End Try
        Dim logged = _audit.Record(New AuditEntry(ModuleKey.Statutory, "GeneratePackLine", "StatutoryFiling", Key(periodStart, line), "Generated"))
        If Not logged.Succeeded Then Return OperationResult(Of String).Fail(logged.Message)
        Return OperationResult(Of String).Ok(written.Value & note)
    End Function

    Private Shared Function Same(a As MonthlyPackLine, b As MonthlyPackLine) As Boolean
        Return a.Kind = b.Kind AndAlso Nullable.Equals(a.StateId, b.StateId)
    End Function

    Private Shared Function Key(periodStart As Date, line As MonthlyPackLine) As String
        Dim state = If(line.StateId.HasValue, ":" & line.StateId.Value.ToString(CultureInfo.InvariantCulture), String.Empty)
        Return $"{periodStart.ToString(MonthFormat, CultureInfo.InvariantCulture)}:{line.Kind}{state}"
    End Function

    Private Function Check(action As PermissionAction, periodStart As Date) As OperationResult
        If Not _permissions.IsAllowed(ModuleKey.Statutory, action) Then Return OperationResult.Fail(NotAllowed)
        If periodStart.Day <> 1 Then Return OperationResult.Fail("The month must start on the first day.")
        Return OperationResult.Ok()
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class
