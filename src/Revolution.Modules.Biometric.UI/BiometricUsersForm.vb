Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Biometric

''' <summary>
''' Biometric users, plain version for testing (Brave; Sigma restyles it). People are managed here, not devices (Owner, 2026-11-06): each employee has one biometric user id, the same on every
''' device, and optionally one RFID card. Any device of any brand that reports the id or the card is matched to the employee. Pick an employee and set the id (Next free id suggests the next one
''' to enrol on a device), map a card, or clear them. The lower list shows ids and cards that devices reported and that match nobody: pick one of those and an employee and press Link, then the
''' waiting punches are matched. Resolve punches matches them again after any change.
''' </summary>
Friend NotInheritable Class BiometricUsersForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 1100
    Private Const FormHeight As Integer = 760
    Private Const UnknownHeight As Integer = 190
    Private Const TimeFormat As String = "dd-MM-yyyy HH:mm"

    Private ReadOnly _services As BiometricServices
    Private ReadOnly _biometricId As New NumericUpDown()
    Private ReadOnly _cardNo As New TextBox()
    Private ReadOnly _onlyNotEnrolled As New CheckBox()
    Private ReadOnly _users As New DataGridView()
    Private ReadOnly _unknown As New DataGridView()
    Private ReadOnly _load As New Button()
    Private ReadOnly _next As New Button()
    Private ReadOnly _setId As New Button()
    Private ReadOnly _clearId As New Button()
    Private ReadOnly _mapCard As New Button()
    Private ReadOnly _retireCard As New Button()
    Private ReadOnly _link As New Button()
    Private ReadOnly _resolve As New Button()
    Private ReadOnly _punches As New Button()
    Private ReadOnly _exit As New Button()
    Private _unknownItems As IReadOnlyList(Of UnknownBiometricUser) = New List(Of UnknownBiometricUser)()

    Public Sub New(services As BiometricServices)
        MyBase.New("Biometric users", "Set each person's biometric id and card once. Any device that reports them is matched.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ConfigureUsersGrid()
        ConfigureUnknownGrid()
        Content.Controls.Add(_users)
        _unknown.Height = UnknownHeight
        _unknown.Dock = DockStyle.Bottom
        Content.Controls.Add(_unknown)

        _biometricId.Minimum = 1D
        _biometricId.Maximum = Integer.MaxValue
        _cardNo.MaxLength = 30
        _onlyNotEnrolled.Text = "Only people with no id and no card"
        _onlyNotEnrolled.AutoSize = True
        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Biometric id", _biometricId)
        AddField(layout, "RFID card number", _cardNo)
        AddField(layout, "List", _onlyNotEnrolled)

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        CancelButton = _exit
        For Each pair As Tuple(Of Button, String) In New List(Of Tuple(Of Button, String)) From {
            Tuple.Create(_punches, "Punches"), Tuple.Create(_resolve, "Resolve punches"), Tuple.Create(_link, "Link unknown to employee"), Tuple.Create(_retireCard, "Retire card"),
            Tuple.Create(_mapCard, "Map card"), Tuple.Create(_clearId, "Clear id"), Tuple.Create(_setId, "Set id"), Tuple.Create(_next, "Next free id"), Tuple.Create(_load, "Load")}
            pair.Item1.Text = pair.Item2
            pair.Item1.AutoSize = True
        Next
        bar.Controls.Add(_exit)
        bar.Controls.Add(_punches)
        bar.Controls.Add(_resolve)
        bar.Controls.Add(_link)
        bar.Controls.Add(_retireCard)
        bar.Controls.Add(_mapCard)
        bar.Controls.Add(_clearId)
        bar.Controls.Add(_setId)
        bar.Controls.Add(_next)
        bar.Controls.Add(_load)
        AddHandler _load.Click, Sub(sender As Object, e As EventArgs) DoLoad()
        AddHandler _next.Click, Sub(sender As Object, e As EventArgs) DoNext()
        AddHandler _setId.Click, Sub(sender As Object, e As EventArgs) DoSetId()
        AddHandler _clearId.Click, Sub(sender As Object, e As EventArgs) DoClearId()
        AddHandler _mapCard.Click, Sub(sender As Object, e As EventArgs) DoMapCard()
        AddHandler _retireCard.Click, Sub(sender As Object, e As EventArgs) DoRetireCard()
        AddHandler _link.Click, Sub(sender As Object, e As EventArgs) DoLink()
        AddHandler _resolve.Click, Sub(sender As Object, e As EventArgs) DoResolve()
        AddHandler _punches.Click, Sub(sender As Object, e As EventArgs) DoPunches()
        AddHandler _onlyNotEnrolled.CheckedChanged, Sub(sender As Object, e As EventArgs) DoLoad()
    End Sub

    Private Shared Sub ConfigureGrid(grid As DataGridView)
        grid.ReadOnly = True
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.RowHeadersVisible = False
        grid.AutoGenerateColumns = False
        grid.MultiSelect = False
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    End Sub

    Private Shared Sub AddColumn(grid As DataGridView, property_ As String, header As String, weight As Single)
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.DataPropertyName = property_, .HeaderText = header, .FillWeight = weight, .Name = property_})
    End Sub

    Private Sub ConfigureUsersGrid()
        _users.Dock = DockStyle.Fill
        ConfigureGrid(_users)
        AddColumn(_users, NameOf(UserLine.EmpCode), "Code", 10)
        AddColumn(_users, NameOf(UserLine.FullName), "Name", 26)
        AddColumn(_users, NameOf(UserLine.BiometricId), "Biometric id", 12)
        AddColumn(_users, NameOf(UserLine.CardNo), "RFID card", 16)
        AddColumn(_users, NameOf(UserLine.LastPunch), "Last punch", 20)
        AddColumn(_users, NameOf(UserLine.Punches), "Punches (30 days)", 16)
    End Sub

    Private Sub ConfigureUnknownGrid()
        ConfigureGrid(_unknown)
        AddColumn(_unknown, NameOf(UnknownLine.DeviceUserId), "Unknown id", 14)
        AddColumn(_unknown, NameOf(UnknownLine.CardNo), "Unknown card", 20)
        AddColumn(_unknown, NameOf(UnknownLine.Punches), "Punches", 12)
        AddColumn(_unknown, NameOf(UnknownLine.FirstSeen), "First seen", 20)
        AddColumn(_unknown, NameOf(UnknownLine.LastSeen), "Last seen", 20)
        AddColumn(_unknown, NameOf(UnknownLine.Devices), "Devices", 14)
    End Sub

    Private Function PickedUser() As UserLine
        Return TryCast(If(_users.CurrentRow Is Nothing, Nothing, _users.CurrentRow.DataBoundItem), UserLine)
    End Function

    Private Function PickedUnknown() As UnknownLine
        Return TryCast(If(_unknown.CurrentRow Is Nothing, Nothing, _unknown.CurrentRow.DataBoundItem), UnknownLine)
    End Function

    Private Sub DoLoad()
        ClearValidation()
        Dim users As OperationResult(Of IReadOnlyList(Of BiometricUserItem)) = _services.Biometric.ListUsers(_onlyNotEnrolled.Checked)
        If Not users.Succeeded Then
            ShowError(users.Message)
            Return
        End If
        Dim unknown As OperationResult(Of IReadOnlyList(Of UnknownBiometricUser)) = _services.Biometric.ListUnknownUsers()
        If Not unknown.Succeeded Then
            ShowError(unknown.Message)
            Return
        End If
        _users.DataSource = users.Value.Select(Function(u) New UserLine(u)).ToList()
        _unknownItems = unknown.Value
        _unknown.DataSource = unknown.Value.Select(Function(u, position) New UnknownLine(u, position)).ToList()
        ShowStatus($"{users.Value.Count.ToString(CultureInfo.InvariantCulture)} people, {unknown.Value.Count.ToString(CultureInfo.InvariantCulture)} unknown ids or cards on devices.")
    End Sub

    Private Sub DoNext()
        ClearValidation()
        Dim result As OperationResult(Of Integer) = _services.Biometric.NextBiometricId()
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _biometricId.Value = result.Value
        ShowStatus($"The next free biometric id is {result.Value.ToString(CultureInfo.InvariantCulture)}.")
    End Sub

    Private Sub DoSetId()
        ClearValidation()
        Dim person As UserLine = PickedUser()
        If person Is Nothing Then
            ShowError("Pick a person in the list first.")
            Return
        End If
        Dim result As OperationResult = _services.Biometric.AssignBiometricId(person.EmployeeId, CInt(_biometricId.Value))
        Finish(result, "The biometric id is set.", _biometricId)
    End Sub

    Private Sub DoClearId()
        ClearValidation()
        Dim person As UserLine = PickedUser()
        If person Is Nothing Then
            ShowError("Pick a person in the list first.")
            Return
        End If
        Finish(_services.Biometric.ClearBiometricId(person.EmployeeId), "The biometric id is cleared.", Nothing)
    End Sub

    Private Sub DoMapCard()
        ClearValidation()
        Dim person As UserLine = PickedUser()
        If person Is Nothing Then
            ShowError("Pick a person in the list first.")
            Return
        End If
        Finish(_services.Biometric.MapCard(person.EmployeeId, _cardNo.Text), "The card is mapped. The old card, if any, is retired.", _cardNo)
    End Sub

    Private Sub DoRetireCard()
        ClearValidation()
        Dim person As UserLine = PickedUser()
        If person Is Nothing Then
            ShowError("Pick a person in the list first.")
            Return
        End If
        Finish(_services.Biometric.UnmapCard(person.EmployeeId), "The card is retired.", Nothing)
    End Sub

    Private Sub Finish(result As OperationResult, done As String, field As Control)
        If Not result.Succeeded Then
            If field IsNot Nothing Then SetFieldError(field, result.Message)
            ShowError(result.Message)
            Return
        End If
        DoLoad()
        ShowStatus(done)
    End Sub

    Private Sub DoLink()
        ClearValidation()
        Dim person As UserLine = PickedUser()
        Dim row As UnknownLine = PickedUnknown()
        If person Is Nothing OrElse row Is Nothing Then
            ShowError("Pick a person in the top list and an unknown id or card in the lower list.")
            Return
        End If
        Dim result As OperationResult(Of ResolveResult) = _services.Biometric.LinkUnknown(_unknownItems(row.Index), person.EmployeeId)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        DoLoad()
        ShowStatus($"Linked. {result.Value.Resolved.ToString(CultureInfo.InvariantCulture)} punches matched, {result.Value.StillUnmatched.ToString(CultureInfo.InvariantCulture)} still waiting.")
    End Sub

    Private Sub DoPunches()
        Using form As New BiometricPunchesForm(_services)
            form.ShowDialog(Me)
        End Using
    End Sub

    Private Sub DoResolve()
        ClearValidation()
        Dim result As OperationResult(Of ResolveResult) = _services.Biometric.ResolvePunches()
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        DoLoad()
        ShowStatus($"{result.Value.Resolved.ToString(CultureInfo.InvariantCulture)} punches matched, {result.Value.StillUnmatched.ToString(CultureInfo.InvariantCulture)} still waiting.")
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        DoLoad()
    End Sub

    Private NotInheritable Class UserLine
        Public Sub New(item As BiometricUserItem)
            EmployeeId = item.EmployeeId
            EmpCode = item.EmpCode
            FullName = item.FullName
            BiometricId = If(item.BiometricId.HasValue, item.BiometricId.Value.ToString(CultureInfo.InvariantCulture), String.Empty)
            CardNo = If(item.CardNo, String.Empty)
            LastPunch = If(item.LastPunchTime.HasValue, item.LastPunchTime.Value.ToString(TimeFormat, CultureInfo.InvariantCulture), String.Empty)
            Punches = item.Punches30.ToString(CultureInfo.InvariantCulture)
        End Sub

        Public ReadOnly Property EmployeeId As Integer
        Public ReadOnly Property EmpCode As String
        Public ReadOnly Property FullName As String
        Public ReadOnly Property BiometricId As String
        Public ReadOnly Property CardNo As String
        Public ReadOnly Property LastPunch As String
        Public ReadOnly Property Punches As String
    End Class

    Private NotInheritable Class UnknownLine
        Public Sub New(item As UnknownBiometricUser, position As Integer)
            Index = position
            DeviceUserId = If(item.DeviceUserId.HasValue, item.DeviceUserId.Value.ToString(CultureInfo.InvariantCulture), String.Empty)
            CardNo = If(item.CardNo, String.Empty)
            Punches = item.PunchCount.ToString(CultureInfo.InvariantCulture)
            FirstSeen = item.FirstSeen.ToString(TimeFormat, CultureInfo.InvariantCulture)
            LastSeen = item.LastSeen.ToString(TimeFormat, CultureInfo.InvariantCulture)
            Devices = item.DeviceCount.ToString(CultureInfo.InvariantCulture)
        End Sub

        Public ReadOnly Property Index As Integer
        Public ReadOnly Property DeviceUserId As String
        Public ReadOnly Property CardNo As String
        Public ReadOnly Property Punches As String
        Public ReadOnly Property FirstSeen As String
        Public ReadOnly Property LastSeen As String
        Public ReadOnly Property Devices As String
    End Class
End Class
