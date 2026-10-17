Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Employees

''' <summary>Factory master: address, state, licence, worker limit and the EPF / ESI sub codes.</summary>
Friend NotInheritable Class FactoryForm
    Inherits MasterFormBase

    Private Const DateFormat As String = "dd-MM-yyyy"

    Private ReadOnly _services As EmployeesServices
    Private ReadOnly _items As New Dictionary(Of Integer, FactoryItem)()
    Private ReadOnly _stateNames As New Dictionary(Of Integer, String)()
    Private ReadOnly _code As New TextBox() With {.MaxLength = OrgMasterValidator.MaxCodeLength}
    Private ReadOnly _name As New TextBox() With {.MaxLength = OrgMasterValidator.MaxFactoryName}
    Private ReadOnly _state As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList}
    Private ReadOnly _address1 As New TextBox() With {.MaxLength = 150}
    Private ReadOnly _address2 As New TextBox() With {.MaxLength = 150}
    Private ReadOnly _city As New TextBox() With {.MaxLength = 100}
    Private ReadOnly _licence As New TextBox() With {.MaxLength = 50}
    Private ReadOnly _licenceTo As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = DateFormat, .ShowCheckBox = True}
    Private ReadOnly _maxWorkers As New TextBox() With {.MaxLength = 6}
    Private ReadOnly _manager As New TextBox() With {.MaxLength = 120}
    Private ReadOnly _epfSub As New TextBox() With {.MaxLength = 20}
    Private ReadOnly _esiSub As New TextBox() With {.MaxLength = 17}
    Private ReadOnly _active As New CheckBox() With {.Text = "Active", .AutoSize = True}
    Private _currentId As Integer

    Public Sub New(services As EmployeesServices, canEdit As Boolean)
        MyBase.New("Factory", "Masters", New String() {"Code", "Factory", "State", "Status"}, canEdit)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
    End Sub

    Protected Overrides Sub BuildEditor(layout As TableLayoutPanel)
        AddField(layout, "Code", _code)
        AddField(layout, "Factory name", _name)
        AddField(layout, "State", _state)
        AddField(layout, "Address line 1", _address1)
        AddField(layout, "Address line 2", _address2)
        AddField(layout, "City", _city)
        AddField(layout, "Factory licence number", _licence)
        AddField(layout, "Licence valid to", _licenceTo)
        AddField(layout, "Maximum workers", _maxWorkers)
        AddField(layout, "Manager name", _manager)
        AddField(layout, "EPF sub code", _epfSub)
        AddField(layout, "ESI sub code", _esiSub)
        AddField(layout, "Status", _active)
        LoadStates()
        ClearEditor()
    End Sub

    Private Sub LoadStates()
        Dim result = _services.Factories.ListStates()
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _stateNames.Clear()
        For Each state In result.Value
            _stateNames(state.StateId) = state.Name
        Next
        _state.DataSource = result.Value.ToList()
        _state.DisplayMember = "Name"
        _state.ValueMember = "StateId"
    End Sub

    Protected Overrides Function LoadRows(includeInactive As Boolean) As OperationResult(Of IReadOnlyList(Of MasterRow))
        Dim result = _services.Factories.List(Not includeInactive)
        If Not result.Succeeded Then Return OperationResult(Of IReadOnlyList(Of MasterRow)).Fail(result.Message)
        _items.Clear()
        Dim rows As New List(Of MasterRow)()
        For Each item In result.Value
            _items(item.FactoryId) = item
            Dim stateName As String = String.Empty
            _stateNames.TryGetValue(item.StateId, stateName)
            rows.Add(New MasterRow(item.FactoryId,
                                   New String() {item.Code, item.Name, stateName, If(item.IsActive, "Active", "Inactive")},
                                   item.IsActive))
        Next
        Return OperationResult(Of IReadOnlyList(Of MasterRow)).Ok(rows)
    End Function

    Protected Overrides Sub ShowRow(id As Integer)
        Dim item As FactoryItem = Nothing
        If Not _items.TryGetValue(id, item) Then
            ClearEditor()
            Return
        End If
        _currentId = item.FactoryId
        _code.Text = item.Code
        _name.Text = item.Name
        _state.SelectedValue = item.StateId
        _address1.Text = item.AddressLine1
        _address2.Text = item.AddressLine2
        _city.Text = item.City
        _licence.Text = item.LicenceNo
        ShowLicenceDate(item.LicenceValidTo)
        _maxWorkers.Text = If(item.MaxWorkers.HasValue, item.MaxWorkers.Value.ToString(CultureInfo.InvariantCulture), String.Empty)
        _manager.Text = item.ManagerName
        _epfSub.Text = item.EpfSubCode
        _esiSub.Text = item.EsiSubCode
        _active.Checked = item.IsActive
    End Sub

    Private Sub ShowLicenceDate(value As Date?)
        _licenceTo.Checked = value.HasValue
        If value.HasValue Then _licenceTo.Value = value.Value
    End Sub

    Protected Overrides Sub ClearEditor()
        _currentId = 0
        _code.Clear()
        _name.Clear()
        _state.SelectedIndex = -1
        _address1.Clear()
        _address2.Clear()
        _city.Clear()
        _licence.Clear()
        _licenceTo.Checked = False
        _maxWorkers.Clear()
        _manager.Clear()
        _epfSub.Clear()
        _esiSub.Clear()
        _active.Checked = True
        _code.Focus()
    End Sub

    Protected Overrides Function SaveEditor() As OperationResult(Of Integer)
        Dim maxWorkers As Integer? = Nothing
        Dim typed As String = _maxWorkers.Text.Trim()
        If typed.Length > 0 Then
            Dim parsed As Integer
            If Not Integer.TryParse(typed, NumberStyles.None, CultureInfo.InvariantCulture, parsed) Then
                Return OperationResult(Of Integer).Fail("Maximum workers must be a whole number.")
            End If
            maxWorkers = parsed
        End If
        Dim item As New FactoryItem With {
            .FactoryId = _currentId,
            .Code = _code.Text,
            .Name = _name.Text,
            .StateId = If(_state.SelectedValue Is Nothing, 0, Convert.ToInt32(_state.SelectedValue)),
            .AddressLine1 = _address1.Text,
            .AddressLine2 = _address2.Text,
            .City = _city.Text,
            .LicenceNo = _licence.Text,
            .LicenceValidTo = If(_licenceTo.Checked, CType(_licenceTo.Value.Date, Date?), Nothing),
            .MaxWorkers = maxWorkers,
            .ManagerName = _manager.Text,
            .EpfSubCode = _epfSub.Text,
            .EsiSubCode = _esiSub.Text,
            .IsActive = _active.Checked}
        Return _services.Factories.Save(item)
    End Function
End Class
