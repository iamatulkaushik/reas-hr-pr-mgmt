Imports System.Windows.Forms
Imports Revolution.Modules.Employees

''' <summary>
''' Employee screen: search and list on the left; Details, IDs and Bank tabs on the right.
''' A new employee is saved on Details first; the IDs and Bank tabs open once the employee exists.
''' </summary>
Friend NotInheritable Class EmployeeForm
    Inherits BaseForm

    Private Const ListWidth As Integer = 460
    Private Const FormWidth As Integer = 1100
    Private Const FormHeight As Integer = 680
    Private Const DateFormat As String = "dd-MM-yyyy"
    Private Const DefaultAge As Integer = 25
    Private Const DefaultNationality As String = "Indian"
    Private Const AllFactories As String = "All factories"

    Private ReadOnly _services As EmployeesServices
    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _search As New TextBox()
    Private ReadOnly _factoryFilter As New ComboBox()
    Private ReadOnly _showInactive As New CheckBox() With {.Text = "Show inactive", .AutoSize = True}
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _tabs As New TabControl() With {.Dock = DockStyle.Fill}
    Private ReadOnly _ids As EmployeeIdsPanel
    Private ReadOnly _bank As EmployeeBankPanel
    Private ReadOnly _code As New TextBox() With {.ReadOnly = True}
    Private ReadOnly _name As New TextBox() With {.MaxLength = 100}
    Private ReadOnly _father As New TextBox() With {.MaxLength = 100}
    Private ReadOnly _gender As New ComboBox()
    Private ReadOnly _dob As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = DateFormat}
    Private ReadOnly _join As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = DateFormat}
    Private ReadOnly _employment As New ComboBox()
    Private ReadOnly _worker As New ComboBox()
    Private ReadOnly _payType As New ComboBox()
    Private ReadOnly _factory As New ComboBox()
    Private ReadOnly _department As New ComboBox()
    Private ReadOnly _designation As New ComboBox()
    Private ReadOnly _marital As New ComboBox()
    Private ReadOnly _mobile As New TextBox() With {.MaxLength = 10}
    Private ReadOnly _email As New TextBox() With {.MaxLength = 150}
    Private ReadOnly _nationality As New TextBox() With {.MaxLength = 50}
    Private ReadOnly _status As New TextBox() With {.ReadOnly = True}
    Private ReadOnly _saveButton As New Button() With {.Text = "Save", .AutoSize = True}
    Private ReadOnly _newButton As New Button() With {.Text = "New", .AutoSize = True}
    Private ReadOnly _closeButton As New Button() With {.Text = "Close", .AutoSize = True, .DialogResult = DialogResult.Cancel}
    Private _departments As IReadOnlyList(Of DepartmentItem) = New List(Of DepartmentItem)()
    Private _detail As EmployeeDetail
    Private _employeeId As Integer
    Private _loading As Boolean

    Public Sub New(services As EmployeesServices, canEdit As Boolean)
        MyBase.New("Employee", "Masters")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _canEdit = canEdit
        _ids = New EmployeeIdsPanel(services, canEdit)
        _bank = New EmployeeBankPanel(services, canEdit)
        ClientSize = New Drawing.Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        Dim detailsPage As New TabPage("Details") With {.AutoScroll = True}
        Dim layout = FieldLayout.Create()
        layout.Padding = New Padding(Theme.Space2)
        FieldLayout.Add(layout, "Employee code", _code)
        FieldLayout.Add(layout, "Full name", _name)
        FieldLayout.Add(layout, "Father / spouse name", _father)
        FieldLayout.Add(layout, "Gender", _gender)
        FieldLayout.Add(layout, "Date of birth", _dob)
        FieldLayout.Add(layout, "Joining date", _join)
        FieldLayout.Add(layout, "Employment type", _employment)
        FieldLayout.Add(layout, "Worker type", _worker)
        FieldLayout.Add(layout, "Pay type", _payType)
        FieldLayout.Add(layout, "Factory", _factory)
        FieldLayout.Add(layout, "Department", _department)
        FieldLayout.Add(layout, "Designation", _designation)
        FieldLayout.Add(layout, "Marital status", _marital)
        FieldLayout.Add(layout, "Mobile", _mobile)
        FieldLayout.Add(layout, "Email", _email)
        FieldLayout.Add(layout, "Nationality", _nationality)
        FieldLayout.Add(layout, "Status", _status)
        detailsPage.Controls.Add(layout)
        _tabs.TabPages.Add(detailsPage)
        _tabs.TabPages.Add(New TabPage("IDs") With {.Padding = New Padding(0)})
        _tabs.TabPages(1).Controls.Add(_ids)
        _tabs.TabPages.Add(New TabPage("Bank"))
        _tabs.TabPages(2).Controls.Add(_bank)
        _ids.Dock = DockStyle.Fill
        _bank.Dock = DockStyle.Fill

        Dim listPanel As New Panel With {.Dock = DockStyle.Left, .Width = ListWidth}
        ConfigureGrid()
        Dim filterBar As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .WrapContents = True}
        _search.Width = 170
        _factoryFilter.Width = 150
        filterBar.Controls.Add(_search)
        filterBar.Controls.Add(_factoryFilter)
        filterBar.Controls.Add(_showInactive)
        listPanel.Controls.Add(_grid)
        listPanel.Controls.Add(filterBar)

        Content.Controls.Add(_tabs)
        Content.Controls.Add(listPanel)
        Dim bar = CreateButtonBar()
        _saveButton.Enabled = _canEdit
        _newButton.Enabled = _canEdit
        bar.Controls.Add(_saveButton)
        bar.Controls.Add(_newButton)
        bar.Controls.Add(_closeButton)
        CancelButton = _closeButton
        WireEvents()
    End Sub

    Private Sub ConfigureGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.MultiSelect = False
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        For Each header In New String() {"Code", "Name", "Factory", "Status"}
            _grid.Columns.Add(header, header)
        Next
    End Sub

    Private Sub WireEvents()
        AddHandler _saveButton.Click, Sub(sender As Object, e As EventArgs) SaveEmployee()
        AddHandler _newButton.Click, Sub(sender As Object, e As EventArgs) StartNew()
        AddHandler _closeButton.Click, Sub(sender As Object, e As EventArgs) Close()
        AddHandler _grid.SelectionChanged, AddressOf OnSelectionChanged
        AddHandler _search.KeyDown, AddressOf OnSearchKeyDown
        AddHandler _factoryFilter.SelectionChangeCommitted, Sub(sender As Object, e As EventArgs) ReloadList(_employeeId)
        AddHandler _showInactive.CheckedChanged, Sub(sender As Object, e As EventArgs) ReloadList(_employeeId)
        AddHandler _factory.SelectionChangeCommitted, Sub(sender As Object, e As EventArgs) FillDepartments(Nothing)
        AddHandler _ids.Notice, AddressOf OnNotice
        AddHandler _bank.Notice, AddressOf OnNotice
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        _dob.MaxDate = Date.Today
        _join.MaxDate = Date.Today.AddYears(1)
        LoadReferenceLists()
        MyBase.OnLoad(e)
        _search.Focus()
        ReloadList(0)
    End Sub

    Protected Overrides Sub OnSaveRequested()
        SaveEmployee()
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        ReloadList(_employeeId)
    End Sub

    Private Sub LoadReferenceLists()
        FieldLayout.Bind(_gender, EmployeeLookups.Genders)
        FieldLayout.Bind(_employment, EmployeeLookups.EmploymentTypes)
        FieldLayout.Bind(_worker, EmployeeLookups.WorkerTypes)
        FieldLayout.Bind(_payType, EmployeeLookups.PayTypes)
        FieldLayout.Bind(_marital, FieldLayout.WithBlank(EmployeeLookups.MaritalStatuses))
        Dim factories = _services.Factories.List(True)
        Dim filter As New List(Of KeyValuePair(Of Integer, String)) From {New KeyValuePair(Of Integer, String)(0, AllFactories)}
        Dim choice As New List(Of KeyValuePair(Of Integer, String))()
        If factories.Succeeded Then
            For Each item In factories.Value
                filter.Add(New KeyValuePair(Of Integer, String)(item.FactoryId, item.Name))
                choice.Add(New KeyValuePair(Of Integer, String)(item.FactoryId, item.Name))
            Next
        Else
            ShowError(factories.Message)
        End If
        FieldLayout.Bind(_factoryFilter, filter)
        FieldLayout.Bind(_factory, choice)
        LoadDepartmentsAndDesignations()
    End Sub

    Private Sub LoadDepartmentsAndDesignations()
        Dim departments = _services.Departments.List(Nothing, True)
        If departments.Succeeded Then _departments = departments.Value
        Dim designations As New List(Of KeyValuePair(Of Integer, String))()
        Dim result = _services.Designations.List(True)
        If result.Succeeded Then
            For Each item In result.Value
                designations.Add(New KeyValuePair(Of Integer, String)(item.DesignationId, item.Name))
            Next
        End If
        FieldLayout.Bind(_designation, FieldLayout.WithBlank(designations))
        FillDepartments(Nothing)
    End Sub

    ''' <summary>Shows the departments of the chosen factory. Pass a department id to select it.</summary>
    Private Sub FillDepartments(selectId As Integer?)
        Dim factoryId As Integer = FieldLayout.IntValue(_factory)
        Dim pairs As New List(Of KeyValuePair(Of Integer, String))()
        For Each item In _departments
            If item.FactoryId = factoryId Then pairs.Add(New KeyValuePair(Of Integer, String)(item.DepartmentId, item.Name))
        Next
        FieldLayout.Bind(_department, FieldLayout.WithBlank(pairs))
        FieldLayout.SelectValue(_department, selectId)
    End Sub

    Private Sub ReloadList(selectId As Integer)
        ClearValidation()
        Dim factoryId As Integer? = FieldLayout.OptionalValue(_factoryFilter)
        Dim result = _services.Employees.List(_search.Text, factoryId, Not _showInactive.Checked)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _loading = True
        Try
            _grid.Rows.Clear()
            For Each item In result.Value
                Dim at = _grid.Rows.Add(item.EmpCode, item.FullName, item.FactoryName, item.Status.ToString())
                _grid.Rows(at).Tag = item.EmployeeId
                If Not item.IsActive Then _grid.Rows(at).DefaultCellStyle.ForeColor = Theme.TextMuted
            Next
            SelectById(selectId)
        Finally
            _loading = False
        End Try
        ShowCurrent()
        ShowStatus($"{_grid.Rows.Count} employee(s).")
    End Sub

    Private Sub SelectById(id As Integer)
        _grid.ClearSelection()
        For Each row As DataGridViewRow In _grid.Rows
            If id <> 0 AndAlso CInt(row.Tag) = id Then
                _grid.CurrentCell = row.Cells(0)
                row.Selected = True
                Return
            End If
        Next
        If id = 0 AndAlso _grid.Rows.Count > 0 Then _grid.CurrentCell = _grid.Rows(0).Cells(0)
    End Sub

    Private Sub ShowCurrent()
        If _grid.CurrentRow Is Nothing Then
            StartNew()
        Else
            ShowEmployee(CInt(_grid.CurrentRow.Tag))
        End If
    End Sub

    Private Sub OnSelectionChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        ClearValidation()
        ShowCurrent()
    End Sub

    Private Sub OnSearchKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode <> Keys.Enter Then Return
        e.SuppressKeyPress = True
        ReloadList(0)
    End Sub

    Private Sub OnNotice(text As String, isError As Boolean)
        If isError Then
            ShowError(text)
        Else
            ClearValidation()
            ShowStatus(text)
        End If
    End Sub

    Private Sub ShowEmployee(id As Integer)
        Dim result = _services.Employees.Get(id)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Dim d = result.Value
        _detail = d
        _employeeId = d.EmployeeId
        _code.Text = d.EmpCode
        _name.Text = d.FullName
        _father.Text = d.FatherOrSpouseName
        _gender.SelectedValue = d.Gender
        _dob.Value = ClampDate(_dob, d.DateOfBirth)
        _join.Value = ClampDate(_join, d.JoinDate)
        _employment.SelectedValue = d.EmploymentTypeId
        _worker.SelectedValue = d.WorkerTypeId
        _payType.SelectedValue = CInt(d.PayType)
        _factory.SelectedValue = d.FactoryId
        FillDepartments(d.DepartmentId)
        FieldLayout.SelectValue(_designation, d.DesignationId)
        FieldLayout.SelectValue(_marital, d.MaritalStatusId)
        _mobile.Text = d.Mobile
        _email.Text = d.Email
        _nationality.Text = d.Nationality
        _status.Text = d.Status.ToString()
        _ids.ShowEmployee(d.EmployeeId)
        _bank.ShowEmployee(d.EmployeeId)
    End Sub

    Private Shared Function ClampDate(picker As DateTimePicker, value As Date) As Date
        If value < picker.MinDate Then Return picker.MinDate
        If value > picker.MaxDate Then Return picker.MaxDate
        Return value
    End Function

    Private Sub StartNew()
        ClearValidation()
        _detail = Nothing
        _employeeId = 0
        _loading = True
        _grid.ClearSelection()
        _grid.CurrentCell = Nothing
        _loading = False
        _code.Text = "(new)"
        _name.Clear()
        _father.Clear()
        _gender.SelectedIndex = 0
        _dob.Value = ClampDate(_dob, Date.Today.AddYears(-DefaultAge))
        _join.Value = ClampDate(_join, Date.Today)
        _employment.SelectedIndex = 0
        _worker.SelectedIndex = 0
        _payType.SelectedIndex = 0
        _factory.SelectedIndex = If(_factory.Items.Count > 0, 0, -1)
        FillDepartments(Nothing)
        FieldLayout.SelectValue(_designation, Nothing)
        FieldLayout.SelectValue(_marital, Nothing)
        _mobile.Clear()
        _email.Clear()
        _nationality.Text = DefaultNationality
        _status.Text = EmployeeStatus.Active.ToString()
        _ids.ShowEmployee(0)
        _bank.ShowEmployee(0)
        _tabs.SelectedIndex = 0
        _name.Focus()
    End Sub

    Private Function BuildDraft() As EmployeeDraft
        Return New EmployeeDraft With {
            .FullName = _name.Text.Trim(),
            .DateOfBirth = _dob.Value.Date,
            .JoinDate = _join.Value.Date,
            .PayType = CType(FieldLayout.IntValue(_payType), PayType),
            .Mobile = _mobile.Text.Trim()}
    End Function

    ''' <summary>Fields the screen does not edit (section, grade, reporting, biometric) keep their saved values.</summary>
    Private Function BuildPlacement() As EmployeePlacement
        Dim placement As New EmployeePlacement With {
            .Gender = Convert.ToString(_gender.SelectedValue),
            .FatherOrSpouseName = _father.Text.Trim(),
            .EmploymentTypeId = FieldLayout.IntValue(_employment),
            .WorkerTypeId = FieldLayout.IntValue(_worker),
            .FactoryId = FieldLayout.IntValue(_factory),
            .DepartmentId = FieldLayout.OptionalValue(_department),
            .DesignationId = FieldLayout.OptionalValue(_designation),
            .MaritalStatusId = FieldLayout.OptionalValue(_marital),
            .Nationality = _nationality.Text.Trim(),
            .Email = _email.Text.Trim()}
        If _detail IsNot Nothing Then
            placement.SectionId = _detail.SectionId
            placement.GradeId = _detail.GradeId
            placement.ReportingToEmployeeId = _detail.ReportingToEmployeeId
            placement.BiometricId = _detail.BiometricId
        End If
        Return placement
    End Function

    Private Sub SaveEmployee()
        If Not _canEdit Then
            ShowError("You are not allowed to change employees.")
            Return
        End If
        ClearValidation()
        If _employeeId = 0 Then
            CreateEmployee()
        Else
            UpdateEmployee()
        End If
    End Sub

    Private Sub CreateEmployee()
        Dim result = _services.Employees.Create(BuildDraft(), BuildPlacement())
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        ReloadList(result.Value.EmployeeId)
        ShowStatus($"Employee {result.Value.EmpCode} saved. Add the IDs and bank on the next tabs.")
        If result.Value.IdWarning.Length > 0 Then ShowError(result.Value.IdWarning)
    End Sub

    Private Sub UpdateEmployee()
        Dim result = _services.Employees.Update(_employeeId, _detail.RowVersion, BuildDraft(), BuildPlacement())
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        ReloadList(_employeeId)
        ShowStatus("Employee saved.")
    End Sub
End Class
