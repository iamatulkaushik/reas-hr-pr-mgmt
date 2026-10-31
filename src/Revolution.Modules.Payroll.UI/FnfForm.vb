Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Payroll

''' <summary>
''' Full and final screen, plain version for testing (Brave; Sigma restyles it). Type the separation id, choose the Owner's options, Prepare to see the lines, totals, due window and notes,
''' then Save, Approve, Mark paid or Reopen. Prepare saves nothing. Every action is checked again by FnfService; the buttons are only switched on once there is something to act on.
''' </summary>
Friend NotInheritable Class FnfForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 860
    Private Const FormHeight As Integer = 700
    Private Const MaxMoney As Decimal = 99999999D
    Private Const NotesHeight As Integer = 90

    Private Shared ReadOnly Indian As CultureInfo = CultureInfo.GetCultureInfo("en-IN")

    Private ReadOnly _services As PayrollServices
    Private ReadOnly _separation As New NumericUpDown()
    Private ReadOnly _ruleBonus As New CheckBox()
    Private ReadOnly _bonusPct As New NumericUpDown()
    Private ReadOnly _arrears As New NumericUpDown()
    Private ReadOnly _overrideSalary As New CheckBox()
    Private ReadOnly _lastSalary As New NumericUpDown()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _notes As New ListBox()
    Private ReadOnly _prepare As New Button()
    Private ReadOnly _loadSaved As New Button()
    Private ReadOnly _save As New Button()
    Private ReadOnly _approve As New Button()
    Private ReadOnly _pay As New Button()
    Private ReadOnly _reopen As New Button()
    Private ReadOnly _exit As New Button()
    Private _plan As FnfPlan

    Public Sub New(services As PayrollServices)
        MyBase.New("Full and final", "Prepare a settlement from the facts, check the lines, then save, approve and pay it.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
        SetActions(saved:=False)
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid()
        _notes.Dock = DockStyle.Bottom
        _notes.Height = NotesHeight
        Content.Controls.Add(_grid)
        Content.Controls.Add(_notes)

        _separation.Minimum = 1D
        _separation.Maximum = Integer.MaxValue
        _ruleBonus.Text = "Bonus at the rule's minimum"
        _ruleBonus.Checked = True
        _ruleBonus.AutoSize = True
        _bonusPct.DecimalPlaces = 2
        _bonusPct.Maximum = 100D
        _bonusPct.Enabled = False
        _arrears.Maximum = MaxMoney
        _overrideSalary.Text = "Type the last salary by hand"
        _overrideSalary.AutoSize = True
        _lastSalary.Maximum = MaxMoney
        _lastSalary.Enabled = False
        AddHandler _ruleBonus.CheckedChanged, Sub(sender As Object, e As EventArgs) _bonusPct.Enabled = Not _ruleBonus.Checked
        AddHandler _overrideSalary.CheckedChanged, Sub(sender As Object, e As EventArgs) _lastSalary.Enabled = _overrideSalary.Checked

        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Separation id", _separation)
        AddField(layout, "Bonus", _ruleBonus)
        AddField(layout, "Bonus percentage (when not the minimum)", _bonusPct)
        AddField(layout, "Arrears to recover (rupees, Owner's option)", _arrears)
        AddField(layout, "Last salary", _overrideSalary)
        AddField(layout, "Last salary amount (rupees)", _lastSalary)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        For Each button As Button In {_reopen, _pay, _approve, _save, _loadSaved, _prepare}
            button.AutoSize = True
        Next
        _prepare.Text = "Prepare"
        _loadSaved.Text = "Load saved"
        _save.Text = "Save lines"
        _approve.Text = "Approve"
        _pay.Text = "Mark paid"
        _reopen.Text = "Reopen"
        bar.Controls.Add(_exit)
        For Each button As Button In {_reopen, _pay, _approve, _save, _loadSaved, _prepare}
            bar.Controls.Add(button)
        Next
        AddHandler _prepare.Click, Sub(sender As Object, e As EventArgs) DoPrepare()
        AddHandler _loadSaved.Click, Sub(sender As Object, e As EventArgs) DoLoadSaved()
        AddHandler _save.Click, Sub(sender As Object, e As EventArgs) DoSave()
        AddHandler _approve.Click, Sub(sender As Object, e As EventArgs) DoMove(Function(id) _services.Fnf.Approve(id), "Approved.")
        AddHandler _pay.Click, Sub(sender As Object, e As EventArgs) DoMove(Function(id) _services.Fnf.MarkPaid(id), "Marked paid.")
        AddHandler _reopen.Click, Sub(sender As Object, e As EventArgs) DoMove(Function(id) _services.Fnf.Reopen(id), "Reopened. Prepare or change the lines, then save again.")
    End Sub

    Private Sub ConfigureGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoGenerateColumns = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(LineRow.Description), .HeaderText = "Line", .FillWeight = 60})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(LineRow.Side), .HeaderText = "Side", .FillWeight = 20})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = NameOf(LineRow.AmountText), .HeaderText = "Amount", .FillWeight = 20,
                                                               .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}})
    End Sub

    Private Function CurrentId() As Integer
        Return CInt(_separation.Value)
    End Function

    Private Function Options() As FnfPrepareOptions
        Return New FnfPrepareOptions With {
            .BonusPct = If(_ruleBonus.Checked, CType(Nothing, Decimal?), _bonusPct.Value),
            .ArrearsRecovery = _arrears.Value,
            .LastSalaryOverride = If(_overrideSalary.Checked, _lastSalary.Value, CType(Nothing, Decimal?))}
    End Function

    Private Sub DoPrepare()
        ClearValidation()
        Dim result As OperationResult(Of FnfPlan) = _services.Fnf.Prepare(CurrentId(), Options())
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _plan = result.Value
        ShowPlan(_plan.Lines, _plan.Totals, _plan.Notes)
        ShowStatus($"{Summary(_plan.Totals)} Due {_plan.DueWindow.PeriodFrom:dd-MM-yyyy} to {_plan.DueWindow.PeriodTo:dd-MM-yyyy}. Not saved yet.")
        SetActions(saved:=False)
    End Sub

    Private Sub DoLoadSaved()
        ClearValidation()
        Dim result As OperationResult(Of FnfSettlementView) = _services.Fnf.GetSettlement(CurrentId())
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        If result.Value Is Nothing Then
            ShowStatus("Nothing is saved for this separation yet. Use Prepare.")
            ShowPlan(New List(Of FnfLine)(), New FnfTotals(0D, 0D), New List(Of String)())
            _plan = Nothing
            SetActions(saved:=False)
            Return
        End If
        _plan = New FnfPlan(result.Value.Lines, result.Value.Totals, FnfDueWindow.For(result.Value.ExitDate), New List(Of String)())
        ShowPlan(_plan.Lines, _plan.Totals, _plan.Notes)
        ShowStatus($"Saved settlement, {result.Value.Status}. {Summary(result.Value.Totals)}")
        SetActions(saved:=True, status:=result.Value.Status)
    End Sub

    Private Sub DoSave()
        ClearValidation()
        If _plan Is Nothing Then
            ShowError("Prepare the settlement first.")
            Return
        End If
        Dim result As OperationResult(Of Integer) = _services.Fnf.SaveLines(CurrentId(), _plan.Lines)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        DoLoadSaved()
    End Sub

    Private Sub DoMove(move As Func(Of Integer, OperationResult), done As String)
        ClearValidation()
        Dim result As OperationResult = move(CurrentId())
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        DoLoadSaved()
        ShowStatus(done)
    End Sub

    Private Sub ShowPlan(lines As IReadOnlyList(Of FnfLine), totals As FnfTotals, notes As IReadOnlyList(Of String))
        _grid.DataSource = lines.Select(Function(l) New LineRow(l)).ToList()
        _notes.Items.Clear()
        For Each note As String In notes
            _notes.Items.Add(note)
        Next
    End Sub

    Private Shared Function Summary(totals As FnfTotals) As String
        Dim word As String = If(totals.IsRecoverable, "to recover from the employee", "payable")
        Return $"Earnings {Money(totals.Earnings)}, deductions {Money(totals.Deductions)}, net {Money(Math.Abs(totals.Net))} {word}."
    End Function

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("C0", Indian)
    End Function

    ''' <summary>Buttons follow the saved status: lines can be saved until approval, Approve needs Calculated, Mark paid and Reopen need Approved.</summary>
    Private Sub SetActions(saved As Boolean, Optional status As FnfStatus = FnfStatus.Open)
        _save.Enabled = _plan IsNot Nothing AndAlso (Not saved OrElse FnfTransition.CanEdit(status))
        _approve.Enabled = saved AndAlso FnfTransition.CanMove(status, FnfStatus.Approved)
        _pay.Enabled = saved AndAlso FnfTransition.CanMove(status, FnfStatus.Paid)
        _reopen.Enabled = saved AndAlso FnfTransition.CanMove(status, FnfStatus.Calculated)
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        DoLoadSaved()
    End Sub

    Private NotInheritable Class LineRow
        Public Sub New(line As FnfLine)
            Description = line.Description
            Side = If(line.IsDeduction, "Deduction", "Earning")
            AmountText = Money(line.Amount)
        End Sub

        Public ReadOnly Property Description As String
        Public ReadOnly Property Side As String
        Public ReadOnly Property AmountText As String
    End Class
End Class
