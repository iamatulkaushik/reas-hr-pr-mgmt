Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Payroll

''' <summary>
''' Leave accrual screen, plain version for testing (Brave; Sigma moves it into the company settings screen as one field group, CR 0020).
''' Pick a leave type and set its rule: days earned for every so many days worked, how a fraction is rounded, and an optional cap on the balance. Save rule.
''' Then pick a month, Preview to see who earns what, and Post to write it to the ledger. A month is posted once per leave type. Owner and Associate only.
''' </summary>
Friend NotInheritable Class LeaveAccrualForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1000
    Private Const FormHeight As Integer = 700
    Private Const MaxDays As Decimal = 366D
    Private Const MonthFormat As String = "MMM yyyy"

    Private ReadOnly _services As PayrollServices
    Private ReadOnly _leaveType As New ComboBox()
    Private ReadOnly _earned As New NumericUpDown()
    Private ReadOnly _per As New NumericUpDown()
    Private ReadOnly _rounding As New ComboBox()
    Private ReadOnly _useCap As New CheckBox()
    Private ReadOnly _cap As New NumericUpDown()
    Private ReadOnly _month As New DateTimePicker()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _saveRule As New Button()
    Private ReadOnly _preview As New Button()
    Private ReadOnly _post As New Button()
    Private ReadOnly _exit As New Button()
    Private _items As IReadOnlyList(Of LeaveAccrualSettingItem) = New List(Of LeaveAccrualSettingItem)()

    Public Sub New(services As PayrollServices)
        MyBase.New("Leave accrual", "Set how leave is earned for each leave type, then preview and post each month's accrual.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
        LoadSettings()
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid()
        Content.Controls.Add(_grid)

        _leaveType.DropDownStyle = ComboBoxStyle.DropDownList
        _earned.DecimalPlaces = 2
        _earned.Maximum = MaxDays
        _per.DecimalPlaces = 2
        _per.Minimum = 1D
        _per.Maximum = MaxDays
        _per.Value = 20D
        _rounding.DropDownStyle = ComboBoxStyle.DropDownList
        _rounding.Items.Add(New RoundingChoice("Half a day or more counts as one", Revolution.Core.RateRounding.Nearest))
        _rounding.Items.Add(New RoundingChoice("Always up", Revolution.Core.RateRounding.Up))
        _rounding.Items.Add(New RoundingChoice("Always down", Revolution.Core.RateRounding.Down))
        _rounding.SelectedIndex = 0
        _useCap.Text = "Limit the balance"
        _useCap.AutoSize = True
        _cap.DecimalPlaces = 2
        _cap.Maximum = MaxDays
        _cap.Enabled = False
        _month.Format = DateTimePickerFormat.Custom
        _month.CustomFormat = MonthFormat
        _month.ShowUpDown = True

        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Leave type", _leaveType)
        AddField(layout, "Days earned", _earned)
        AddField(layout, "For every (days worked)", _per)
        AddField(layout, "Fraction", _rounding)
        AddField(layout, "Balance limit", _useCap)
        AddField(layout, "Limit (days)", _cap)
        AddField(layout, "Month to post", _month)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        _post.Text = "Post month"
        _post.AutoSize = True
        _post.Enabled = False
        _preview.Text = "Preview month"
        _preview.AutoSize = True
        _saveRule.Text = "Save rule"
        _saveRule.AutoSize = True
        bar.Controls.Add(_exit)
        bar.Controls.Add(_post)
        bar.Controls.Add(_preview)
        bar.Controls.Add(_saveRule)
        AddHandler _useCap.CheckedChanged, Sub(sender As Object, e As EventArgs) _cap.Enabled = _useCap.Checked
        AddHandler _leaveType.SelectedIndexChanged, Sub(sender As Object, e As EventArgs) ShowRule()
        AddHandler _month.ValueChanged, Sub(sender As Object, e As EventArgs) ForgetPreview()
        AddHandler _saveRule.Click, Sub(sender As Object, e As EventArgs) DoSave()
        AddHandler _preview.Click, Sub(sender As Object, e As EventArgs) DoPreview()
        AddHandler _post.Click, Sub(sender As Object, e As EventArgs) DoPost()
    End Sub

    Private Sub ConfigureGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoGenerateColumns = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn(NameOf(AccrualLine.EmployeeId), "Employee id", 20)
        AddColumn(NameOf(AccrualLine.Present), "Days present", 20)
        AddColumn(NameOf(AccrualLine.Balance), "Balance before", 20)
        AddColumn(NameOf(AccrualLine.Earned), "Leave earned", 20)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single)
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight, .Name = property_,
                                                              .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}})
    End Sub

    Private Sub LoadSettings()
        Dim result As OperationResult(Of IReadOnlyList(Of LeaveAccrualSettingItem)) = AccrualService().ListSettings()
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _items = result.Value
        _leaveType.Items.Clear()
        For Each item As LeaveAccrualSettingItem In _items
            _leaveType.Items.Add(item.Code & " - " & item.Name)
        Next
        If _leaveType.Items.Count > 0 Then _leaveType.SelectedIndex = 0
    End Sub

    Private Function AccrualService() As LeaveAccrualService
        If _services.LeaveAccrual Is Nothing Then Throw New InvalidOperationException("Leave accrual is not available.")
        Return _services.LeaveAccrual
    End Function

    Private Function Picked() As LeaveAccrualSettingItem
        Dim index As Integer = _leaveType.SelectedIndex
        Return If(index >= 0 AndAlso index < _items.Count, _items(index), Nothing)
    End Function

    Private Sub ShowRule()
        ForgetPreview()
        Dim item As LeaveAccrualSettingItem = Picked()
        If item Is Nothing OrElse item.Setting Is Nothing Then
            _earned.Value = 0D
            _per.Value = 20D
            _rounding.SelectedIndex = 0
            _useCap.Checked = False
            If item IsNot Nothing Then ShowStatus("No rule is set for this leave type yet.")
            Return
        End If
        _earned.Value = item.Setting.EarnedDays
        _per.Value = item.Setting.PerWorkedDays
        For i As Integer = 0 To _rounding.Items.Count - 1
            If DirectCast(_rounding.Items(i), RoundingChoice).Value = item.Setting.Rounding Then _rounding.SelectedIndex = i
        Next
        _useCap.Checked = item.Setting.MaxBalance.HasValue
        _cap.Value = If(item.Setting.MaxBalance.HasValue, item.Setting.MaxBalance.Value, 0D)
    End Sub

    Private Sub ForgetPreview()
        _grid.DataSource = Nothing
        _post.Enabled = False
    End Sub

    Private Sub DoSave()
        ClearValidation()
        Dim item As LeaveAccrualSettingItem = Picked()
        If item Is Nothing Then
            ShowError("Pick a leave type.")
            Return
        End If
        Dim choice As RoundingChoice = DirectCast(_rounding.SelectedItem, RoundingChoice)
        Dim result As OperationResult = AccrualService().SaveSetting(item.LeaveTypeId, _earned.Value, _per.Value, choice.Value, If(_useCap.Checked, CType(_cap.Value, Decimal?), Nothing))
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Dim keep As Integer = _leaveType.SelectedIndex
        LoadSettings()
        _leaveType.SelectedIndex = keep
        ShowStatus("The rule is saved.")
    End Sub

    Private Sub DoPreview()
        ClearValidation()
        ForgetPreview()
        Dim item As LeaveAccrualSettingItem = Picked()
        If item Is Nothing Then
            ShowError("Pick a leave type.")
            Return
        End If
        Dim result As OperationResult(Of LeaveAccrualPreview) = AccrualService().Preview(item.LeaveTypeId, _month.Value)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _grid.DataSource = result.Value.Entries.Select(Function(e) New AccrualLine(e)).ToList()
        _post.Enabled = result.Value.Entries.Count > 0
        ShowStatus($"{result.Value.Entries.Count.ToString(CultureInfo.InvariantCulture)} of {result.Value.EmployeesChecked.ToString(CultureInfo.InvariantCulture)} employees earn leave, " &
                   $"{result.Value.TotalDays.ToString("0.##", CultureInfo.InvariantCulture)} days in all.")
    End Sub

    Private Sub DoPost()
        ClearValidation()
        Dim item As LeaveAccrualSettingItem = Picked()
        If item Is Nothing Then Return
        Dim answer As DialogResult = MessageBox.Show(Me, "Post this month's leave to the ledger? A month can be posted only once for a leave type.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If answer <> DialogResult.Yes Then Return
        Dim result As OperationResult(Of Integer) = AccrualService().Post(item.LeaveTypeId, _month.Value)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        ForgetPreview()
        ShowStatus($"Posted leave for {result.Value.ToString(CultureInfo.InvariantCulture)} employees.")
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        LoadSettings()
    End Sub

    Private NotInheritable Class RoundingChoice
        Public Sub New(text As String, value As Revolution.Core.RateRounding)
            Me.Text = text
            Me.Value = value
        End Sub

        Public ReadOnly Property Text As String
        Public ReadOnly Property Value As Revolution.Core.RateRounding

        Public Overrides Function ToString() As String
            Return Text
        End Function
    End Class

    Private NotInheritable Class AccrualLine
        Public Sub New(entry As LeaveAccrualEntry)
            EmployeeId = entry.EmployeeId.ToString(CultureInfo.InvariantCulture)
            Present = entry.PresentDays.ToString("0.##", CultureInfo.InvariantCulture)
            Balance = entry.Balance.ToString("0.##", CultureInfo.InvariantCulture)
            Earned = entry.Days.ToString("0.##", CultureInfo.InvariantCulture)
        End Sub

        Public ReadOnly Property EmployeeId As String
        Public ReadOnly Property Present As String
        Public ReadOnly Property Balance As String
        Public ReadOnly Property Earned As String
    End Class
End Class
