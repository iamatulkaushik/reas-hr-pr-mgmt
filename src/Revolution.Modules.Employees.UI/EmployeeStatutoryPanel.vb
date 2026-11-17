Imports System.Windows.Forms
Imports Revolution.Modules.Employees

''' <summary>
''' The Statutory tab: EPF, ESI, professional tax, labour welfare and tax regime flags of one employee. Ids (PF, ESI number, PAN, Aadhaar) are on the IDs tab, not here.
''' The pension and higher wage options need EPF membership, the dispensary and the higher wage ceiling need ESI membership (CR 0030): clearing a membership clears what depends on it,
''' so the screen never offers a combination the service refuses.
''' </summary>
Friend NotInheritable Class EmployeeStatutoryPanel
    Inherits UserControl

    Private Const NewRegimeText As String = "New regime"
    Private Const OldRegimeText As String = "Old regime"

    Private ReadOnly _service As EmployeeStatutoryService
    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _epfMember As New CheckBox() With {.Text = "EPF member", .AutoSize = True}
    Private ReadOnly _pfNumber As New TextBox() With {.MaxLength = 30}
    Private ReadOnly _epfJoined As New DateTimePicker() With {.Format = DateTimePickerFormat.Short, .ShowCheckBox = True, .Checked = False}
    Private ReadOnly _pension As New CheckBox() With {.Text = "Pension (EPS) member", .AutoSize = True}
    Private ReadOnly _higherWage As New CheckBox() With {.Text = "Higher wage option (contribution above the wage ceiling)", .AutoSize = True}
    Private ReadOnly _esiMember As New CheckBox() With {.Text = "ESI member", .AutoSize = True}
    Private ReadOnly _dispensary As New TextBox() With {.MaxLength = 60}
    Private ReadOnly _esiHigher As New CheckBox() With {.Text = "ESI higher wage ceiling applies (person with disability)", .AutoSize = True}
    Private ReadOnly _pt As New CheckBox() With {.Text = "Professional tax applies", .AutoSize = True}
    Private ReadOnly _lwf As New CheckBox() With {.Text = "Labour welfare fund applies", .AutoSize = True}
    Private ReadOnly _regime As New ComboBox()
    Private ReadOnly _international As New CheckBox() With {.Text = "International worker", .AutoSize = True}
    Private ReadOnly _saveButton As New Button() With {.Text = "Save statutory", .AutoSize = True}
    Private _employeeId As Integer
    Private _loading As Boolean

    Public Event Notice(text As String, isError As Boolean)

    Public Sub New(service As EmployeeStatutoryService, canEdit As Boolean)
        If service Is Nothing Then Throw New ArgumentNullException(NameOf(service))
        _service = service
        _canEdit = canEdit
        BuildLayout()
        ShowEmployee(0)
    End Sub

    Private Sub BuildLayout()
        _regime.DropDownStyle = ComboBoxStyle.DropDownList
        _regime.Items.Add(NewRegimeText)
        _regime.Items.Add(OldRegimeText)
        Dim layout = FieldLayout.Create()
        layout.Padding = New Padding(Theme.Space2)
        FieldLayout.Add(layout, " ", _epfMember)
        FieldLayout.Add(layout, "PF number", _pfNumber)
        FieldLayout.Add(layout, "EPF joining date (leave unticked if not known)", _epfJoined)
        FieldLayout.Add(layout, " ", _pension)
        FieldLayout.Add(layout, " ", _higherWage)
        FieldLayout.Add(layout, " ", _esiMember)
        FieldLayout.Add(layout, "ESI dispensary", _dispensary)
        FieldLayout.Add(layout, " ", _esiHigher)
        FieldLayout.Add(layout, " ", _pt)
        FieldLayout.Add(layout, " ", _lwf)
        FieldLayout.Add(layout, "Tax regime", _regime)
        FieldLayout.Add(layout, " ", _international)
        Dim bar As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .Padding = New Padding(Theme.Space2)}
        bar.Controls.Add(_saveButton)
        AddHandler _saveButton.Click, Sub(sender As Object, e As EventArgs) SaveFlags()
        AddHandler _epfMember.CheckedChanged, Sub(sender As Object, e As EventArgs) OnMembershipChanged()
        AddHandler _esiMember.CheckedChanged, Sub(sender As Object, e As EventArgs) OnMembershipChanged()
        AutoScroll = True
        Controls.Add(bar)
        Controls.Add(layout)
    End Sub

    ''' <summary>What depends on a membership is available only while the membership is ticked, and is cleared when it is not.</summary>
    Private Sub OnMembershipChanged()
        Dim epf As Boolean = _epfMember.Checked
        Dim esi As Boolean = _esiMember.Checked
        If Not _loading Then
            If Not epf Then
                _higherWage.Checked = False
            End If
            If Not esi Then
                _esiHigher.Checked = False
            End If
        End If
        Dim editable As Boolean = _canEdit AndAlso _employeeId > 0
        _pfNumber.Enabled = editable AndAlso epf
        _epfJoined.Enabled = editable AndAlso epf
        _pension.Enabled = editable AndAlso epf
        _higherWage.Enabled = editable AndAlso epf
        _dispensary.Enabled = editable AndAlso esi
        _esiHigher.Enabled = editable AndAlso esi
    End Sub

    Public Sub ShowEmployee(employeeId As Integer)
        _employeeId = employeeId
        Enabled = employeeId > 0
        Dim flags As New StatutoryFlags()
        If employeeId > 0 Then
            Dim result = _service.GetFlags(employeeId)
            If Not result.Succeeded Then
                RaiseEvent Notice(result.Message, True)
                Return
            End If
            flags = result.Value
        End If
        _loading = True
        Try
            _epfMember.Checked = flags.IsEpfMember
            _pfNumber.Text = flags.PfNumber
            If flags.EpfJoinDate.HasValue Then
                _epfJoined.Value = flags.EpfJoinDate.Value
                _epfJoined.Checked = True
            Else
                _epfJoined.Checked = False
            End If
            _pension.Checked = flags.IsPensionMember
            _higherWage.Checked = flags.HigherWageOption
            _esiMember.Checked = flags.IsEsiMember
            _dispensary.Text = flags.EsiDispensary
            _esiHigher.Checked = flags.EsiHigherCeiling
            _pt.Checked = flags.PtApplicable
            _lwf.Checked = flags.LwfApplicable
            _regime.SelectedIndex = If(flags.TaxRegimeId = StatutoryFlags.OldTaxRegime, 1, 0)
            _international.Checked = flags.IsInternationalWorker
        Finally
            _loading = False
        End Try
        OnMembershipChanged()
        _pt.Enabled = _canEdit AndAlso employeeId > 0
        _lwf.Enabled = _canEdit AndAlso employeeId > 0
        _epfMember.Enabled = _canEdit AndAlso employeeId > 0
        _esiMember.Enabled = _canEdit AndAlso employeeId > 0
        _regime.Enabled = _canEdit AndAlso employeeId > 0
        _international.Enabled = _canEdit AndAlso employeeId > 0
        _saveButton.Enabled = _canEdit AndAlso employeeId > 0
    End Sub

    Private Sub SaveFlags()
        If _employeeId <= 0 Then
            Return
        End If
        Dim flags As New StatutoryFlags With {
            .IsEpfMember = _epfMember.Checked,
            .PfNumber = _pfNumber.Text.Trim(),
            .EpfJoinDate = If(_epfJoined.Checked, CType(_epfJoined.Value.Date, Date?), Nothing),
            .IsPensionMember = _pension.Checked,
            .HigherWageOption = _higherWage.Checked,
            .IsEsiMember = _esiMember.Checked,
            .EsiDispensary = _dispensary.Text.Trim(),
            .EsiHigherCeiling = _esiHigher.Checked,
            .PtApplicable = _pt.Checked,
            .LwfApplicable = _lwf.Checked,
            .TaxRegimeId = If(_regime.SelectedIndex = 1, StatutoryFlags.OldTaxRegime, StatutoryFlags.NewTaxRegime),
            .IsInternationalWorker = _international.Checked}
        Dim result = _service.SaveFlags(_employeeId, flags)
        If Not result.Succeeded Then
            RaiseEvent Notice(result.Message, True)
            Return
        End If
        ShowEmployee(_employeeId)
        RaiseEvent Notice("Statutory settings saved.", False)
    End Sub
End Class
