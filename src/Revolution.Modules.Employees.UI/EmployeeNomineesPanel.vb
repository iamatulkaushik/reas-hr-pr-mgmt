Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Employees

''' <summary>
''' The Nominees tab: family members on top, nominations below. A nomination is optional. For each scheme the shares
''' may total up to 100 percent. A family member who is still a nominee cannot be deleted.
''' </summary>
Friend NotInheritable Class EmployeeNomineesPanel
    Inherits UserControl

    Private Const GridHeight As Integer = 130
    Private Const DateFormat As String = "dd-MM-yyyy"
    Private Const Caption As String = "Revolution HRMS"

    Private ReadOnly _services As EmployeesServices
    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _familyGrid As New DataGridView()
    Private ReadOnly _name As New TextBox() With {.MaxLength = FamilyValidator.MaxNameLength, .Width = 170}
    Private ReadOnly _relation As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDown, .MaxLength = FamilyValidator.MaxRelationLength, .Width = 110}
    Private ReadOnly _birth As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = DateFormat, .ShowCheckBox = True, .Width = 120}
    Private ReadOnly _dependent As New CheckBox() With {.Text = "Dependent", .AutoSize = True}
    Private ReadOnly _residing As New CheckBox() With {.Text = "Residing", .AutoSize = True, .Checked = True}
    Private ReadOnly _saveMember As New Button() With {.Text = "Save member", .AutoSize = True}
    Private ReadOnly _newMember As New Button() With {.Text = "New", .AutoSize = True}
    Private ReadOnly _deleteMember As New Button() With {.Text = "Delete", .AutoSize = True}
    Private ReadOnly _nomineeGrid As New DataGridView()
    Private ReadOnly _scheme As New ComboBox() With {.Width = 110}
    Private ReadOnly _member As New ComboBox() With {.Width = 200}
    Private ReadOnly _share As New TextBox() With {.Width = 60, .MaxLength = 6}
    Private ReadOnly _saveNominee As New Button() With {.Text = "Save nomination", .AutoSize = True}
    Private ReadOnly _removeNominee As New Button() With {.Text = "Remove", .AutoSize = True}
    Private ReadOnly _totals As New Label() With {.AutoSize = True}
    Private _employeeId As Integer
    Private _familyId As Integer
    Private _nomineeId As Integer
    Private _loading As Boolean

    Public Event Notice(text As String, isError As Boolean)

    Public Sub New(services As EmployeesServices, canEdit As Boolean)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _canEdit = canEdit
        BuildLayout()
    End Sub

    Public Sub ShowEmployee(employeeId As Integer)
        _employeeId = employeeId
        Enabled = employeeId > 0
        ClearMember()
        ClearNomination()
        If employeeId <= 0 Then
            _familyGrid.Rows.Clear()
            _nomineeGrid.Rows.Clear()
            _totals.Text = String.Empty
            Return
        End If
        ReloadFamily()
        ReloadNominees()
    End Sub

    Private Sub BuildLayout()
        Dim layout = FieldLayout.Create()
        layout.Padding = New Padding(Theme.Space2)
        ConfigureGrid(_familyGrid, New String() {"Name", "Relation", "Date of birth", "Dependent", "Residing"})
        ConfigureGrid(_nomineeGrid, New String() {"Scheme", "Member", "Relation", "Share %"})
        _relation.Items.AddRange(New Object() {"Spouse", "Son", "Daughter", "Father", "Mother", "Brother", "Sister", "Other"})
        FieldLayout.Bind(_scheme, SchemeChoices())

        layout.Controls.Add(New Label With {.Text = "Family members", .AutoSize = True, .Font = New Drawing.Font(Font, Drawing.FontStyle.Bold)})
        layout.Controls.Add(_familyGrid)
        layout.Controls.Add(Row(New Label With {.Text = "Name", .AutoSize = True, .Margin = New Padding(0, 6, 2, 0)}, _name,
                                New Label With {.Text = "Relation", .AutoSize = True, .Margin = New Padding(6, 6, 2, 0)}, _relation,
                                New Label With {.Text = "Born", .AutoSize = True, .Margin = New Padding(6, 6, 2, 0)}, _birth,
                                _dependent, _residing))
        layout.Controls.Add(Row(_saveMember, _newMember, _deleteMember))
        layout.Controls.Add(New Label With {.Text = "Nominations (optional)", .AutoSize = True, .Margin = New Padding(0, Theme.Space2, 0, 0), .Font = New Drawing.Font(Font, Drawing.FontStyle.Bold)})
        layout.Controls.Add(_nomineeGrid)
        layout.Controls.Add(Row(New Label With {.Text = "Scheme", .AutoSize = True, .Margin = New Padding(0, 6, 2, 0)}, _scheme,
                                New Label With {.Text = "Member", .AutoSize = True, .Margin = New Padding(6, 6, 2, 0)}, _member,
                                New Label With {.Text = "Share %", .AutoSize = True, .Margin = New Padding(6, 6, 2, 0)}, _share))
        layout.Controls.Add(Row(_saveNominee, _removeNominee))
        layout.Controls.Add(_totals)
        AutoScroll = True
        Controls.Add(layout)

        _saveMember.Enabled = _canEdit
        _newMember.Enabled = _canEdit
        _deleteMember.Enabled = _canEdit
        _saveNominee.Enabled = _canEdit
        _removeNominee.Enabled = _canEdit
        AddHandler _familyGrid.SelectionChanged, AddressOf OnFamilySelected
        AddHandler _nomineeGrid.SelectionChanged, AddressOf OnNomineeSelected
        AddHandler _saveMember.Click, Sub(sender As Object, e As EventArgs) SaveMember()
        AddHandler _newMember.Click, Sub(sender As Object, e As EventArgs) StartNewMember()
        AddHandler _deleteMember.Click, Sub(sender As Object, e As EventArgs) DeleteMember()
        AddHandler _saveNominee.Click, Sub(sender As Object, e As EventArgs) SaveNomination()
        AddHandler _removeNominee.Click, Sub(sender As Object, e As EventArgs) RemoveNomination()
    End Sub

    Private Shared Function Row(ParamArray items As Control()) As FlowLayoutPanel
        Dim bar As New FlowLayoutPanel With {.AutoSize = True, .WrapContents = True, .Margin = New Padding(0, 2, 0, 2)}
        For Each item In items
            bar.Controls.Add(item)
        Next
        Return bar
    End Function

    Private Shared Sub ConfigureGrid(grid As DataGridView, headers As String())
        grid.Height = GridHeight
        grid.Dock = DockStyle.Top
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

    Private Shared Function SchemeChoices() As IEnumerable(Of KeyValuePair(Of Integer, String))
        Return New List(Of KeyValuePair(Of Integer, String)) From {
            New KeyValuePair(Of Integer, String)(CInt(NomineeScheme.Epf), "EPF"),
            New KeyValuePair(Of Integer, String)(CInt(NomineeScheme.Eps), "EPS"),
            New KeyValuePair(Of Integer, String)(CInt(NomineeScheme.Gratuity), "Gratuity"),
            New KeyValuePair(Of Integer, String)(CInt(NomineeScheme.Esi), "ESI"),
            New KeyValuePair(Of Integer, String)(CInt(NomineeScheme.Insurance), "Insurance")}
    End Function

    Private Shared Function SchemeName(scheme As NomineeScheme) As String
        For Each pair In SchemeChoices()
            If pair.Key = CInt(scheme) Then Return pair.Value
        Next
        Return scheme.ToString()
    End Function

    Private Function FailIfNot(result As OperationResult) As Boolean
        If result.Succeeded Then Return False
        RaiseEvent Notice(result.Message, True)
        Return True
    End Function

    Private Sub ReloadFamily()
        Dim result = _services.Family.ListFamily(_employeeId)
        If Not result.Succeeded Then
            RaiseEvent Notice(result.Message, True)
            Return
        End If
        _loading = True
        Try
            _familyGrid.Rows.Clear()
            Dim choices As New List(Of KeyValuePair(Of Integer, String))()
            For Each item In result.Value
                Dim born As String = If(item.DateOfBirth.HasValue, item.DateOfBirth.Value.ToString(DateFormat, CultureInfo.InvariantCulture), String.Empty)
                Dim at = _familyGrid.Rows.Add(item.Name, item.Relation, born, If(item.IsDependent, "Yes", "No"), If(item.IsResiding, "Yes", "No"))
                _familyGrid.Rows(at).Tag = item
                choices.Add(New KeyValuePair(Of Integer, String)(item.FamilyId, $"{item.Name} ({item.Relation})"))
            Next
            FieldLayout.Bind(_member, choices)
            _familyGrid.ClearSelection()
            _familyGrid.CurrentCell = Nothing
        Finally
            _loading = False
        End Try
    End Sub

    Private Sub ReloadNominees()
        Dim result = _services.Family.ListNominees(_employeeId)
        If Not result.Succeeded Then
            RaiseEvent Notice(result.Message, True)
            Return
        End If
        _loading = True
        Try
            _nomineeGrid.Rows.Clear()
            For Each item In result.Value
                Dim at = _nomineeGrid.Rows.Add(SchemeName(item.Scheme), item.MemberName, item.Relation, item.SharePercent.ToString("0.##", CultureInfo.CurrentCulture))
                _nomineeGrid.Rows(at).Tag = item
            Next
            _nomineeGrid.ClearSelection()
            _nomineeGrid.CurrentCell = Nothing
        Finally
            _loading = False
        End Try
        ShowTotals(result.Value)
    End Sub

    Private Sub ShowTotals(rows As IReadOnlyList(Of NomineeRow))
        Dim parts As New List(Of String)()
        For Each scheme In rows.Select(Function(n) n.Scheme).Distinct().OrderBy(Function(s) CInt(s))
            Dim total As Decimal = rows.Where(Function(n) n.Scheme = scheme).Sum(Function(n) n.SharePercent)
            parts.Add($"{SchemeName(scheme)} {total.ToString("0.##", CultureInfo.CurrentCulture)}%")
        Next
        _totals.Text = If(parts.Count = 0, "No nominations saved.", "Totals: " & String.Join("   ", parts))
    End Sub

    Private Sub OnFamilySelected(sender As Object, e As EventArgs)
        If _loading OrElse _familyGrid.CurrentRow Is Nothing Then Return
        Dim item = DirectCast(_familyGrid.CurrentRow.Tag, FamilyMember)
        _familyId = item.FamilyId
        _name.Text = item.Name
        _relation.Text = item.Relation
        _birth.Checked = item.DateOfBirth.HasValue
        If item.DateOfBirth.HasValue Then _birth.Value = item.DateOfBirth.Value
        _dependent.Checked = item.IsDependent
        _residing.Checked = item.IsResiding
    End Sub

    Private Sub OnNomineeSelected(sender As Object, e As EventArgs)
        If _loading OrElse _nomineeGrid.CurrentRow Is Nothing Then Return
        Dim item = DirectCast(_nomineeGrid.CurrentRow.Tag, NomineeRow)
        _nomineeId = item.NomineeId
        _scheme.SelectedValue = CInt(item.Scheme)
        _member.SelectedValue = item.FamilyId
        _share.Text = item.SharePercent.ToString("0.##", CultureInfo.CurrentCulture)
    End Sub

    Private Sub ClearMember()
        _familyId = 0
        _name.Clear()
        _relation.Text = String.Empty
        _birth.Checked = False
        _dependent.Checked = False
        _residing.Checked = True
    End Sub

    Private Sub ClearNomination()
        _nomineeId = 0
        _share.Clear()
    End Sub

    Private Sub StartNewMember()
        _familyGrid.ClearSelection()
        _familyGrid.CurrentCell = Nothing
        ClearMember()
        _name.Focus()
    End Sub

    Private Sub SaveMember()
        If _employeeId <= 0 Then Return
        Dim member As New FamilyMember With {
            .FamilyId = _familyId,
            .Name = _name.Text,
            .Relation = _relation.Text,
            .DateOfBirth = If(_birth.Checked, CType(_birth.Value.Date, Date?), Nothing),
            .IsDependent = _dependent.Checked,
            .IsResiding = _residing.Checked}
        Dim result = _services.Family.SaveFamily(_employeeId, member)
        If Not result.Succeeded Then
            RaiseEvent Notice(result.Message, True)
            Return
        End If
        ClearMember()
        ReloadFamily()
        ReloadNominees()
        RaiseEvent Notice("Family member saved.", False)
    End Sub

    Private Sub DeleteMember()
        If _employeeId <= 0 OrElse _familyId = 0 Then
            RaiseEvent Notice("Select a family member first.", True)
            Return
        End If
        If MessageBox.Show(Me, $"Delete {_name.Text.Trim()}?", Caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
        If FailIfNot(_services.Family.DeleteFamily(_employeeId, _familyId)) Then Return
        ClearMember()
        ReloadFamily()
        RaiseEvent Notice("Family member deleted.", False)
    End Sub

    Private Sub SaveNomination()
        If _employeeId <= 0 Then Return
        Dim share As Decimal
        If Not Decimal.TryParse(_share.Text.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, share) Then
            RaiseEvent Notice("Share must be a number, for example 50 or 33.33.", True)
            Return
        End If
        Dim scheme = CType(FieldLayout.IntValue(_scheme), NomineeScheme)
        If FailIfNot(_services.Family.SaveNominee(_employeeId, scheme, FieldLayout.IntValue(_member), share)) Then Return
        ClearNomination()
        ReloadNominees()
        RaiseEvent Notice("Nomination saved.", False)
    End Sub

    Private Sub RemoveNomination()
        If _employeeId <= 0 OrElse _nomineeId = 0 Then
            RaiseEvent Notice("Select a nomination first.", True)
            Return
        End If
        If FailIfNot(_services.Family.DeleteNominee(_employeeId, _nomineeId)) Then Return
        ClearNomination()
        ReloadNominees()
        RaiseEvent Notice("Nomination removed.", False)
    End Sub
End Class
