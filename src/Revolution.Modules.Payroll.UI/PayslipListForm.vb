Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Payroll

''' <summary>
''' Payslip list screen, plain version for testing (Brave; Sigma restyles it and adds the printed layout). Type a run id, and an employee id to see one payslip or leave it at zero for all,
''' then Load. A run below Approved is marked DRAFT, and the income tax column shows only when the plan has the TDS feature, as on the printed payslip.
''' </summary>
Friend NotInheritable Class PayslipListForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1000
    Private Const FormHeight As Integer = 640

    Private Shared ReadOnly Indian As CultureInfo = CultureInfo.GetCultureInfo("en-IN")

    Private ReadOnly _services As PayrollServices
    Private ReadOnly _runId As New NumericUpDown()
    Private ReadOnly _employeeId As New NumericUpDown()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _load As New Button()
    Private ReadOnly _exit As New Button()

    Public Sub New(services As PayrollServices)
        MyBase.New("Payslips", "Load the payslips of a run to check them before they are printed.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid()
        Content.Controls.Add(_grid)

        _runId.Minimum = 1D
        _runId.Maximum = Integer.MaxValue
        _employeeId.Minimum = 0D
        _employeeId.Maximum = Integer.MaxValue

        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Run id", _runId)
        AddField(layout, "Employee id (0 for everyone)", _employeeId)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        _load.Text = "Load"
        _load.AutoSize = True
        bar.Controls.Add(_exit)
        bar.Controls.Add(_load)
        AddHandler _load.Click, Sub(sender As Object, e As EventArgs) DoLoad()
    End Sub

    Private Sub ConfigureGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoGenerateColumns = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn(NameOf(SlipRow.EmpCode), "Code", 8, False)
        AddColumn(NameOf(SlipRow.FullName), "Name", 18, False)
        AddColumn(NameOf(SlipRow.PaidDays), "Paid days", 7, True)
        AddColumn(NameOf(SlipRow.Gross), "Gross", 10, True)
        AddColumn(NameOf(SlipRow.Tds), "Income tax", 9, True)
        AddColumn(NameOf(SlipRow.Deductions), "Total deductions", 10, True)
        AddColumn(NameOf(SlipRow.NetPay), "Net pay", 10, True)
        AddColumn(NameOf(SlipRow.Words), "In words", 28, False)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single, right As Boolean)
        Dim column As New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight, .Name = property_}
        If right Then column.DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}
        _grid.Columns.Add(column)
    End Sub

    Private Sub DoLoad()
        ClearValidation()
        Dim employee As Integer? = If(_employeeId.Value > 0D, CType(CInt(_employeeId.Value), Integer?), Nothing)
        Dim result As OperationResult(Of IReadOnlyList(Of Payslip)) = _services.Payslips.GetPayslips(CInt(_runId.Value), employee, False)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _grid.DataSource = result.Value.Select(Function(p) New SlipRow(p)).ToList()
        Dim showTds As Boolean = result.Value.Count = 0 OrElse result.Value(0).ShowTds
        _grid.Columns(NameOf(SlipRow.Tds)).Visible = showTds
        Dim draft As Boolean = result.Value.Count > 0 AndAlso result.Value(0).IsDraft
        ShowStatus($"{result.Value.Count.ToString(CultureInfo.InvariantCulture)} payslips.{If(draft, " The run is not approved yet: these are DRAFT payslips.", String.Empty)}")
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        DoLoad()
    End Sub

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("C0", Indian)
    End Function

    Private NotInheritable Class SlipRow
        Public Sub New(slip As Payslip)
            EmpCode = slip.Row.EmpCode
            FullName = slip.Row.FullName
            PaidDays = slip.Row.PaidDays.ToString("0.##", Indian)
            Gross = Money(slip.Row.GrossEarnings)
            Tds = Money(slip.Row.TdsAmount)
            Deductions = Money(slip.Row.TotalDeductions)
            NetPay = Money(slip.Row.NetPay)
            Words = slip.NetPayInWords
        End Sub

        Public ReadOnly Property EmpCode As String
        Public ReadOnly Property FullName As String
        Public ReadOnly Property PaidDays As String
        Public ReadOnly Property Gross As String
        Public ReadOnly Property Tds As String
        Public ReadOnly Property Deductions As String
        Public ReadOnly Property NetPay As String
        Public ReadOnly Property Words As String
    End Class
End Class
