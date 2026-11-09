Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Biometric

''' <summary>
''' Devices, plain version for testing (Brave; Sigma restyles it). The agent reads the devices listed here; people are managed under Biometric users, so a device can be replaced or moved without touching them.
''' Pick a location, give the device a code, a name, the brand (ESSL for the first customer), the address on the LAN and the port (4370 for most eSSL devices), then Save device. Pick a row to change it.
''' A device that is not active is not read. Devices are never deleted: switch one off instead.
''' </summary>
Friend NotInheritable Class BiometricDevicesForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1000
    Private Const FormHeight As Integer = 700
    Private Const DefaultPort As Decimal = 4370D
    Private Const TimeFormat As String = "dd-MM-yyyy HH:mm"

    Private ReadOnly _services As BiometricServices
    Private ReadOnly _location As New ComboBox()
    Private ReadOnly _code As New TextBox()
    Private ReadOnly _name As New TextBox()
    Private ReadOnly _brand As New ComboBox()
    Private ReadOnly _host As New TextBox()
    Private ReadOnly _port As New NumericUpDown()
    Private ReadOnly _active As New CheckBox()
    Private ReadOnly _showInactive As New CheckBox()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _save As New Button()
    Private ReadOnly _new As New Button()
    Private ReadOnly _load As New Button()
    Private ReadOnly _exit As New Button()
    Private _items As IReadOnlyList(Of DeviceItem) = New List(Of DeviceItem)()
    Private _editing As Integer?

    Public Sub New(services As BiometricServices)
        MyBase.New("Biometric devices", "Register the devices the agent reads. People are set under Biometric users.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
        LoadLocations()
        DoLoad()
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid()
        Content.Controls.Add(_grid)

        _location.DropDownStyle = ComboBoxStyle.DropDownList
        _code.MaxLength = 30
        _name.MaxLength = 100
        _brand.DropDownStyle = ComboBoxStyle.DropDown
        _brand.MaxLength = 30
        _brand.Items.Add("ESSL")
        _brand.Text = "ESSL"
        _host.MaxLength = 100
        _port.Minimum = 1D
        _port.Maximum = 65535D
        _port.Value = DefaultPort
        _active.Text = "Active (the agent reads it)"
        _active.Checked = True
        _active.AutoSize = True
        _showInactive.Text = "Show devices that are switched off"
        _showInactive.AutoSize = True
        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Location", _location)
        AddField(layout, "Device code (letters, digits, - and _)", _code)
        AddField(layout, "Device name", _name)
        AddField(layout, "Brand", _brand)
        AddField(layout, "Address on the LAN", _host)
        AddField(layout, "Port", _port)
        AddField(layout, "State", _active)
        AddField(layout, "List", _showInactive)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        _save.Text = "Save device"
        _save.AutoSize = True
        _new.Text = "New device"
        _new.AutoSize = True
        _load.Text = "Load"
        _load.AutoSize = True
        bar.Controls.Add(_exit)
        bar.Controls.Add(_save)
        bar.Controls.Add(_new)
        bar.Controls.Add(_load)
        AddHandler _load.Click, Sub(sender As Object, e As EventArgs) DoLoad()
        AddHandler _new.Click, Sub(sender As Object, e As EventArgs) ClearFields()
        AddHandler _save.Click, Sub(sender As Object, e As EventArgs) DoSave()
        AddHandler _showInactive.CheckedChanged, Sub(sender As Object, e As EventArgs) DoLoad()
        AddHandler _grid.SelectionChanged, Sub(sender As Object, e As EventArgs) ShowPicked()
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
        AddColumn(NameOf(DeviceLine.Code), "Code", 10)
        AddColumn(NameOf(DeviceLine.Name), "Name", 20)
        AddColumn(NameOf(DeviceLine.Location), "Location", 18)
        AddColumn(NameOf(DeviceLine.Brand), "Brand", 9)
        AddColumn(NameOf(DeviceLine.Address), "Address", 16)
        AddColumn(NameOf(DeviceLine.State), "State", 8)
        AddColumn(NameOf(DeviceLine.LastPunch), "Newest punch", 19)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single)
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight, .Name = property_})
    End Sub

    Private Sub LoadLocations()
        _location.Items.Clear()
        If _services.Factories Is Nothing Then
            ShowError("Locations are not available.")
            Return
        End If
        For Each choice As FactoryChoice In _services.Factories.ListActive()
            _location.Items.Add(choice)
        Next
        If _location.Items.Count > 0 Then _location.SelectedIndex = 0
    End Sub

    Private Sub DoLoad()
        ClearValidation()
        Dim result As OperationResult(Of IReadOnlyList(Of DeviceItem)) = _services.Biometric.ListDevices(_showInactive.Checked)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _items = result.Value
        _grid.DataSource = result.Value.Select(Function(d) New DeviceLine(d)).ToList()
        ShowStatus($"{result.Value.Count.ToString(CultureInfo.InvariantCulture)} devices.")
    End Sub

    Private Sub ShowPicked()
        If _grid.CurrentRow Is Nothing OrElse _grid.CurrentRow.Index < 0 OrElse _grid.CurrentRow.Index >= _items.Count Then Return
        Dim device As DeviceItem = _items(_grid.CurrentRow.Index)
        _editing = device.DeviceId
        _code.Text = device.Code
        _name.Text = device.Name
        _brand.Text = device.Brand
        _host.Text = device.Host
        _port.Value = Math.Min(_port.Maximum, Math.Max(_port.Minimum, device.Port))
        _active.Checked = device.IsActive
        For i As Integer = 0 To _location.Items.Count - 1
            If DirectCast(_location.Items(i), FactoryChoice).FactoryId = device.FactoryId Then _location.SelectedIndex = i
        Next
    End Sub

    Private Sub ClearFields()
        ClearValidation()
        _editing = Nothing
        _code.Clear()
        _name.Clear()
        _brand.Text = "ESSL"
        _host.Clear()
        _port.Value = DefaultPort
        _active.Checked = True
        If _location.Items.Count > 0 Then _location.SelectedIndex = 0
        _grid.ClearSelection()
    End Sub

    Private Sub DoSave()
        ClearValidation()
        Dim place As FactoryChoice = TryCast(_location.SelectedItem, FactoryChoice)
        If place Is Nothing Then
            SetFieldError(_location, "Pick a location.")
            ShowError("Pick a location.")
            Return
        End If
        Dim device As New DeviceWrite With {.DeviceId = _editing, .FactoryId = place.FactoryId, .Code = _code.Text, .Name = _name.Text, .Brand = _brand.Text,
                                            .Host = _host.Text, .Port = CInt(_port.Value), .IsActive = _active.Checked}
        Dim result As OperationResult(Of Integer) = _services.Biometric.SaveDevice(device)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        ClearFields()
        DoLoad()
        ShowStatus("The device is saved.")
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        DoLoad()
    End Sub

    Private NotInheritable Class DeviceLine
        Public Sub New(device As DeviceItem)
            Code = device.Code
            Name = device.Name
            Location = device.FactoryName
            Brand = device.Brand
            Address = device.Host & ":" & device.Port.ToString(CultureInfo.InvariantCulture)
            State = If(device.IsActive, "Active", "Off")
            LastPunch = If(device.LastPunchTime.HasValue, device.LastPunchTime.Value.ToString(TimeFormat, CultureInfo.InvariantCulture), String.Empty)
        End Sub

        Public ReadOnly Property Code As String
        Public ReadOnly Property Name As String
        Public ReadOnly Property Location As String
        Public ReadOnly Property Brand As String
        Public ReadOnly Property Address As String
        Public ReadOnly Property State As String
        Public ReadOnly Property LastPunch As String
    End Class
End Class
