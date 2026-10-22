Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>
''' Shift assignment: pick an employee, see the shift history, assign a shift from a date.
''' The new shift ends the open one the day before. The employee list is the roll of today's attendance sheet.
''' </summary>
Friend NotInheritable Class ShiftAssignForm
    Inherits BaseForm

    Private Const ListWidth As Integer = 420
    Private Const FormWidth As Integer = 960
    Private Const FormHeight As Integer = 560
    Private Const GridHeight As Integer = 200
    Private Const DateFormat As String = "dd-MM-yyyy"
    Private Const OpenEnded As String = "Open"

    Private ReadOnly _services As AttendanceServices
    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _search As New TextBox() With {.Dock = DockStyle.Top}
    Private ReadOnly _employeeGrid As New DataGridView()
    Private ReadOnly _historyGrid As New DataGridView()
    Private ReadOnly _who As New Label() With {.AutoSize = True, .Font = New Font("Segoe UI", 10.5F, FontStyle.Bold)}
    Private ReadOnly _shift As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList}
    Private ReadOnly _from As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = DateFormat}
    Private ReadOnly _assignButton As New Button() With {.Text = "Assign shift", .AutoSize = True}
    Private ReadOnly _closeButton As New Button() With {.Text = "Close", .AutoSize = True, .DialogResult = DialogResult.Cancel}
    Private _roll As IReadOnlyList(Of AttendanceSheetRow) = New List(Of AttendanceSheetRow)()
    Private _employeeId As Integer
    Private _loading As Boolean

    Public Sub New(services As AttendanceServices, canEdit As Boolean)
        MyBase.New("Shift assignment", "Attendance")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _canEdit = canEdit
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        Dim listPanel As New Panel With {.Dock = DockStyle.Left, .Width = ListWidth}
        ConfigureGrid(_employeeGrid, New String() {"Code", "Employee"})
        _employeeGrid.Dock = DockStyle.Fill
        listPanel.Controls.Add(_employeeGrid)
        listPanel.Controls.Add(_search)

        Dim details As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(Theme.Space2)}
        Dim layout As New TableLayoutPanel With {.ColumnCount = 1, .Dock = DockStyle.Top, .AutoSize = True}
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        ConfigureGrid(_historyGrid, New String() {"Shift", "From", "To"})
        _historyGrid.Height = GridHeight
        _historyGrid.Dock = DockStyle.Top
        layout.Controls.Add(_who)
        layout.Controls.Add(_historyGrid)
        AddField(layout, "New shift", _shift)
        AddField(layout, "Effective from", _from)
        details.Controls.Add(layout)

        Content.Controls.Add(details)
        Content.Controls.Add(listPanel)
        Dim bar = CreateButtonBar()
        _assignButton.Enabled = _canEdit
        bar.Controls.Add(_assignButton)
        bar.Controls.Add(_closeButton)
        CancelButton = _closeButton
        AddHandler _search.TextChanged, Sub(sender As Object, e As EventArgs) FillEmployees()
        AddHandler _employeeGrid.SelectionChanged, AddressOf OnEmployeeSelected
        AddHandler _assignButton.Click, Sub(sender As Object, e As EventArgs) Assign()
        AddHandler _closeButton.Click, Sub(sender As Object, e As EventArgs) Close()
    End Sub

    Private Shared Sub ConfigureGrid(grid As DataGridView, headers As String())
        grid.ReadOnly = True
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.RowHeadersVisible = False
        grid.MultiSelect = False
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        For Each header In headers
            grid.Columns.Add(header, header)
        Next
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadShifts()
        LoadRoll()
        _search.Focus()
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        LoadShifts()
        LoadRoll()
    End Sub

    Protected Overrides Sub OnSaveRequested()
        Assign()
    End Sub

    Private Sub LoadShifts()
        Dim result = _services.Roster.ListShifts()
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Dim choices As New List(Of KeyValuePair(Of Integer, String))()
        For Each item In result.Value
            If item.IsActive AndAlso item.ShiftId.HasValue Then choices.Add(New KeyValuePair(Of Integer, String)(item.ShiftId.Value, item.Name))
        Next
        _shift.DataSource = choices
        _shift.DisplayMember = "Value"
        _shift.ValueMember = "Key"
        If choices.Count = 0 Then ShowError("Create a shift first (Attendance > Shifts).")
    End Sub

    Private Sub LoadRoll()
        Dim result = _services.Board.LoadDay(Date.Today)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _roll = result.Value
        FillEmployees()
    End Sub

    Private Sub FillEmployees()
        Dim filter As String = _search.Text.Trim()
        _loading = True
        Try
            _employeeGrid.Rows.Clear()
            For Each person In _roll
                If filter.Length > 0 AndAlso person.EmpCode.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 AndAlso
                   person.EmployeeName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 Then Continue For
                Dim at = _employeeGrid.Rows.Add(person.EmpCode, person.EmployeeName)
                _employeeGrid.Rows(at).Tag = person
            Next
            _employeeGrid.ClearSelection()
            _employeeGrid.CurrentCell = Nothing
        Finally
            _loading = False
        End Try
        ShowEmployee(Nothing)
        ShowStatus($"{_employeeGrid.Rows.Count} employee(s).")
    End Sub

    Private Sub OnEmployeeSelected(sender As Object, e As EventArgs)
        If _loading Then Return
        ClearValidation()
        If _employeeGrid.CurrentRow Is Nothing Then
            ShowEmployee(Nothing)
        Else
            ShowEmployee(DirectCast(_employeeGrid.CurrentRow.Tag, AttendanceSheetRow))
        End If
    End Sub

    Private Sub ShowEmployee(person As AttendanceSheetRow)
        _historyGrid.Rows.Clear()
        If person Is Nothing Then
            _employeeId = 0
            _who.Text = "Select an employee."
            Return
        End If
        _employeeId = person.EmployeeId
        _who.Text = $"{person.EmpCode}  {person.EmployeeName}"
        Dim result = _services.Roster.ListEmployeeShifts(person.EmployeeId)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        For Each item In result.Value
            _historyGrid.Rows.Add(item.ShiftName, item.EffectiveFrom.ToString(DateFormat, CultureInfo.InvariantCulture),
                                  If(item.EffectiveTo.HasValue, item.EffectiveTo.Value.ToString(DateFormat, CultureInfo.InvariantCulture), OpenEnded))
        Next
    End Sub

    Private Sub Assign()
        If Not _canEdit Then
            ShowError("You are not allowed to assign shifts.")
            Return
        End If
        If _employeeId = 0 Then
            ShowError("Select an employee first.")
            Return
        End If
        If _shift.SelectedValue Is Nothing Then
            ShowError("Choose a shift.")
            Return
        End If
        ClearValidation()
        Dim result = _services.Roster.AssignShift(_employeeId, Convert.ToInt32(_shift.SelectedValue), _from.Value.Date)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        If _employeeGrid.CurrentRow IsNot Nothing Then ShowEmployee(DirectCast(_employeeGrid.CurrentRow.Tag, AttendanceSheetRow))
        ShowStatus("Shift assigned.")
    End Sub
End Class
