Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Payroll

''' <summary>
''' Read-only details of one payroll run, opened from Lock Payroll so the run can be checked before it is locked: the header (period, status, totals) and a line for every employee.
''' Nothing can be changed here and nothing is stored. The service checks View access again.
''' </summary>
Friend NotInheritable Class PayrollRunDetailsForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1000
    Private Const FormHeight As Integer = 560

    Private Shared ReadOnly Indian As CultureInfo = CultureInfo.GetCultureInfo("en-IN")

    Private ReadOnly _services As PayrollServices
    Private ReadOnly _runId As Integer
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _exit As New Button()

    Public Sub New(services As PayrollServices, runId As Integer)
        MyBase.New("Run details", "Read only. Check the lines before the run is locked.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _runId = runId
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
        LoadDetails()
    End Sub

    Private Sub BuildLayout()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoGenerateColumns = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn(NameOf(LineRow.EmployeeId), "Employee id", 9, False)
        AddColumn(NameOf(LineRow.PaidDays), "Paid days", 8, True)
        AddColumn(NameOf(LineRow.Gross), "Gross", 11, True)
        AddColumn(NameOf(LineRow.Epf), "PF", 9, True)
        AddColumn(NameOf(LineRow.Esi), "ESI", 8, True)
        AddColumn(NameOf(LineRow.ProfessionalTax), "PT", 7, True)
        AddColumn(NameOf(LineRow.Tds), "Income tax", 9, True)
        AddColumn(NameOf(LineRow.Emi), "EMI", 8, True)
        AddColumn(NameOf(LineRow.OtherDeductions), "Other deductions", 10, True)
        AddColumn(NameOf(LineRow.Deductions), "Total deductions", 11, True)
        AddColumn(NameOf(LineRow.NetPay), "Net pay", 11, True)
        Content.Controls.Add(_grid)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Close"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        bar.Controls.Add(_exit)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single, right As Boolean)
        Dim column As New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight}
        If right Then column.DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}
        _grid.Columns.Add(column)
    End Sub

    Private Sub LoadDetails()
        ClearValidation()
        Dim header As OperationResult(Of PayrollRunHeader) = _services.Runs.GetRun(_runId)
        If Not header.Succeeded Then
            ShowError(header.Message)
            Return
        End If
        Dim lines As OperationResult(Of IReadOnlyList(Of RunLineResult)) = _services.Runs.ListLines(_runId)
        If Not lines.Succeeded Then
            ShowError(lines.Message)
            Return
        End If
        _grid.DataSource = lines.Value.Select(Function(l) New LineRow(l)).ToList()
        Dim run As PayrollRunHeader = header.Value
        ShowStatus($"Run {run.RunId.ToString(CultureInfo.InvariantCulture)} (no. {run.RunNo.ToString(CultureInfo.InvariantCulture)}), {run.PeriodFrom:dd-MM-yyyy} to {run.PeriodTo:dd-MM-yyyy}, {run.Status}. " &
                   $"{run.EmployeeCount.ToString(CultureInfo.InvariantCulture)} employees, gross {Money(run.TotalGross)}, deductions {Money(run.TotalDeductions)}, net {Money(run.TotalNet)}.")
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        LoadDetails()
    End Sub

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("C0", Indian)
    End Function

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
            OtherDeductions = Money(line.OtherDeductions)
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
        Public ReadOnly Property OtherDeductions As String
        Public ReadOnly Property Deductions As String
        Public ReadOnly Property NetPay As String
    End Class
End Class
