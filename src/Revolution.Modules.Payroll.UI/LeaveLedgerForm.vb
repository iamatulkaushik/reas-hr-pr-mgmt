Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Payroll

''' <summary>
''' Leave ledger screen, plain version for testing (Brave; Sigma restyles it). Type an employee id and Load to see the balance of every leave type and the ledger below it. Post an entry
''' (days as a positive number, except for an Adjustment, which may be negative), or add a leave type with its encashment and carry-forward limits (zero means no limit). The ledger only grows:
''' a mistake is corrected with an Adjustment.
''' </summary>
Friend NotInheritable Class LeaveLedgerForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1000
    Private Const FormHeight As Integer = 780
    Private Const BalanceHeight As Integer = 150
    Private Const MaxDays As Decimal = 366D

    Private ReadOnly _services As PayrollServices
    Private ReadOnly _employeeId As New NumericUpDown()
    Private ReadOnly _leaveType As New ComboBox()
    Private ReadOnly _kind As New ComboBox()
    Private ReadOnly _date As New DateTimePicker()
    Private ReadOnly _days As New NumericUpDown()
    Private ReadOnly _note As New TextBox()
    Private ReadOnly _typeCode As New TextBox()
    Private ReadOnly _typeName As New TextBox()
    Private ReadOnly _encashable As New CheckBox()
    Private ReadOnly _maxEncash As New NumericUpDown()
    Private ReadOnly _maxCarry As New NumericUpDown()
    Private ReadOnly _balances As New DataGridView()
    Private ReadOnly _entries As New DataGridView()
    Private ReadOnly _load As New Button()
    Private ReadOnly _post As New Button()
    Private ReadOnly _saveType As New Button()
    Private ReadOnly _exit As New Button()
    Private _types As IReadOnlyList(Of LeaveTypeItem) = New List(Of LeaveTypeItem)()

    Public Sub New(services As PayrollServices)
        MyBase.New("Leave ledger", "See an employee's leave balances and post entries. The ledger only grows; correct a mistake with an Adjustment.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
        LoadTypes()
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid(_entries)
        ConfigureGrid(_balances)
        _entries.Dock = DockStyle.Fill
        _balances.Dock = DockStyle.Top
        _balances.Height = BalanceHeight
        AddColumn(_balances, NameOf(BalanceRow.Name), "Leave type", 20, False)
        AddColumn(_balances, NameOf(BalanceRow.Accrued), "Accrued", 10, True)
        AddColumn(_balances, NameOf(BalanceRow.CarriedForward), "Carried forward", 12, True)
        AddColumn(_balances, NameOf(BalanceRow.Taken), "Taken", 10, True)
        AddColumn(_balances, NameOf(BalanceRow.Encashed), "Encashed", 10, True)
        AddColumn(_balances, NameOf(BalanceRow.Lapsed), "Lapsed", 10, True)
        AddColumn(_balances, NameOf(BalanceRow.Adjusted), "Adjusted", 10, True)
        AddColumn(_balances, NameOf(BalanceRow.Balance), "Balance", 10, True)
        AddColumn(_entries, NameOf(EntryRow.EntryDate), "Date", 14, False)
        AddColumn(_entries, NameOf(EntryRow.LeaveType), "Leave type", 20, False)
        AddColumn(_entries, NameOf(EntryRow.Kind), "Entry", 16, False)
        AddColumn(_entries, NameOf(EntryRow.Days), "Days", 10, True)
        AddColumn(_entries, NameOf(EntryRow.Year), "Leave year", 10, False)
        AddColumn(_entries, NameOf(EntryRow.Note), "Note", 30, False)
        Content.Controls.Add(_entries)
        Content.Controls.Add(_balances)

        _employeeId.Minimum = 1D
        _employeeId.Maximum = Integer.MaxValue
        _leaveType.DropDownStyle = ComboBoxStyle.DropDownList
        _kind.DropDownStyle = ComboBoxStyle.DropDownList
        _kind.DataSource = [Enum].GetValues(GetType(LeaveEntryKind))
        _date.Format = DateTimePickerFormat.Short
        _days.DecimalPlaces = 2
        _days.Minimum = -MaxDays
        _days.Maximum = MaxDays
        _note.MaxLength = 200
        _typeCode.MaxLength = 10
        _typeName.MaxLength = 100
        _encashable.Text = "Can be encashed at exit"
        _encashable.AutoSize = True
        _maxEncash.DecimalPlaces = 2
        _maxEncash.Maximum = MaxDays
        _maxCarry.DecimalPlaces = 2
        _maxCarry.Maximum = MaxDays

        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Employee id", _employeeId)
        AddField(layout, "Leave type", _leaveType)
        AddField(layout, "Entry", _kind)
        AddField(layout, "Date", _date)
        AddField(layout, "Days", _days)
        AddField(layout, "Note", _note)
        AddField(layout, "New type: code", _typeCode)
        AddField(layout, "New type: name", _typeName)
        AddField(layout, "New type: encashment", _encashable)
        AddField(layout, "New type: most days to encash (0 for no limit)", _maxEncash)
        AddField(layout, "New type: most days to carry forward (0 for no limit)", _maxCarry)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        bar.Controls.Add(_exit)
        For Each pair In {Tuple.Create(_saveType, "Save leave type"), Tuple.Create(_post, "Post entry"), Tuple.Create(_load, "Load")}
            pair.Item1.Text = pair.Item2
            pair.Item1.AutoSize = True
            bar.Controls.Add(pair.Item1)
        Next
        AddHandler _load.Click, Sub(sender As Object, e As EventArgs) DoLoad()
        AddHandler _post.Click, Sub(sender As Object, e As EventArgs) DoPost()
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
        Dim result As OperationResult(Of IReadOnlyList(Of LeaveTypeItem)) = _services.Leave.ListTypes()
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _types = result.Value
        _leaveType.DataSource = Nothing
        _leaveType.DisplayMember = NameOf(LeaveTypeItem.Name)
        _leaveType.DataSource = _types.Where(Function(t) t.IsActive).ToList()
    End Sub

    Private Function SelectedType() As LeaveTypeItem
        Return TryCast(_leaveType.SelectedItem, LeaveTypeItem)
    End Function

    Private Sub DoLoad()
        ClearValidation()
        Dim employee As Integer = CInt(_employeeId.Value)
        Dim balances As OperationResult(Of IReadOnlyList(Of LeaveBalanceRow)) = _services.Leave.ListBalances(employee)
        If Not balances.Succeeded Then
            ShowError(balances.Message)
            Return
        End If
        Dim entries As OperationResult(Of IReadOnlyList(Of LeaveEntryView)) = _services.Leave.ListEntries(employee, Nothing)
        If Not entries.Succeeded Then
            ShowError(entries.Message)
            Return
        End If
        _balances.DataSource = balances.Value.Select(Function(b) New BalanceRow(b)).ToList()
        _entries.DataSource = entries.Value.Select(Function(e) New EntryRow(e, _types)).ToList()
        ShowStatus($"{balances.Value.Count.ToString(CultureInfo.InvariantCulture)} leave types, {entries.Value.Count.ToString(CultureInfo.InvariantCulture)} ledger entries.")
    End Sub

    Private Sub DoPost()
        ClearValidation()
        Dim leaveType As LeaveTypeItem = SelectedType()
        If leaveType Is Nothing OrElse Not leaveType.LeaveTypeId.HasValue Then
            ShowError("Pick a leave type. Add one first if the list is empty.")
            Return
        End If
        Dim kind As LeaveEntryKind = CType(_kind.SelectedItem, LeaveEntryKind)
        Dim result As OperationResult(Of Long) = _services.Leave.Post(CInt(_employeeId.Value), leaveType.LeaveTypeId.Value, _date.Value.Date, kind, _days.Value, _note.Text)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        DoLoad()
        ShowStatus($"Entry {result.Value.ToString(CultureInfo.InvariantCulture)} posted.")
    End Sub

    Private Sub DoSaveType()
        ClearValidation()
        Dim item As New LeaveTypeItem(Nothing, _typeCode.Text, _typeName.Text, _encashable.Checked,
                                      If(_maxEncash.Value > 0D, CType(_maxEncash.Value, Decimal?), Nothing), If(_maxCarry.Value > 0D, CType(_maxCarry.Value, Decimal?), Nothing), True)
        Dim result As OperationResult(Of Integer) = _services.Leave.SaveType(item)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        LoadTypes()
        ShowStatus($"Leave type {_typeCode.Text.Trim().ToUpperInvariant()} saved.")
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        LoadTypes()
    End Sub

    Private NotInheritable Class BalanceRow
        Public Sub New(row As LeaveBalanceRow)
            Name = row.Name
            Accrued = Days(row.AccruedDays)
            CarriedForward = Days(row.CarriedForwardDays)
            Taken = Days(row.TakenDays)
            Encashed = Days(row.EncashedDays)
            Lapsed = Days(row.LapsedDays)
            Adjusted = Days(row.AdjustedDays)
            Balance = Days(row.BalanceDays)
        End Sub

        Public ReadOnly Property Name As String
        Public ReadOnly Property Accrued As String
        Public ReadOnly Property CarriedForward As String
        Public ReadOnly Property Taken As String
        Public ReadOnly Property Encashed As String
        Public ReadOnly Property Lapsed As String
        Public ReadOnly Property Adjusted As String
        Public ReadOnly Property Balance As String
    End Class

    Private NotInheritable Class EntryRow
        Public Sub New(entry As LeaveEntryView, types As IReadOnlyList(Of LeaveTypeItem))
            EntryDate = entry.EntryDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
            Dim match As LeaveTypeItem = types.FirstOrDefault(Function(t) t.LeaveTypeId.HasValue AndAlso t.LeaveTypeId.Value = entry.LeaveTypeId)
            LeaveType = If(match Is Nothing, entry.LeaveTypeId.ToString(CultureInfo.InvariantCulture), match.Name)
            Kind = entry.Kind.ToString()
            Me.Days = LeaveLedgerForm.Days(entry.Days)
            Year = $"{entry.LeaveYear.ToString(CultureInfo.InvariantCulture)}-{((entry.LeaveYear + 1) Mod 100).ToString("00", CultureInfo.InvariantCulture)}"
            Note = entry.Note
        End Sub

        Public ReadOnly Property EntryDate As String
        Public ReadOnly Property LeaveType As String
        Public ReadOnly Property Kind As String
        Public ReadOnly Property Days As String
        Public ReadOnly Property Year As String
        Public ReadOnly Property Note As String
    End Class

    Private Shared Function Days(value As Decimal) As String
        Return value.ToString("0.##", CultureInfo.InvariantCulture)
    End Function
End Class
