Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Biometric

''' <summary>
''' Agent keys, plain version for testing (Brave; Sigma restyles it). Each punch agent (one computer on a factory LAN) has its own key. Give the agent a label, usually the computer name, and press Issue key:
''' the key is shown once. Copy it into the agent's settings with RevolutionAgent protect on that computer, then close the box. It cannot be read again; a lost key is retired and a new one issued.
''' Only the Owner or the Associate of the company can do this. Retire stops an agent at once.
''' </summary>
Friend NotInheritable Class AgentKeysForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 900
    Private Const FormHeight As Integer = 640
    Private Const TimeFormat As String = "dd-MM-yyyy HH:mm"

    Private ReadOnly _services As BiometricServices
    Private ReadOnly _label As New TextBox()
    Private ReadOnly _key As New TextBox()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _issue As New Button()
    Private ReadOnly _copy As New Button()
    Private ReadOnly _retire As New Button()
    Private ReadOnly _load As New Button()
    Private ReadOnly _exit As New Button()
    Private _items As IReadOnlyList(Of AgentKeyItem) = New List(Of AgentKeyItem)()

    Public Sub New(services As BiometricServices)
        MyBase.New("Agent keys", "Issue a key for each punch agent. The key is shown once.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
        DoLoad()
    End Sub

    Private Sub BuildLayout()
        ConfigureGrid()
        Content.Controls.Add(_grid)

        _label.MaxLength = AgentKeyService.MaxLabelLength
        _key.ReadOnly = True
        _key.Font = New Font(FontFamily.GenericMonospace, 10.0F)
        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Agent label (for example the computer name)", _label)
        AddField(layout, "New key (shown once, copy it now)", _key)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        For Each pair As Tuple(Of Button, String) In New List(Of Tuple(Of Button, String)) From {
            Tuple.Create(_retire, "Retire picked agent"), Tuple.Create(_copy, "Copy key"), Tuple.Create(_issue, "Issue key"), Tuple.Create(_load, "Load")}
            pair.Item1.Text = pair.Item2
            pair.Item1.AutoSize = True
        Next
        _copy.Enabled = False
        bar.Controls.Add(_exit)
        bar.Controls.Add(_retire)
        bar.Controls.Add(_copy)
        bar.Controls.Add(_issue)
        bar.Controls.Add(_load)
        AddHandler _load.Click, Sub(sender As Object, e As EventArgs) DoLoad()
        AddHandler _issue.Click, Sub(sender As Object, e As EventArgs) DoIssue()
        AddHandler _copy.Click, Sub(sender As Object, e As EventArgs) DoCopy()
        AddHandler _retire.Click, Sub(sender As Object, e As EventArgs) DoRetire()
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
        AddColumn(NameOf(KeyLine.Label), "Agent", 30)
        AddColumn(NameOf(KeyLine.State), "State", 12)
        AddColumn(NameOf(KeyLine.Created), "Issued (UTC)", 20)
        AddColumn(NameOf(KeyLine.LastSeen), "Last seen (UTC)", 20)
        AddColumn(NameOf(KeyLine.Retired), "Retired (UTC)", 18)
    End Sub

    Private Sub AddColumn(property_ As String, header As String, weight As Single)
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight, .Name = property_})
    End Sub

    Private Function AgentKeys() As AgentKeyService
        If _services.AgentKeys Is Nothing Then Throw New InvalidOperationException("Agent keys are not available.")
        Return _services.AgentKeys
    End Function

    Private Sub DoLoad()
        ClearValidation()
        Dim result As OperationResult(Of IReadOnlyList(Of AgentKeyItem)) = AgentKeys().List()
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _items = result.Value
        _grid.DataSource = result.Value.Select(Function(k) New KeyLine(k)).ToList()
        ShowStatus($"{result.Value.Count.ToString(CultureInfo.InvariantCulture)} agents.")
    End Sub

    Private Sub DoIssue()
        ClearValidation()
        _key.Clear()
        _copy.Enabled = False
        Dim result As OperationResult(Of IssuedAgentKey) = AgentKeys().Issue(_label.Text)
        If Not result.Succeeded Then
            SetFieldError(_label, result.Message)
            ShowError(result.Message)
            Return
        End If
        _key.Text = result.Value.Key
        _copy.Enabled = True
        _label.Clear()
        DoLoad()
        ShowStatus("The key is issued. Copy it now: it is not shown again.")
    End Sub

    Private Sub DoCopy()
        If _key.TextLength = 0 Then Return
        Clipboard.SetText(_key.Text)
        ShowStatus("The key is copied to the clipboard. Clear the clipboard after you have used it.")
    End Sub

    Private Sub DoRetire()
        ClearValidation()
        If _grid.CurrentRow Is Nothing OrElse _grid.CurrentRow.Index < 0 OrElse _grid.CurrentRow.Index >= _items.Count Then
            ShowError("Pick an agent in the list first.")
            Return
        End If
        Dim picked As AgentKeyItem = _items(_grid.CurrentRow.Index)
        Dim answer As DialogResult = MessageBox.Show(Me, "Retire this agent? It stops at once and cannot be started again with the same key.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
        If answer <> DialogResult.Yes Then Return
        Dim result As OperationResult = AgentKeys().Retire(picked.RegistrationId)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        DoLoad()
        ShowStatus("The agent is retired.")
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        _key.Clear()
        MyBase.OnFormClosed(e)
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        DoLoad()
    End Sub

    Private NotInheritable Class KeyLine
        Public Sub New(item As AgentKeyItem)
            Label = item.Label
            State = If(item.IsActive, "Active", "Retired")
            Created = item.CreatedUtc.ToString(TimeFormat, CultureInfo.InvariantCulture)
            LastSeen = If(item.LastSeenUtc.HasValue, item.LastSeenUtc.Value.ToString(TimeFormat, CultureInfo.InvariantCulture), "never")
            Retired = If(item.RetiredUtc.HasValue, item.RetiredUtc.Value.ToString(TimeFormat, CultureInfo.InvariantCulture), String.Empty)
        End Sub

        Public ReadOnly Property Label As String
        Public ReadOnly Property State As String
        Public ReadOnly Property Created As String
        Public ReadOnly Property LastSeen As String
        Public ReadOnly Property Retired As String
    End Class
End Class
