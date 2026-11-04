Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Payroll

''' <summary>
''' Lock Payroll screen (step 7f), a separate screen to finalize a run (Owner 2026-11-04). Pick a pay month, see every run of the month (the regular run, arrears and bonus runs) with its status and totals,
''' open the read-only details of a run, then lock the selected run.
''' A run that is only Processed is approved and locked in one step when the plan needs no second approver; where it does, the run must already be approved. Reopen needs a written reason.
''' Every action is checked again by PayrollRunService. Nothing is stored in this screen.
''' </summary>
Friend NotInheritable Class PayrollLockForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 900
    Private Const FormHeight As Integer = 520
    Private Const MinYear As Integer = 2000
    Private Const MaxYear As Integer = 9998

    Private Shared ReadOnly Indian As CultureInfo = CultureInfo.GetCultureInfo("en-IN")

    Private ReadOnly _services As PayrollServices
    Private ReadOnly _year As New NumericUpDown()
    Private ReadOnly _month As New NumericUpDown()
    Private ReadOnly _reason As New TextBox()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _load As New Button()
    Private ReadOnly _lock As New Button()
    Private ReadOnly _reopen As New Button()
    Private ReadOnly _details As New Button()
    Private ReadOnly _exit As New Button()

    Public Sub New(services As PayrollServices)
        MyBase.New("Lock payroll", "Check the month's runs, then lock the payroll so it can be paid and reported.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
        UpdateActions()
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
        _reason.MaxLength = 200

        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Year", _year)
        AddField(layout, "Month", _month)
        AddField(layout, "Reason (for Reopen, at least 10 characters)", _reason)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        bar.Controls.Add(_exit)
        Dim actions As Button() = {_reopen, _lock, _details, _load}
        Dim names As String() = {"Reopen", "Lock payroll", "Details", "Load"}
        For index As Integer = 0 To actions.Length - 1
            actions(index).Text = names(index)
            actions(index).AutoSize = True
            bar.Controls.Add(actions(index))
        Next
        AddHandler _load.Click, Sub(sender As Object, e As EventArgs) LoadRuns("Loaded.")
        AddHandler _lock.Click, Sub(sender As Object, e As EventArgs) DoLock()
        AddHandler _reopen.Click, Sub(sender As Object, e As EventArgs) DoReopen()
        AddHandler _details.Click, Sub(sender As Object, e As EventArgs) ShowDetails()
        AddHandler _grid.SelectionChanged, Sub(sender As Object, e As EventArgs) UpdateActions()
    End Sub

    Private Sub ConfigureGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoGenerateColumns = False
        _grid.MultiSelect = False
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn(NameOf(RunRow.RunId), "Run id", 8, False)
        AddColumn(NameOf(RunRow.Kind), "Run", 12, False)
        AddColumn(NameOf(RunRow.Period), "Period", 18, False)
        AddColumn(NameOf(RunRow.Status), "Status", 12, False)
        AddColumn(NameOf(RunRow.Employees), "Employees", 10, True)
        AddColumn(NameOf(RunRow.Gross), "Gross", 14, True)
        AddColumn(NameOf(RunRow.Deductions), "Deductions", 14, True)
        AddColumn(NameOf(RunRow.NetPay), "Net pay", 14, True)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single, right As Boolean)
        Dim column As New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight}
        If right Then column.DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}
        _grid.Columns.Add(column)
    End Sub

    Private Function Selected() As RunRow
        If _grid.CurrentRow Is Nothing Then Return Nothing
        Return TryCast(_grid.CurrentRow.DataBoundItem, RunRow)
    End Function

    Private Sub LoadRuns(message As String)
        ClearValidation()
        Dim listed As OperationResult(Of IReadOnlyList(Of PayrollRunHeader)) = _services.Runs.ListRunsOfMonth(CInt(_year.Value), CInt(_month.Value))
        If Not listed.Succeeded Then
            _grid.DataSource = New List(Of RunRow)()
            ShowError(listed.Message)
            UpdateActions()
            Return
        End If
        _grid.DataSource = listed.Value.Select(Function(h) New RunRow(h)).ToList()
        If listed.Value.Count = 0 Then
            ShowStatus("No payroll run was made for this month.")
        Else
            ShowStatus($"{message} {listed.Value.Count.ToString(CultureInfo.InvariantCulture)} run(s) for the month.")
        End If
        UpdateActions()
    End Sub

    ''' <summary>Opens the read-only details of the selected run, so it can be checked before it is locked.</summary>
    Private Sub ShowDetails()
        Dim run As RunRow = Selected()
        If run Is Nothing Then Return
        Using form As New PayrollRunDetailsForm(_services, run.RunId)
            form.ShowDialog(Me)
        End Using
    End Sub

    Private Sub DoLock()
        Dim run As RunRow = Selected()
        If run Is Nothing Then Return
        Dim answer As DialogResult = MessageBox.Show(Me, $"Lock run {run.RunId.ToString(CultureInfo.InvariantCulture)} ({run.Kind})? Net pay {run.NetPay} for {run.Employees} employees.", Text,
                                                     MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
        If answer <> DialogResult.Yes Then Return
        ClearValidation()
        Dim result As OperationResult = _services.Runs.LockPayroll(run.RunId)
        If Not result.Succeeded Then
            LoadRuns("Reloaded.")
            ShowError(result.Message)
            Return
        End If
        LoadRuns($"Run {run.RunId.ToString(CultureInfo.InvariantCulture)} is locked.")
    End Sub

    Private Sub DoReopen()
        Dim run As RunRow = Selected()
        If run Is Nothing Then Return
        ClearValidation()
        Dim result As OperationResult = _services.Runs.Reopen(run.RunId, _reason.Text)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _reason.Clear()
        LoadRuns($"Run {run.RunId.ToString(CultureInfo.InvariantCulture)} is open again.")
    End Sub

    ''' <summary>Lock is offered for a Processed, Submitted or Approved run (the service says what is missing); Reopen only for a Locked run.</summary>
    Private Sub UpdateActions()
        Dim run As RunRow = Selected()
        _lock.Enabled = run IsNot Nothing AndAlso (run.State = PayrollRunStatus.Processed OrElse run.State = PayrollRunStatus.Submitted OrElse run.State = PayrollRunStatus.Approved)
        _reopen.Enabled = run IsNot Nothing AndAlso run.State = PayrollRunStatus.Locked
        _details.Enabled = run IsNot Nothing
        _load.Enabled = True
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        LoadRuns("Refreshed.")
    End Sub

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("C0", Indian)
    End Function

    Private NotInheritable Class RunRow
        Public Sub New(header As PayrollRunHeader)
            RunId = header.RunId
            Kind = If(header.RunNo = 1, "Regular", "Arrears " & header.RunNo.ToString(CultureInfo.InvariantCulture))
            Period = $"{header.PeriodFrom:dd-MM-yyyy} to {header.PeriodTo:dd-MM-yyyy}"
            State = header.Status
            Status = header.Status.ToString()
            Employees = header.EmployeeCount.ToString(CultureInfo.InvariantCulture)
            Gross = Money(header.TotalGross)
            Deductions = Money(header.TotalDeductions)
            NetPay = Money(header.TotalNet)
        End Sub

        Public ReadOnly Property RunId As Integer
        Public ReadOnly Property Kind As String
        Public ReadOnly Property Period As String
        Public ReadOnly Property State As PayrollRunStatus
        Public ReadOnly Property Status As String
        Public ReadOnly Property Employees As String
        Public ReadOnly Property Gross As String
        Public ReadOnly Property Deductions As String
        Public ReadOnly Property NetPay As String
    End Class
End Class
