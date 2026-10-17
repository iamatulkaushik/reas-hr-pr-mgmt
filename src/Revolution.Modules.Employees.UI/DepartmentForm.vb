Imports System.Windows.Forms
Imports Revolution.Modules.Employees

''' <summary>Department master. A department belongs to one factory; the factory cannot be changed once saved.</summary>
Friend NotInheritable Class DepartmentForm
    Inherits MasterFormBase

    Private ReadOnly _services As EmployeesServices
    Private ReadOnly _items As New Dictionary(Of Integer, DepartmentItem)()
    Private ReadOnly _factoryNames As New Dictionary(Of Integer, String)()
    Private ReadOnly _factory As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList}
    Private ReadOnly _code As New TextBox() With {.MaxLength = OrgMasterValidator.MaxCodeLength}
    Private ReadOnly _name As New TextBox() With {.MaxLength = OrgMasterValidator.MaxShortName}
    Private ReadOnly _hazardous As New CheckBox() With {.Text = "Hazardous process", .AutoSize = True}
    Private ReadOnly _active As New CheckBox() With {.Text = "Active", .AutoSize = True}
    Private _currentId As Integer

    Public Sub New(services As EmployeesServices, canEdit As Boolean)
        MyBase.New("Department", "Masters", New String() {"Code", "Department", "Factory", "Status"}, canEdit)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
    End Sub

    Protected Overrides Sub BuildEditor(layout As TableLayoutPanel)
        AddField(layout, "Factory", _factory)
        AddField(layout, "Code", _code)
        AddField(layout, "Department name", _name)
        AddField(layout, "Hazardous", _hazardous)
        AddField(layout, "Status", _active)
        LoadFactories()
        ClearEditor()
    End Sub

    Private Sub LoadFactories()
        Dim result = _services.Factories.List(True)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _factoryNames.Clear()
        For Each factory In result.Value
            _factoryNames(factory.FactoryId) = factory.Name
        Next
        _factory.DataSource = result.Value.ToList()
        _factory.DisplayMember = "Name"
        _factory.ValueMember = "FactoryId"
        If result.Value.Count = 0 Then ShowError("Create a factory first (Masters > Factory).")
    End Sub

    Protected Overrides Function LoadRows(includeInactive As Boolean) As OperationResult(Of IReadOnlyList(Of MasterRow))
        Dim result = _services.Departments.List(Nothing, Not includeInactive)
        If Not result.Succeeded Then Return OperationResult(Of IReadOnlyList(Of MasterRow)).Fail(result.Message)
        _items.Clear()
        Dim rows As New List(Of MasterRow)()
        For Each item In result.Value
            _items(item.DepartmentId) = item
            Dim factoryName As String = String.Empty
            _factoryNames.TryGetValue(item.FactoryId, factoryName)
            rows.Add(New MasterRow(item.DepartmentId,
                                   New String() {item.Code, item.Name, factoryName, If(item.IsActive, "Active", "Inactive")},
                                   item.IsActive))
        Next
        Return OperationResult(Of IReadOnlyList(Of MasterRow)).Ok(rows)
    End Function

    Protected Overrides Sub ShowRow(id As Integer)
        Dim item As DepartmentItem = Nothing
        If Not _items.TryGetValue(id, item) Then
            ClearEditor()
            Return
        End If
        _currentId = item.DepartmentId
        _factory.SelectedValue = item.FactoryId
        _factory.Enabled = False
        _code.Text = item.Code
        _name.Text = item.Name
        _hazardous.Checked = item.IsHazardous
        _active.Checked = item.IsActive
    End Sub

    Protected Overrides Sub ClearEditor()
        _currentId = 0
        _factory.Enabled = True
        _factory.SelectedIndex = If(_factory.Items.Count > 0, 0, -1)
        _code.Clear()
        _name.Clear()
        _hazardous.Checked = False
        _active.Checked = True
        _code.Focus()
    End Sub

    Protected Overrides Function SaveEditor() As OperationResult(Of Integer)
        Dim factoryId As Integer = If(_factory.SelectedValue Is Nothing, 0, Convert.ToInt32(_factory.SelectedValue))
        Dim item As New DepartmentItem With {
            .DepartmentId = _currentId,
            .FactoryId = factoryId,
            .Code = _code.Text,
            .Name = _name.Text,
            .IsHazardous = _hazardous.Checked,
            .IsActive = _active.Checked}
        Return _services.Departments.Save(item)
    End Function
End Class
