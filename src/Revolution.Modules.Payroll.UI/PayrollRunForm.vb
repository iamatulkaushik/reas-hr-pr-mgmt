Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Payroll

''' <summary>
''' Payroll run screen, plain version for testing (Brave; Sigma restyles it). Create a regular run for a month, Process it, then Submit, Approve, Lock, apply loan recoveries and
''' Mark paid; Return to draft and Reopen need a reason. The grid shows every employee's line. Every action is checked again by PayrollRunService.
''' </summary>
Friend NotInheritable Class PayrollRunForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 980
    Private Const FormHeight As Integer = 720
    Private Const MinYear As Integer = 2000
    Private Const MaxYear As Integer = 9998

    Private Shared ReadOnly Indian As CultureInfo = CultureInfo.GetCultureInfo("en-IN")

    Private ReadOnly _services As PayrollServices
    Private ReadOnly _year As New NumericUpDown()
    Private ReadOnly _month As New NumericUpDown()
    Private ReadOnly _runId As New NumericUpDown()
    Private ReadOnly _reason As New TextBox()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _create As New Button()
    Private ReadOnly _load As New Button()
    Private ReadOnly _process As New Button()
    Private ReadOnly _submit As New Button()
    Private ReadOnly _approve As New Button()
    Private ReadOnly _lock As New Button()
    Private ReadOnly _loans As New Button()
    Private ReadOnly _pay As New Button()
    Private ReadOnly _back As New Button()
    Private ReadOnly _reopen As New Button()
    Private ReadOnly _exit As New Button()

    Public Sub New(services As PayrollServices)
        MyBase.New("Payroll run", "Create a run for a month, process it, then send it through approval, lock it and mark it paid.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
        SetActions(Nothing)
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid()
        Content.Controls.Add(_grid)

        _year.Minimum = MinYear
        _year.Maximum = MaxYear
        _year.Value = Date.Today.Year
        _month.Minimum = 1D
        _month.Maximum = 12D
        _month.Value = Date.Today.Month
        _runId.Minimum = 0D
        _runId.Maximum = Integer.MaxValue
        _reason.MaxLength = 200

        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Year", _year)
        AddField(layout, "Month", _month)
        AddField(layout, "Run id", _runId)
        AddField(layout, "Reason (for Return to draft and Reopen)", _reason)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        bar.Controls.Add(_exit)
        Dim actions As Button() = {_reopen, _pay, _loans, _lock, _approve, _back, _submit, _process, _load, _create}
        Dim names As String() = {"Reopen", "Mark paid", "Apply loan recoveries", "Lock", "Approve", "Return to draft", "Submit", "Process", "Load", "Create run"}
        For index As Integer = 0 To actions.Length - 1
            actions(index).Text = names(index)
            actions(index).AutoSize = True
            bar.Controls.Add(actions(index))
        Next
        AddHandler _create.Click, Sub(sender As Object, e As EventArgs) DoCreate()
        AddHandler _load.Click, Sub(sender As Object, e As EventArgs) Reload("Loaded.")
        AddHandler _process.Click, Sub(sender As Object, e As EventArgs) DoProcess()
        AddHandler _submit.Click, Sub(sender As Object, e As EventArgs) DoMove(Function(id) _services.Runs.Submit(id), "Submitted for approval.")
        AddHandler _approve.Click, Sub(sender As Object, e As EventArgs) DoMove(Function(id) _services.Runs.Approve(id), "Approved.")
        AddHandler _lock.Click, Sub(sender As Object, e As EventArgs) DoMove(Function(id) _services.Runs.Lock(id), "Locked.")
        AddHandler _loans.Click, Sub(sender As Object, e As EventArgs) DoMove(Function(id) _services.Runs.ApplyLoanRecoveries(id), "Loan recoveries applied.")
        AddHandler _pay.Click, Sub(sender As Object, e As EventArgs) DoMove(Function(id) _services.Runs.MarkPaid(id), "Marked paid.")
        AddHandler _back.Click, Sub(sender As Object, e As EventArgs) DoMove(Function(id) _services.Runs.ReturnToDraft(id, _reason.Text), "Returned to draft.")
        AddHandler _reopen.Click, Sub(sender As Object, e As EventArgs) DoMove(Function(id) _services.Runs.Reopen(id, _reason.Text), "Reopened.")
    End Sub

    Private Sub ConfigureGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoGenerateColumns = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn(NameOf(LineRow.EmployeeId), "Employee id", 10, False)
        AddColumn(NameOf(LineRow.PaidDays), "Paid days", 8, True)
        AddColumn(NameOf(LineRow.Gross), "Gross", 12, True)
        AddColumn(NameOf(LineRow.Epf), "EPF", 10, True)
        AddColumn(NameOf(LineRow.Esi), "ESI", 10, True)
        AddColumn(NameOf(LineRow.ProfessionalTax), "PT", 8, True)
        AddColumn(NameOf(LineRow.Tds), "TDS", 10, True)
        AddColumn(NameOf(LineRow.Emi), "EMI", 10, True)
        AddColumn(NameOf(LineRow.Deductions), "Total deductions", 12, True)
        AddColumn(NameOf(LineRow.NetPay), "Net pay", 12, True)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single, right As Boolean)
        Dim column As New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight}
        If right Then column.DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}
        _grid.Columns.Add(column)
    End Sub

    Private Function CurrentId() As Integer
        Return CInt(_runId.Value)
    End Function

    Private Sub DoCreate()
        ClearValidation()
        Dim result As OperationResult(Of Integer) = _services.Runs.CreateRegularRun(CInt(_year.Value), CInt(_month.Value))
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _runId.Value = result.Value
        Reload($"Run {result.Value.ToString(CultureInfo.InvariantCulture)} created.")
    End Sub

    Private Sub DoProcess()
        ClearValidation()
        Dim result As OperationResult(Of Integer) = _services.Runs.Process(CurrentId())
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Dim warnings As IReadOnlyList(Of String) = _services.Runs.TdsWarnings
        Dim note As String = If(warnings.Count = 0, String.Empty, $" Tax could not be taken in full for: {String.Join(", ", warnings)}.")
        Reload($"Processed {result.Value.ToString(CultureInfo.InvariantCulture)} employees.{note}")
    End Sub

    Private Sub DoMove(move As Func(Of Integer, OperationResult), done As String)
        ClearValidation()
        Dim result As OperationResult = move(CurrentId())
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Reload(done)
    End Sub

    ''' <summary>Reads the run header and lines, fills the grid, and shows the message with the status line. Buttons follow the status.</summary>
    Private Sub Reload(message As String)
        Dim header As OperationResult(Of PayrollRunHeader) = _services.Runs.GetRun(CurrentId())
        If Not header.Succeeded Then
            ShowError(header.Message)
            SetActions(Nothing)
            Return
        End If
        Dim lines As OperationResult(Of IReadOnlyList(Of RunLineResult)) = _services.Runs.ListLines(CurrentId())
        _grid.DataSource = If(lines.Succeeded, lines.Value.Select(Function(l) New LineRow(l)).ToList(), New List(Of LineRow)())
        Dim run As PayrollRunHeader = header.Value
        ShowStatus($"{message} Run {run.RunId.ToString(CultureInfo.InvariantCulture)} (no. {run.RunNo.ToString(CultureInfo.InvariantCulture)}), {run.PeriodFrom:dd-MM-yyyy} to {run.PeriodTo:dd-MM-yyyy}, {run.Status}. " &
                   $"{run.EmployeeCount.ToString(CultureInfo.InvariantCulture)} employees, gross {Money(run.TotalGross)}, deductions {Money(run.TotalDeductions)}, net {Money(run.TotalNet)}.")
        SetActions(run.Status)
    End Sub

    ''' <summary>Process on Draft or Processed, Submit on Processed, Approve on Submitted (or Processed when the plan needs no second approver, which the service decides), Return to draft on Processed or Submitted, Lock on Approved, Loan recoveries, Mark paid and Reopen on Locked.</summary>
    Private Sub SetActions(status As PayrollRunStatus?)
        Dim s As PayrollRunStatus = If(status, PayrollRunStatus.Draft)
        Dim known As Boolean = status.HasValue
        _process.Enabled = known AndAlso (s = PayrollRunStatus.Draft OrElse s = PayrollRunStatus.Processed)
        _submit.Enabled = known AndAlso s = PayrollRunStatus.Processed
        _approve.Enabled = known AndAlso (s = PayrollRunStatus.Submitted OrElse s = PayrollRunStatus.Processed)
        _back.Enabled = known AndAlso (s = PayrollRunStatus.Processed OrElse s = PayrollRunStatus.Submitted)
        _lock.Enabled = known AndAlso s = PayrollRunStatus.Approved
        _loans.Enabled = known AndAlso s = PayrollRunStatus.Locked
        _pay.Enabled = known AndAlso s = PayrollRunStatus.Locked
        _reopen.Enabled = known AndAlso s = PayrollRunStatus.Locked
        _load.Enabled = True
    End Sub

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("C0", Indian)
    End Function

    Protected Overrides Sub OnRefreshRequested()
        If CurrentId() > 0 Then Reload("Refreshed.")
    End Sub

    Private NotInheritable Class LineRow
        Public Sub New(line As RunLineResult)
            EmployeeId = line.EmployeeId
            PaidDays = line.PaidDays.ToString("0.##", Indian)
            Gross = Money(line.GrossEarnings)
            Epf = Money(line.EpfEmployee)
            Esi = Money(line.EsiEmployee)
            ProfessionalTax = Money(line.ProfessionalTax)
            Tds = Money(line.TdsAmount)
            Emi = Money(line.LoanEmi)
            Deductions = Money(line.TotalDeductions)
            NetPay = Money(line.NetPay)
        End Sub

        Public ReadOnly Property EmployeeId As Integer
        Public ReadOnly Property PaidDays As String
        Public ReadOnly Property Gross As String
        Public ReadOnly Property Epf As String
        Public ReadOnly Property Esi As String
        Public ReadOnly Property ProfessionalTax As String
        Public ReadOnly Property Tds As String
        Public ReadOnly Property Emi As String
        Public ReadOnly Property Deductions As String
        Public ReadOnly Property NetPay As String
    End Class
End Class
