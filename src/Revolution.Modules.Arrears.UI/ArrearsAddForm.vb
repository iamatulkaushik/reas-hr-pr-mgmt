Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Arrears
Imports Revolution.Modules.Payroll

''' <summary>
''' New arrears claim, plain version for testing (Brave; Sigma restyles it). Give the employee id, the first and last wage month and a reason, then Preview:
''' for each month the screen shows what the approved run paid, what was due with the salary in force now, and the EPF and ESI on the difference at that month's rates.
''' Save claim stores it. Nothing is stored before that. Tax is not worked here (section 89 relief comes with the run that pays the arrears).
''' </summary>
Friend NotInheritable Class ArrearsAddForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1000
    Private Const FormHeight As Integer = 640
    Private Const MonthFormat As String = "MMM yyyy"

    Private Shared ReadOnly Indian As CultureInfo = CultureInfo.GetCultureInfo("en-IN")

    Private ReadOnly _services As ArrearsServices
    Private ReadOnly _employeeId As New NumericUpDown()
    Private ReadOnly _fromMonth As New DateTimePicker()
    Private ReadOnly _toMonth As New DateTimePicker()
    Private ReadOnly _reason As New TextBox()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _preview As New Button()
    Private ReadOnly _save As New Button()
    Private ReadOnly _exit As New Button()
    Private _preparation As ArrearsPreparation
    Private _preparedFor As Integer

    Public Sub New(services As ArrearsServices)
        MyBase.New("New arrears claim", "Work out arrears for an employee over wage months, look at them, then save the claim.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid()
        Content.Controls.Add(_grid)

        _employeeId.Minimum = 1D
        _employeeId.Maximum = Integer.MaxValue
        ConfigureMonth(_fromMonth)
        ConfigureMonth(_toMonth)
        _reason.MaxLength = ArrearsService.MaxReasonLength

        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Employee id", _employeeId)
        AddField(layout, "First wage month", _fromMonth)
        AddField(layout, "Last wage month", _toMonth)
        AddField(layout, "Reason (for example: revision of basic from April)", _reason)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        _save.Text = "Save claim"
        _save.AutoSize = True
        _save.Enabled = False
        _preview.Text = "Preview"
        _preview.AutoSize = True
        bar.Controls.Add(_exit)
        bar.Controls.Add(_save)
        bar.Controls.Add(_preview)
        AddHandler _preview.Click, Sub(sender As Object, e As EventArgs) DoPreview()
        AddHandler _save.Click, Sub(sender As Object, e As EventArgs) DoSave()
        AddHandler _employeeId.ValueChanged, Sub(sender As Object, e As EventArgs) ForgetPreview()
        AddHandler _fromMonth.ValueChanged, Sub(sender As Object, e As EventArgs) ForgetPreview()
        AddHandler _toMonth.ValueChanged, Sub(sender As Object, e As EventArgs) ForgetPreview()
    End Sub

    Private Shared Sub ConfigureMonth(picker As DateTimePicker)
        picker.Format = DateTimePickerFormat.Custom
        picker.CustomFormat = MonthFormat
        picker.ShowUpDown = True
    End Sub

    Private Sub ConfigureGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoGenerateColumns = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn(NameOf(MonthLine.Month), "Wage month", 11, False)
        AddColumn(NameOf(MonthLine.Paid), "Paid", 13, True)
        AddColumn(NameOf(MonthLine.Due), "Due now", 13, True)
        AddColumn(NameOf(MonthLine.Difference), "Arrears", 13, True)
        AddColumn(NameOf(MonthLine.Epf), "Employee EPF", 13, True)
        AddColumn(NameOf(MonthLine.Esi), "Employee ESI", 13, True)
        AddColumn(NameOf(MonthLine.Net), "Net before tax", 13, True)
        AddColumn(NameOf(MonthLine.Employer), "Employer share", 13, True)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single, right As Boolean)
        Dim column As New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight, .Name = property_}
        If right Then column.DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}
        _grid.Columns.Add(column)
    End Sub

    Private Sub ForgetPreview()
        _preparation = Nothing
        _save.Enabled = False
    End Sub

    Private Sub DoPreview()
        ClearValidation()
        ForgetPreview()
        Dim employee As Integer = CInt(_employeeId.Value)
        Dim result As OperationResult(Of ArrearsPreparation) = _services.Prepare.Prepare(employee, _fromMonth.Value, _toMonth.Value)
        If Not result.Succeeded Then
            _grid.DataSource = Nothing
            ShowError(result.Message)
            Return
        End If
        _preparation = result.Value
        _preparedFor = employee
        Dim lines As New List(Of MonthLine)()
        For i As Integer = 0 To _preparation.Inputs.Count - 1
            lines.Add(New MonthLine(_preparation.Inputs(i), _preparation.Result.Months(i)))
        Next
        _grid.DataSource = lines
        Dim total As ArrearsResult = _preparation.Result
        If _preparation.HasDifference Then
            _save.Enabled = True
            ShowStatus($"Arrears {Money(total.GrossTotal)}, EPF and ESI {Money(total.EmployeeDeductionTotal)}, net before tax {Money(total.NetBeforeTax)}, employer share {Money(total.EmployerContributionTotal)}.")
        Else
            ShowStatus("There is no difference to claim in these months.")
        End If
    End Sub

    Private Sub DoSave()
        ClearValidation()
        If _preparation Is Nothing OrElse _preparedFor <> CInt(_employeeId.Value) Then
            ShowError("Preview the claim first.")
            Return
        End If
        Dim result As OperationResult(Of Integer) = _services.Arrears.Create(_preparedFor, _reason.Text, _preparation.Inputs)
        If Not result.Succeeded Then
            SetFieldError(_reason, result.Message)
            ShowError(result.Message)
            Return
        End If
        ShowStatus($"Claim {result.Value.ToString(CultureInfo.InvariantCulture)} is saved.")
        DialogResult = DialogResult.OK
    End Sub

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("C0", Indian)
    End Function

    Private NotInheritable Class MonthLine
        Public Sub New(input As ArrearsMonthInput, month As ArrearsMonthResult)
            Me.Month = input.WageMonth.ToString(MonthFormat, CultureInfo.InvariantCulture)
            Paid = Money(input.Paid.GrossEarnings)
            Due = Money(input.Due.GrossEarnings)
            Difference = Money(month.GrossDifference)
            Epf = Money(month.EmployeeEpf)
            Esi = Money(month.EsiEmployee)
            Net = Money(month.GrossDifference - month.EmployeeDeductions)
            Employer = Money(month.EmployerEpf + month.EmployerEps + month.Edli + month.Admin + month.EsiEmployer)
        End Sub

        Public ReadOnly Property Month As String
        Public ReadOnly Property Paid As String
        Public ReadOnly Property Due As String
        Public ReadOnly Property Difference As String
        Public ReadOnly Property Epf As String
        Public ReadOnly Property Esi As String
        Public ReadOnly Property Net As String
        Public ReadOnly Property Employer As String
    End Class
End Class
