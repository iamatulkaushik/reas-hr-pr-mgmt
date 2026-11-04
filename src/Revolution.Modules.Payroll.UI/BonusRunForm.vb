Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Payroll

''' <summary>
''' Bonuses screen (step 7g). Pick the accounting year (April to March), the pay month (after the year ends) and the percentage inside the rule's range, preview what each employee is due with provident
''' fund and income tax taken, then make the bonus run. The run is an off-cycle run: approve, lock and pay it from Lock Payroll. A later run for the same year only pays the difference.
''' Every action is checked again by BonusRunService.
''' </summary>
Friend NotInheritable Class BonusRunForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 960
    Private Const FormHeight As Integer = 620
    Private Const MinYear As Integer = 2000
    Private Const MaxYear As Integer = 9998

    Private Shared ReadOnly Indian As CultureInfo = CultureInfo.GetCultureInfo("en-IN")

    Private ReadOnly _services As PayrollServices
    Private ReadOnly _year As New NumericUpDown()
    Private ReadOnly _pct As New NumericUpDown()
    Private ReadOnly _payYear As New NumericUpDown()
    Private ReadOnly _payMonth As New NumericUpDown()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _preview As New Button()
    Private ReadOnly _make As New Button()
    Private ReadOnly _exit As New Button()
    Private _previewed As Boolean

    Public Sub New(services As PayrollServices)
        MyBase.New("Bonuses", "Preview the bonus for an accounting year, then make the bonus run.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
        _make.Enabled = False
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid()
        Content.Controls.Add(_grid)

        Dim lastYear As Integer = If(Date.Today.Month >= 4, Date.Today.Year - 1, Date.Today.Year - 2)
        _year.Minimum = MinYear
        _year.Maximum = MaxYear
        _year.Value = lastYear
        _pct.DecimalPlaces = 2
        _pct.Minimum = 0.01D
        _pct.Maximum = 100D
        _pct.Value = 8.33D
        _payYear.Minimum = MinYear
        _payYear.Maximum = MaxYear
        _payYear.Value = Date.Today.Year
        _payMonth.Minimum = 1D
        _payMonth.Maximum = 12D
        _payMonth.Value = Date.Today.Month

        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Accounting year starting April", _year)
        AddField(layout, "Bonus % (inside the rule's range)", _pct)
        AddField(layout, "Pay year", _payYear)
        AddField(layout, "Pay month", _payMonth)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        bar.Controls.Add(_exit)
        _make.Text = "Make bonus run"
        _make.AutoSize = True
        bar.Controls.Add(_make)
        _preview.Text = "Preview"
        _preview.AutoSize = True
        bar.Controls.Add(_preview)
        AddHandler _preview.Click, Sub(sender As Object, e As EventArgs) DoPreview()
        AddHandler _make.Click, Sub(sender As Object, e As EventArgs) DoMake()
        AddHandler _year.ValueChanged, Sub(sender As Object, e As EventArgs) Invalidate_()
        AddHandler _pct.ValueChanged, Sub(sender As Object, e As EventArgs) Invalidate_()
        AddHandler _payYear.ValueChanged, Sub(sender As Object, e As EventArgs) Invalidate_()
        AddHandler _payMonth.ValueChanged, Sub(sender As Object, e As EventArgs) Invalidate_()
    End Sub

    Private Sub ConfigureGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoGenerateColumns = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn(NameOf(BonusLine.EmpCode), "Employee", 10, False)
        AddColumn(NameOf(BonusLine.Days), "Days worked", 8, True)
        AddColumn(NameOf(BonusLine.LastWage), "Last month wage", 11, True)
        AddColumn(NameOf(BonusLine.WageBase), "Wage base", 12, True)
        AddColumn(NameOf(BonusLine.Gross), "Bonus for the year", 12, True)
        AddColumn(NameOf(BonusLine.AlreadyPaid), "Already paid", 11, True)
        AddColumn(NameOf(BonusLine.Payable), "Payable now", 11, True)
        AddColumn(NameOf(BonusLine.Pf), "PF", 8, True)
        AddColumn(NameOf(BonusLine.Tax), "Income tax", 9, True)
        AddColumn(NameOf(BonusLine.NetPay), "Net pay", 11, True)
        AddColumn(NameOf(BonusLine.Remark), "Remark", 20, False)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single, right As Boolean)
        Dim column As New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight}
        If right Then column.DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}
        _grid.Columns.Add(column)
    End Sub

    ''' <summary>A changed year or percentage means the grid no longer matches: the run can only be made from a fresh preview.</summary>
    Private Sub Invalidate_()
        _previewed = False
        _make.Enabled = False
    End Sub

    Private Sub DoPreview()
        ClearValidation()
        Dim payMonth As New Date(CInt(_payYear.Value), CInt(_payMonth.Value), 1)
        Dim result As OperationResult(Of BonusRunPlan) = _services.Bonus.Preview(CInt(_year.Value), payMonth, _pct.Value)
        If Not result.Succeeded Then
            _grid.DataSource = New List(Of BonusLine)()
            Invalidate_()
            ShowError(result.Message)
            Return
        End If
        Dim plan As BonusRunPlan = result.Value
        _grid.DataSource = plan.Rows.Select(Function(r) New BonusLine(r)).ToList()
        _previewed = True
        If plan.Problems.Count > 0 Then
            _make.Enabled = False
            ShowError(String.Join(" ", plan.Problems.Take(5)) & If(plan.Problems.Count > 5, $" And {(plan.Problems.Count - 5).ToString(CultureInfo.InvariantCulture)} more.", String.Empty) & " The run cannot be made until this is fixed.")
            Return
        End If
        _make.Enabled = plan.Lines.Count > 0
        ShowStatus($"Rule range {plan.MinPct.ToString("0.##", Indian)}% to {plan.MaxPct.ToString("0.##", Indian)}%. {plan.Lines.Count.ToString(CultureInfo.InvariantCulture)} of " &
                   $"{plan.Rows.Count.ToString(CultureInfo.InvariantCulture)} employees have a bonus to pay: {Money(plan.TotalGross)} bonus, {Money(plan.TotalAmount)} net of PF and income tax, at {plan.Pct.ToString("0.##", Indian)}%.")
    End Sub

    Private Sub DoMake()
        If Not _previewed Then Return
        Dim payMonth As New Date(CInt(_payYear.Value), CInt(_payMonth.Value), 1)
        Dim answer As DialogResult = MessageBox.Show(Me, $"Make the bonus run for the year starting April {CInt(_year.Value).ToString(CultureInfo.InvariantCulture)}, paid in {payMonth:MMMM yyyy}, with PF and income tax taken?", Text,
                                                     MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
        If answer <> DialogResult.Yes Then Return
        ClearValidation()
        Dim result As OperationResult(Of BonusRunResult) = _services.Bonus.Create(CInt(_year.Value), payMonth, _pct.Value)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Invalidate_()
        ShowStatus($"Bonus run {result.Value.RunId.ToString(CultureInfo.InvariantCulture)} (no. {result.Value.RunNo.ToString(CultureInfo.InvariantCulture)}) made for " &
                   $"{result.Value.Employees.ToString(CultureInfo.InvariantCulture)} employees, {Money(result.Value.Total)}. Approve and lock it from Lock Payroll.")
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        DoPreview()
    End Sub

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("C0", Indian)
    End Function

    Private NotInheritable Class BonusLine
        Public Sub New(row As BonusRow)
            EmpCode = row.EmpCode
            Days = row.DaysWorked.ToString(CultureInfo.InvariantCulture)
            LastWage = Money(row.LastMonthlyWage)
            WageBase = Money(row.Result.WageBase)
            Gross = Money(row.Result.GrossBonus)
            AlreadyPaid = Money(row.Result.AlreadyPaid)
            Payable = Money(row.Amount)
            Pf = Money(row.EpfEmployee)
            Tax = Money(row.Tds)
            NetPay = Money(row.NetPay)
            Remark = If(row.Result.IsEligible, If(row.Amount = 0D, "Already paid in full.", String.Empty), row.Result.Reason)
        End Sub

        Public ReadOnly Property EmpCode As String
        Public ReadOnly Property Days As String
        Public ReadOnly Property LastWage As String
        Public ReadOnly Property WageBase As String
        Public ReadOnly Property Gross As String
        Public ReadOnly Property AlreadyPaid As String
        Public ReadOnly Property Payable As String
        Public ReadOnly Property Pf As String
        Public ReadOnly Property Tax As String
        Public ReadOnly Property NetPay As String
        Public ReadOnly Property Remark As String
    End Class
End Class
