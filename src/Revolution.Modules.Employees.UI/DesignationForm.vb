Imports System.Windows.Forms
Imports Revolution.Modules.Employees

''' <summary>Designation master: code, name, skill category and active flag.</summary>
Friend NotInheritable Class DesignationForm
    Inherits MasterFormBase

    Private ReadOnly _services As EmployeesServices
    Private ReadOnly _items As New Dictionary(Of Integer, DesignationItem)()
    Private ReadOnly _code As New TextBox() With {.MaxLength = OrgMasterValidator.MaxCodeLength}
    Private ReadOnly _name As New TextBox() With {.MaxLength = OrgMasterValidator.MaxShortName}
    Private ReadOnly _category As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList}
    Private ReadOnly _active As New CheckBox() With {.Text = "Active", .AutoSize = True}
    Private _currentId As Integer
    Private _gradeId As Integer?

    Public Sub New(services As EmployeesServices, canEdit As Boolean)
        MyBase.New("Designation", "Masters", New String() {"Code", "Designation", "Category", "Status"}, canEdit)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
    End Sub

    Protected Overrides Sub BuildEditor(layout As TableLayoutPanel)
        _category.DataSource = DesignationCategories.All.ToList()
        _category.DisplayMember = "Value"
        _category.ValueMember = "Key"
        AddField(layout, "Code", _code)
        AddField(layout, "Designation name", _name)
        AddField(layout, "Skill category", _category)
        AddField(layout, "Status", _active)
        ClearEditor()
    End Sub

    Protected Overrides Function LoadRows(includeInactive As Boolean) As OperationResult(Of IReadOnlyList(Of MasterRow))
        Dim result = _services.Designations.List(Not includeInactive)
        If Not result.Succeeded Then Return OperationResult(Of IReadOnlyList(Of MasterRow)).Fail(result.Message)
        _items.Clear()
        Dim rows As New List(Of MasterRow)()
        For Each item In result.Value
            _items(item.DesignationId) = item
            rows.Add(New MasterRow(item.DesignationId,
                                   New String() {item.Code, item.Name, DesignationCategories.DisplayName(item.CategoryId), If(item.IsActive, "Active", "Inactive")},
                                   item.IsActive))
        Next
        Return OperationResult(Of IReadOnlyList(Of MasterRow)).Ok(rows)
    End Function

    Protected Overrides Sub ShowRow(id As Integer)
        Dim item As DesignationItem = Nothing
        If Not _items.TryGetValue(id, item) Then
            ClearEditor()
            Return
        End If
        _currentId = item.DesignationId
        _gradeId = item.GradeId
        _code.Text = item.Code
        _name.Text = item.Name
        _category.SelectedValue = item.CategoryId
        _active.Checked = item.IsActive
    End Sub

    Protected Overrides Sub ClearEditor()
        _currentId = 0
        _gradeId = Nothing
        _code.Clear()
        _name.Clear()
        _category.SelectedIndex = -1
        _active.Checked = True
        _code.Focus()
    End Sub

    Protected Overrides Function SaveEditor() As OperationResult(Of Integer)
        Dim category As Integer = If(_category.SelectedValue Is Nothing, 0, Convert.ToInt32(_category.SelectedValue))
        Dim item As New DesignationItem With {
            .DesignationId = _currentId,
            .Code = _code.Text,
            .Name = _name.Text,
            .GradeId = _gradeId,
            .CategoryId = category,
            .IsActive = _active.Checked}
        Return _services.Designations.Save(item)
    End Function
End Class
