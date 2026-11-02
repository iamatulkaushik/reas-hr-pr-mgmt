Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Arrears

''' <summary>
''' Arrears list screen, plain version for testing (Brave; Sigma restyles it). Shows the claims of the company, newest first. Filter by employee id (0 for everyone) and,
''' if ticked, by wage months. Pick a claim and give a reason to cancel it. Cancelled claims show only when asked for.
''' </summary>
Friend NotInheritable Class ArrearsListForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1100
    Private Const FormHeight As Integer = 680
    Private Const MonthFormat As String = "MMM yyyy"

    Private Shared ReadOnly Indian As CultureInfo = CultureInfo.GetCultureInfo("en-IN")

    Private ReadOnly _services As ArrearsServices
    Private ReadOnly _employeeId As New NumericUpDown()
    Private ReadOnly _useMonths As New CheckBox()
    Private ReadOnly _fromMonth As New DateTimePicker()
    Private ReadOnly _toMonth As New DateTimePicker()
    Private ReadOnly _includeCancelled As New CheckBox()
    Private ReadOnly _cancelReason As New TextBox()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _load As New Button()
    Private ReadOnly _cancel As New Button()
    Private ReadOnly _exit As New Button()

    Public Sub New(services As ArrearsServices)
        MyBase.New("Arrears", "See the arrears claims, and cancel a wrong one.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid()
        Content.Controls.Add(_grid)

        _employeeId.Minimum = 0D
        _employeeId.Maximum = Integer.MaxValue
        ConfigureMonth(_fromMonth)
        ConfigureMonth(_toMonth)
        _useMonths.Text = "Only these wage months"
        _useMonths.AutoSize = True
        _includeCancelled.Text = "Show cancelled claims"
        _includeCancelled.AutoSize = True
        _cancelReason.MaxLength = ArrearsService.MaxReasonLength

        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Employee id (0 for everyone)", _employeeId)
        AddField(layout, "Months", _useMonths)
        AddField(layout, "First wage month", _fromMonth)
        AddField(layout, "Last wage month", _toMonth)
        AddField(layout, "Cancelled", _includeCancelled)
        AddField(layout, "Reason to cancel the picked claim", _cancelReason)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        _cancel.Text = "Cancel claim"
        _cancel.AutoSize = True
        _load.Text = "Load"
        _load.AutoSize = True
        bar.Controls.Add(_exit)
        bar.Controls.Add(_cancel)
        bar.Controls.Add(_load)
        AddHandler _load.Click, Sub(sender As Object, e As EventArgs) DoLoad()
        AddHandler _cancel.Click, Sub(sender As Object, e As EventArgs) DoCancel()
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
        _grid.MultiSelect = False
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn(NameOf(ClaimLine.ClaimNo), "Claim", 6, True)
        AddColumn(NameOf(ClaimLine.EmpCode), "Code", 8, False)
        AddColumn(NameOf(ClaimLine.FullName), "Name", 16, False)
        AddColumn(NameOf(ClaimLine.Months), "Wage months", 12, False)
        AddColumn(NameOf(ClaimLine.Gross), "Gross arrears", 10, True)
        AddColumn(NameOf(ClaimLine.Deductions), "EPF and ESI", 9, True)
        AddColumn(NameOf(ClaimLine.Net), "Net before tax", 10, True)
        AddColumn(NameOf(ClaimLine.State), "Status", 8, False)
        AddColumn(NameOf(ClaimLine.Reason), "Reason", 21, False)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single, right As Boolean)
        Dim column As New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight, .Name = property_}
        If right Then column.DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}
        _grid.Columns.Add(column)
    End Sub

    Private Sub DoLoad()
        ClearValidation()
        Dim filter As New ArrearsFilter With {
            .EmployeeId = If(_employeeId.Value > 0D, CType(CInt(_employeeId.Value), Integer?), Nothing),
            .IncludeCancelled = _includeCancelled.Checked}
        If _useMonths.Checked Then
            filter.FromMonth = New Date(_fromMonth.Value.Year, _fromMonth.Value.Month, 1)
            filter.ToMonth = New Date(_toMonth.Value.Year, _toMonth.Value.Month, 1)
        End If
        Dim result As OperationResult(Of IReadOnlyList(Of ArrearsClaimRow)) = _services.Arrears.List(filter)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _grid.DataSource = result.Value.Select(Function(c) New ClaimLine(c)).ToList()
        ShowStatus($"{result.Value.Count.ToString(CultureInfo.InvariantCulture)} claims.")
    End Sub

    Private Sub DoCancel()
        ClearValidation()
        Dim picked As ClaimLine = TryCast(If(_grid.CurrentRow Is Nothing, Nothing, _grid.CurrentRow.DataBoundItem), ClaimLine)
        If picked Is Nothing Then
            ShowError("Pick a claim in the list first.")
            Return
        End If
        Dim result As OperationResult = _services.Arrears.Cancel(picked.ClaimId, _cancelReason.Text)
        If Not result.Succeeded Then
            SetFieldError(_cancelReason, result.Message)
            ShowError(result.Message)
            Return
        End If
        _cancelReason.Clear()
        DoLoad()
        ShowStatus("The claim is cancelled.")
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        DoLoad()
    End Sub

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("C0", Indian)
    End Function

    Private NotInheritable Class ClaimLine
        Public Sub New(claim As ArrearsClaimRow)
            ClaimId = claim.ClaimId
            ClaimNo = claim.ClaimId.ToString(CultureInfo.InvariantCulture)
            EmpCode = claim.EmpCode
            FullName = claim.FullName
            Months = If(claim.FromMonth = claim.ToMonth, claim.FromMonth.ToString(MonthFormat, CultureInfo.InvariantCulture),
                        claim.FromMonth.ToString(MonthFormat, CultureInfo.InvariantCulture) & " to " & claim.ToMonth.ToString(MonthFormat, CultureInfo.InvariantCulture))
            Gross = Money(claim.GrossTotal)
            Deductions = Money(claim.EmployeeDeductions)
            Net = Money(claim.NetBeforeTax)
            State = If(claim.Status = ArrearsClaimStatus.Open, "Open", "Cancelled")
            Reason = claim.Reason
        End Sub

        Public ReadOnly Property ClaimId As Integer
        Public ReadOnly Property ClaimNo As String
        Public ReadOnly Property EmpCode As String
        Public ReadOnly Property FullName As String
        Public ReadOnly Property Months As String
        Public ReadOnly Property Gross As String
        Public ReadOnly Property Deductions As String
        Public ReadOnly Property Net As String
        Public ReadOnly Property State As String
        Public ReadOnly Property Reason As String
    End Class
End Class
