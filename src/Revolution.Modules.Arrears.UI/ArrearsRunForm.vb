Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Arrears
Imports Revolution.Modules.Payroll

''' <summary>
''' Pay arrears in a run, plain version for testing (Brave; Sigma restyles it). Pick the pay month and Preview: one line per employee from the open claims, head by head,
''' with EPF and ESI taken and no tax (tax is deducted in the regular run only, Owner 2026-11-03). Claims that cannot be paid stay open and are named. Make run creates the
''' off-cycle run (RunNo 2 and up) and marks the claims Paid; the run is then approved, locked and paid like any run. The attendance month of the pay month must be closed.
''' </summary>
Friend NotInheritable Class ArrearsRunForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 900
    Private Const FormHeight As Integer = 620
    Private Const MonthFormat As String = "MMM yyyy"

    Private Shared ReadOnly Indian As CultureInfo = CultureInfo.GetCultureInfo("en-IN")

    Private ReadOnly _services As ArrearsServices
    Private ReadOnly _payMonth As New DateTimePicker()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _preview As New Button()
    Private ReadOnly _make As New Button()
    Private ReadOnly _exit As New Button()

    Public Sub New(services As ArrearsServices)
        MyBase.New("Pay arrears in a run", "Pay the open arrears claims in an off-cycle run for a pay month.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid()
        Content.Controls.Add(_grid)

        _payMonth.Format = DateTimePickerFormat.Custom
        _payMonth.CustomFormat = MonthFormat
        _payMonth.ShowUpDown = True
        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Pay month", _payMonth)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        _make.Text = "Make run"
        _make.AutoSize = True
        _make.Enabled = False
        _preview.Text = "Preview"
        _preview.AutoSize = True
        bar.Controls.Add(_exit)
        bar.Controls.Add(_make)
        bar.Controls.Add(_preview)
        AddHandler _payMonth.ValueChanged, Sub(sender As Object, e As EventArgs) ForgetPreview()
        AddHandler _preview.Click, Sub(sender As Object, e As EventArgs) DoPreview()
        AddHandler _make.Click, Sub(sender As Object, e As EventArgs) DoMake()
    End Sub

    Private Sub ConfigureGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoGenerateColumns = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn(NameOf(PayLine.EmployeeId), "Employee id", 16)
        AddColumn(NameOf(PayLine.Gross), "Arrears", 21)
        AddColumn(NameOf(PayLine.Epf), "Employee EPF", 21)
        AddColumn(NameOf(PayLine.Esi), "Employee ESI", 21)
        AddColumn(NameOf(PayLine.Net), "Net pay", 21)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single)
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight, .Name = property_,
                                                              .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}})
    End Sub

    Private Sub ForgetPreview()
        _grid.DataSource = Nothing
        _make.Enabled = False
    End Sub

    Private Function RunService() As ArrearsRunService
        If _services.Run Is Nothing Then Throw New InvalidOperationException("Arrears runs are not available.")
        Return _services.Run
    End Function

    Private Sub DoPreview()
        ClearValidation()
        ForgetPreview()
        Dim result As OperationResult(Of ArrearsRunPlan) = RunService().Preview(_payMonth.Value)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Dim plan As ArrearsRunPlan = result.Value
        _grid.DataSource = plan.Lines.Select(Function(l) New PayLine(l)).ToList()
        _make.Enabled = plan.Lines.Count > 0
        ShowStatus($"{plan.Lines.Count.ToString(CultureInfo.InvariantCulture)} employees, arrears {Money(plan.TotalGross)}, net {Money(plan.TotalNet)}. " &
                   If(plan.Problems.Count = 0, String.Empty, "Left open: " & String.Join(" ", plan.Problems)))
    End Sub

    Private Sub DoMake()
        ClearValidation()
        Dim answer As DialogResult = MessageBox.Show(Me, "Make the arrears run? The claims become Paid and cannot be cancelled.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If answer <> DialogResult.Yes Then Return
        Dim result As OperationResult(Of ArrearsRunResult) = RunService().Create(_payMonth.Value)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        ForgetPreview()
        ShowStatus($"Run {result.Value.RunNo.ToString(CultureInfo.InvariantCulture)} is made for {result.Value.EmployeeCount.ToString(CultureInfo.InvariantCulture)} employees. " &
                   "Approve it in the payroll run screen. " & If(result.Value.Problems.Count = 0, String.Empty, "Left open: " & String.Join(" ", result.Value.Problems)))
    End Sub

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("C0", Indian)
    End Function

    Private NotInheritable Class PayLine
        Public Sub New(line As RunLineResult)
            EmployeeId = line.EmployeeId.ToString(CultureInfo.InvariantCulture)
            Gross = Money(line.GrossEarnings)
            Epf = Money(line.EpfEmployee)
            Esi = Money(line.EsiEmployee)
            Net = Money(line.NetPay)
        End Sub

        Public ReadOnly Property EmployeeId As String
        Public ReadOnly Property Gross As String
        Public ReadOnly Property Epf As String
        Public ReadOnly Property Esi As String
        Public ReadOnly Property Net As String
    End Class
End Class
