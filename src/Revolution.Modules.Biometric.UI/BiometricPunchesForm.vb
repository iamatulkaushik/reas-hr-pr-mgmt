Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Biometric

''' <summary>
''' Stored punches, plain version for testing (Brave; Sigma restyles it). Pick the first and last date (at most 31 days), optionally one employee id (0 for everyone) or only the punches nobody is matched to,
''' and Load. Opened from Biometric users. Times are the device's own clock. At most 5,000 punches are shown: narrow the dates or pick an employee when there are more.
''' </summary>
Friend NotInheritable Class BiometricPunchesForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1100
    Private Const FormHeight As Integer = 700
    Private Const TimeFormat As String = "dd-MM-yyyy HH:mm:ss"

    Private ReadOnly _services As BiometricServices
    Private ReadOnly _from As New DateTimePicker()
    Private ReadOnly _to As New DateTimePicker()
    Private ReadOnly _employeeId As New NumericUpDown()
    Private ReadOnly _unmatchedOnly As New CheckBox()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _load As New Button()
    Private ReadOnly _exit As New Button()

    Public Sub New(services As BiometricServices)
        MyBase.New("Punches", "Stored biometric and card punches, newest data first by time.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid()
        Content.Controls.Add(_grid)

        _from.Format = DateTimePickerFormat.Short
        _to.Format = DateTimePickerFormat.Short
        _from.Value = Date.Today
        _to.Value = Date.Today
        _employeeId.Minimum = 0D
        _employeeId.Maximum = Integer.MaxValue
        _unmatchedOnly.Text = "Only punches matched to nobody"
        _unmatchedOnly.AutoSize = True
        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "First date", _from)
        AddField(layout, "Last date", _to)
        AddField(layout, "Employee id (0 for everyone)", _employeeId)
        AddField(layout, "Which", _unmatchedOnly)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        _load.Text = "Load"
        _load.AutoSize = True
        bar.Controls.Add(_exit)
        bar.Controls.Add(_load)
        AddHandler _load.Click, Sub(sender As Object, e As EventArgs) DoLoad()
    End Sub

    Private Sub ConfigureGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoGenerateColumns = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn(NameOf(PunchLine.PunchTime), "Time", 16)
        AddColumn(NameOf(PunchLine.Device), "Device", 10)
        AddColumn(NameOf(PunchLine.Employee), "Employee", 24)
        AddColumn(NameOf(PunchLine.DeviceUserId), "User id", 9)
        AddColumn(NameOf(PunchLine.CardNo), "Card", 14)
        AddColumn(NameOf(PunchLine.VerifyMode), "Verify", 8)
        AddColumn(NameOf(PunchLine.DeviceStatus), "Device status", 10)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single)
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight, .Name = property_})
    End Sub

    Private Sub DoLoad()
        ClearValidation()
        Dim filter As New PunchFilter With {
            .FromDate = _from.Value.Date, .ToDate = _to.Value.Date, .UnmatchedOnly = _unmatchedOnly.Checked,
            .EmployeeId = If(_employeeId.Value > 0D, CType(CInt(_employeeId.Value), Integer?), Nothing)}
        Dim result As OperationResult(Of IReadOnlyList(Of PunchRow)) = _services.Biometric.ListPunches(filter)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _grid.DataSource = result.Value.Select(Function(p) New PunchLine(p)).ToList()
        ShowStatus($"{result.Value.Count.ToString(CultureInfo.InvariantCulture)} punches.")
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        DoLoad()
    End Sub

    Private NotInheritable Class PunchLine
        Public Sub New(row As PunchRow)
            PunchTime = row.PunchTime.ToString(TimeFormat, CultureInfo.InvariantCulture)
            Device = row.DeviceCode
            Employee = If(row.IsMatched, row.EmpCode & " " & row.FullName, "(not matched)")
            DeviceUserId = If(row.DeviceUserId.HasValue, row.DeviceUserId.Value.ToString(CultureInfo.InvariantCulture), String.Empty)
            CardNo = If(row.CardNo, String.Empty)
            VerifyMode = If(row.VerifyMode.HasValue, row.VerifyMode.Value.ToString(CultureInfo.InvariantCulture), String.Empty)
            DeviceStatus = If(row.DeviceStatus.HasValue, row.DeviceStatus.Value.ToString(CultureInfo.InvariantCulture), String.Empty)
        End Sub

        Public ReadOnly Property PunchTime As String
        Public ReadOnly Property Device As String
        Public ReadOnly Property Employee As String
        Public ReadOnly Property DeviceUserId As String
        Public ReadOnly Property CardNo As String
        Public ReadOnly Property VerifyMode As String
        Public ReadOnly Property DeviceStatus As String
    End Class
End Class
