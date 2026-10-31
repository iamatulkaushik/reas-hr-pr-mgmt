Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Payroll

''' <summary>
''' Loans screen, plain version for testing (Brave; Sigma restyles it). Add a loan type, then for an employee Preview a schedule, Sanction a loan, see the employee's loans and the schedule of the
''' one selected, and Cancel it. The first due month is picked as a year and month. Every action is checked again by LoanService.
''' </summary>
Friend NotInheritable Class LoansForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1000
    Private Const FormHeight As Integer = 800
    Private Const LoansHeight As Integer = 150
    Private Const MaxMoney As Decimal = 99999999D

    Private Shared ReadOnly Indian As CultureInfo = CultureInfo.GetCultureInfo("en-IN")

    Private ReadOnly _services As PayrollServices
    Private ReadOnly _employeeId As New NumericUpDown()
    Private ReadOnly _loanType As New ComboBox()
    Private ReadOnly _principal As New NumericUpDown()
    Private ReadOnly _installments As New NumericUpDown()
    Private ReadOnly _firstYear As New NumericUpDown()
    Private ReadOnly _firstMonth As New NumericUpDown()
    Private ReadOnly _sanctionDate As New DateTimePicker()
    Private ReadOnly _typeName As New TextBox()
    Private ReadOnly _typeMethod As New ComboBox()
    Private ReadOnly _typeInterest As New NumericUpDown()
    Private ReadOnly _typeMax As New NumericUpDown()
    Private ReadOnly _loans As New DataGridView()
    Private ReadOnly _schedule As New DataGridView()
    Private ReadOnly _load As New Button()
    Private ReadOnly _preview As New Button()
    Private ReadOnly _sanction As New Button()
    Private ReadOnly _cancel As New Button()
    Private ReadOnly _saveType As New Button()
    Private ReadOnly _exit As New Button()
    Private _types As IReadOnlyList(Of LoanTypeItem) = New List(Of LoanTypeItem)()
    Private _loanList As IReadOnlyList(Of LoanSummary) = New List(Of LoanSummary)()

    Public Sub New(services As PayrollServices)
        MyBase.New("Loans", "Add loan types, sanction loans for employees, and see their schedules. EMI is recovered by the payroll run.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
        LoadTypes()
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid(_schedule)
        ConfigureGrid(_loans)
        _schedule.Dock = DockStyle.Fill
        _loans.Dock = DockStyle.Top
        _loans.Height = LoansHeight
        _loans.MultiSelect = False
        _loans.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        AddColumn(_loans, NameOf(LoanRow.LoanId), "Loan id", 8, False)
        AddColumn(_loans, NameOf(LoanRow.Sanctioned), "Sanctioned", 14, False)
        AddColumn(_loans, NameOf(LoanRow.Principal), "Principal", 14, True)
        AddColumn(_loans, NameOf(LoanRow.Interest), "Interest", 12, True)
        AddColumn(_loans, NameOf(LoanRow.Installments), "Installments", 10, True)
        AddColumn(_loans, NameOf(LoanRow.Outstanding), "Outstanding", 14, True)
        AddColumn(_loans, NameOf(LoanRow.Status), "Status", 10, False)
        AddColumn(_schedule, NameOf(ScheduleRow.No), "No.", 6, False)
        AddColumn(_schedule, NameOf(ScheduleRow.Due), "Due month", 12, False)
        AddColumn(_schedule, NameOf(ScheduleRow.PrincipalPart), "Principal part", 14, True)
        AddColumn(_schedule, NameOf(ScheduleRow.InterestPart), "Interest part", 14, True)
        AddColumn(_schedule, NameOf(ScheduleRow.Amount), "Instalment", 14, True)
        AddColumn(_schedule, NameOf(ScheduleRow.Recovered), "Recovered", 14, True)
        Content.Controls.Add(_schedule)
        Content.Controls.Add(_loans)
        AddHandler _loans.SelectionChanged, Sub(sender As Object, e As EventArgs) ShowSelectedSchedule()

        _employeeId.Minimum = 1D
        _employeeId.Maximum = Integer.MaxValue
        _loanType.DropDownStyle = ComboBoxStyle.DropDownList
        _principal.Maximum = MaxMoney
        _installments.Minimum = 1D
        _installments.Maximum = 600D
        _installments.Value = 12D
        _firstYear.Minimum = 2000D
        _firstYear.Maximum = 9998D
        _firstYear.Value = Date.Today.Year
        _firstMonth.Minimum = 1D
        _firstMonth.Maximum = 12D
        _firstMonth.Value = Date.Today.Month
        _sanctionDate.Format = DateTimePickerFormat.Short
        _typeName.MaxLength = 100
        _typeMethod.DropDownStyle = ComboBoxStyle.DropDownList
        _typeMethod.DataSource = [Enum].GetValues(GetType(InterestMethod))
        _typeInterest.DecimalPlaces = 2
        _typeInterest.Maximum = 100D
        _typeMax.Minimum = 1D
        _typeMax.Maximum = 600D
        _typeMax.Value = 24D

        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Employee id", _employeeId)
        AddField(layout, "Loan type", _loanType)
        AddField(layout, "Principal (rupees)", _principal)
        AddField(layout, "Installments", _installments)
        AddField(layout, "First due year", _firstYear)
        AddField(layout, "First due month", _firstMonth)
        AddField(layout, "Sanction date", _sanctionDate)
        AddField(layout, "New type: name", _typeName)
        AddField(layout, "New type: interest method", _typeMethod)
        AddField(layout, "New type: interest per year (%)", _typeInterest)
        AddField(layout, "New type: most installments", _typeMax)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        bar.Controls.Add(_exit)
        For Each pair In {Tuple.Create(_cancel, "Cancel selected loan"), Tuple.Create(_sanction, "Sanction"), Tuple.Create(_preview, "Preview schedule"),
                          Tuple.Create(_saveType, "Save loan type"), Tuple.Create(_load, "Load loans")}
            pair.Item1.Text = pair.Item2
            pair.Item1.AutoSize = True
            bar.Controls.Add(pair.Item1)
        Next
        AddHandler _load.Click, Sub(sender As Object, e As EventArgs) DoLoad()
        AddHandler _preview.Click, Sub(sender As Object, e As EventArgs) DoPreview()
        AddHandler _sanction.Click, Sub(sender As Object, e As EventArgs) DoSanction()
        AddHandler _cancel.Click, Sub(sender As Object, e As EventArgs) DoCancel()
        AddHandler _saveType.Click, Sub(sender As Object, e As EventArgs) DoSaveType()
    End Sub

    Private Shared Sub ConfigureGrid(grid As DataGridView)
        grid.ReadOnly = True
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.RowHeadersVisible = False
        grid.AutoGenerateColumns = False
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    End Sub

    Private Shared Sub AddColumn(grid As DataGridView, property_ As String, header As String, weight As Single, right As Boolean)
        Dim column As New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight}
        If right Then column.DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}
        grid.Columns.Add(column)
    End Sub

    Private Sub LoadTypes()
        Dim result As OperationResult(Of IReadOnlyList(Of LoanTypeItem)) = _services.Loans.ListTypes()
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _types = result.Value
        _loanType.DataSource = Nothing
        _loanType.DisplayMember = NameOf(LoanTypeItem.Name)
        _loanType.DataSource = _types.Where(Function(t) t.IsActive).ToList()
    End Sub

    Private Function SelectedType() As LoanTypeItem
        Dim item As LoanTypeItem = TryCast(_loanType.SelectedItem, LoanTypeItem)
        If item Is Nothing OrElse Not item.LoanTypeId.HasValue Then
            ShowError("Pick a loan type. Add one first if the list is empty.")
            Return Nothing
        End If
        Return item
    End Function

    Private Function FirstDueMonthKey() As Integer
        Return CInt(_firstYear.Value) * 100 + CInt(_firstMonth.Value)
    End Function

    Private Sub DoLoad()
        ClearValidation()
        Dim result As OperationResult(Of IReadOnlyList(Of LoanSummary)) = _services.Loans.ListLoans(CInt(_employeeId.Value))
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _loanList = result.Value
        _loans.DataSource = _loanList.Select(Function(l) New LoanRow(l)).ToList()
        ShowStatus($"{_loanList.Count.ToString(CultureInfo.InvariantCulture)} loans. Select one to see its schedule.")
        ShowSelectedSchedule()
    End Sub

    Private Sub ShowSelectedSchedule()
        _schedule.DataSource = New List(Of ScheduleRow)()
        If _loans.CurrentRow Is Nothing OrElse _loans.CurrentRow.Index < 0 OrElse _loans.CurrentRow.Index >= _loanList.Count Then Return
        Dim result As OperationResult(Of IReadOnlyList(Of LoanInstallmentView)) = _services.Loans.GetSchedule(_loanList(_loans.CurrentRow.Index).LoanId)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _schedule.DataSource = result.Value.Select(Function(i) New ScheduleRow(i)).ToList()
    End Sub

    Private Sub DoPreview()
        ClearValidation()
        Dim loanType As LoanTypeItem = SelectedType()
        If loanType Is Nothing Then Return
        Dim result As OperationResult(Of IReadOnlyList(Of LoanInstallmentPlan)) = _services.Loans.PreviewSchedule(loanType.LoanTypeId.Value, _principal.Value, CInt(_installments.Value), FirstDueMonthKey())
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _schedule.DataSource = result.Value.Select(Function(p) New ScheduleRow(p)).ToList()
        ShowStatus($"Preview only, nothing saved. Total to repay {Money(result.Value.Sum(Function(p) p.Amount))}.")
    End Sub

    Private Sub DoSanction()
        ClearValidation()
        Dim loanType As LoanTypeItem = SelectedType()
        If loanType Is Nothing Then Return
        Dim result As OperationResult(Of Integer) = _services.Loans.Sanction(CInt(_employeeId.Value), loanType.LoanTypeId.Value, _sanctionDate.Value.Date, _principal.Value, CInt(_installments.Value), FirstDueMonthKey())
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        DoLoad()
        ShowStatus($"Loan {result.Value.ToString(CultureInfo.InvariantCulture)} sanctioned.")
    End Sub

    Private Sub DoCancel()
        ClearValidation()
        If _loans.CurrentRow Is Nothing OrElse _loans.CurrentRow.Index < 0 OrElse _loans.CurrentRow.Index >= _loanList.Count Then
            ShowError("Select a loan first.")
            Return
        End If
        Dim result As OperationResult = _services.Loans.Cancel(_loanList(_loans.CurrentRow.Index).LoanId)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        DoLoad()
        ShowStatus("Loan cancelled.")
    End Sub

    Private Sub DoSaveType()
        ClearValidation()
        Dim item As New LoanTypeItem(Nothing, _typeName.Text, CType(_typeMethod.SelectedItem, InterestMethod), _typeInterest.Value, CInt(_typeMax.Value), True)
        Dim result As OperationResult(Of Integer) = _services.Loans.SaveType(item)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        LoadTypes()
        ShowStatus($"Loan type {_typeName.Text.Trim()} saved.")
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        LoadTypes()
    End Sub

    Private Shared Function Money(value As Decimal) As String
        Return value.ToString("C0", Indian)
    End Function

    Private Shared Function MonthText(monthKey As Integer) As String
        Return $"{(monthKey Mod 100).ToString("00", CultureInfo.InvariantCulture)}-{(monthKey \ 100).ToString(CultureInfo.InvariantCulture)}"
    End Function

    Private NotInheritable Class LoanRow
        Public Sub New(loan As LoanSummary)
            LoanId = loan.LoanId
            Sanctioned = loan.SanctionDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
            Principal = Money(loan.Principal)
            Interest = $"{loan.AnnualInterestPct.ToString("0.##", CultureInfo.InvariantCulture)}% {loan.Method}"
            Installments = loan.InstallmentCount
            Outstanding = Money(loan.Outstanding)
            Status = loan.Status.ToString()
        End Sub

        Public ReadOnly Property LoanId As Integer
        Public ReadOnly Property Sanctioned As String
        Public ReadOnly Property Principal As String
        Public ReadOnly Property Interest As String
        Public ReadOnly Property Installments As Integer
        Public ReadOnly Property Outstanding As String
        Public ReadOnly Property Status As String
    End Class

    Private NotInheritable Class ScheduleRow
        Public Sub New(plan As LoanInstallmentPlan)
            No = plan.InstallmentNo
            Due = MonthText(plan.DueMonthKey)
            PrincipalPart = Money(plan.PrincipalPart)
            InterestPart = Money(plan.InterestPart)
            Amount = Money(plan.Amount)
            Recovered = String.Empty
        End Sub

        Public Sub New(view As LoanInstallmentView)
            No = view.InstallmentNo
            Due = MonthText(view.DueMonthKey)
            PrincipalPart = Money(view.PrincipalPart)
            InterestPart = Money(view.InterestPart)
            Amount = Money(view.Amount)
            Recovered = Money(view.Recovered)
        End Sub

        Public ReadOnly Property No As Integer
        Public ReadOnly Property Due As String
        Public ReadOnly Property PrincipalPart As String
        Public ReadOnly Property InterestPart As String
        Public ReadOnly Property Amount As String
        Public ReadOnly Property Recovered As String
    End Class
End Class
