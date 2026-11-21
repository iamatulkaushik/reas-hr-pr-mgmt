Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Issues a licence to an Associate (ADR 0018, amended). The Developer chooses the plan, users, companies and dates, points to the private
''' signing key file and types its password. The licence is signed, imported for this installation, and a backup copy is saved beside the key file.
''' The key and its password are used for that one signing and are not kept.
''' </summary>
Public NotInheritable Class IssueLicenceForm
    Inherits BaseForm

    Private Const DateFormat As String = "dd-MM-yyyy"
    Private Const DefaultUsers As Integer = 5
    Private Const MaxUsers As Integer = 100000
    Private Const MaxCompanies As Integer = 500

    ''' <summary>The last key file used in this run, so issuing several licences does not mean browsing every time.</summary>
    Private Shared _lastKeyFile As String = String.Empty

    Private ReadOnly _issuer As ILicenceIssueService
    Private ReadOnly _associateBox As New TextBox() With {.MaxLength = 100}
    Private ReadOnly _planBox As New ComboBox() With {.DropDownStyle = ComboBoxStyle.DropDownList}
    Private ReadOnly _usersBox As New NumericUpDown() With {.Minimum = 1, .Maximum = MaxUsers, .Value = DefaultUsers}
    Private ReadOnly _companiesBox As New NumericUpDown() With {.Minimum = 0, .Maximum = MaxCompanies, .Value = 0}
    Private ReadOnly _fromPicker As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = DateFormat}
    Private ReadOnly _toPicker As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = DateFormat, .ShowCheckBox = True, .Checked = False}
    Private ReadOnly _noteBox As New TextBox() With {.MaxLength = 200}
    Private ReadOnly _keyFileBox As New TextBox()
    Private ReadOnly _browseButton As New Button()
    Private ReadOnly _passwordBox As New TextBox() With {.UseSystemPasswordChar = True, .MaxLength = 128}
    Private ReadOnly _issueButton As New Button()
    Private ReadOnly _cancelButton As New Button()

    Public Sub New(issuer As ILicenceIssueService, Optional associateUserName As String = "", Optional plan As PlanTier = PlanTier.Copper)
        MyBase.New("Issue licence", "Revolution HRMS")
        If issuer Is Nothing Then Throw New ArgumentNullException(NameOf(issuer))
        _issuer = issuer
        _associateBox.Text = If(associateUserName, String.Empty)
        _keyFileBox.Text = _lastKeyFile
        For Each tier In System.Enum.GetValues(GetType(PlanTier)).Cast(Of PlanTier)()
            _planBox.Items.Add(tier)
        Next
        _planBox.SelectedItem = plan
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ClientSize = New Size(520, 600)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        Dim layout = CreateDialogLayout()
        AddField(layout, "Associate user name", _associateBox)
        AddField(layout, "Plan", _planBox)
        AddField(layout, "Active users (Owner and Operator logins)", _usersBox)
        AddField(layout, "Companies (0 = the plan's quota)", _companiesBox)
        AddField(layout, "First day of use", _fromPicker)
        AddField(layout, "Last day of use (tick to set; empty = one year)", _toPicker)
        AddField(layout, "Note (optional)", _noteBox)
        Dim keyRow As New TableLayoutPanel With {.ColumnCount = 2, .RowCount = 1, .AutoSize = True, .Margin = New Padding(0)}
        keyRow.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        keyRow.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        _keyFileBox.Dock = DockStyle.Fill
        _browseButton.Text = "Browse..."
        _browseButton.AutoSize = True
        keyRow.Controls.Add(_keyFileBox, 0, 0)
        keyRow.Controls.Add(_browseButton, 1, 0)
        AddField(layout, "Signing key file", keyRow)
        AddField(layout, "Password of the signing key file", _passwordBox)
        Dim bar = CreateDialogButtonBar()
        _issueButton.Text = "Issue licence"
        _cancelButton.Text = "Cancel"
        _cancelButton.DialogResult = DialogResult.Cancel
        bar.Controls.Add(_issueButton)
        bar.Controls.Add(_cancelButton)
        AcceptButton = _issueButton
        CancelButton = _cancelButton
        AddHandler _browseButton.Click, AddressOf OnBrowseClick
        AddHandler _issueButton.Click, AddressOf OnIssueClick
    End Sub

    Protected Overrides Sub OnSaveRequested()
        _issueButton.PerformClick()
    End Sub

    Private Sub OnBrowseClick(sender As Object, e As EventArgs)
        Using dialog As New OpenFileDialog() With {.Title = "Choose the signing key file", .CheckFileExists = True,
                                                   .Filter = "All files (*.*)|*.*"}
            If dialog.ShowDialog(Me) = DialogResult.OK Then
                _keyFileBox.Text = dialog.FileName
                _passwordBox.Focus()
            End If
        End Using
    End Sub

    Private Async Sub OnIssueClick(sender As Object, e As EventArgs)
        ClearValidation()
        If _planBox.SelectedItem Is Nothing Then
            SetFieldError(_planBox, "Choose a plan.")
            Return
        End If
        Dim validTo As DateOnly? = Nothing
        If _toPicker.Checked Then
            validTo = DateOnly.FromDateTime(_toPicker.Value)
        End If
        Dim companies As Integer? = Nothing
        If _companiesBox.Value > 0 Then
            companies = CInt(_companiesBox.Value)
        End If
        Dim request As New LicenceIssueRequest(_associateBox.Text, CType(_planBox.SelectedItem, PlanTier), CInt(_usersBox.Value), companies,
                                               DateOnly.FromDateTime(_fromPicker.Value), validTo, _noteBox.Text, _keyFileBox.Text, _passwordBox.Text)
        Dim result As OperationResult(Of LicenceIssueResult) = Nothing
        Try
            Await RunBusyAsync(Function()
                                   Return Task.Run(Sub()
                                                       result = _issuer.Issue(request)
                                                   End Sub)
                               End Function)
        Catch
            ShowError("The licence could not be issued. Check the database connection and try again.")
            Return
        End Try
        If Not result.Succeeded Then
            ShowError(result.Message)
            _passwordBox.Focus()
            Return
        End If
        _passwordBox.Clear()
        _lastKeyFile = _keyFileBox.Text.Trim()
        Dim text = "The licence is issued and imported." & vbLf & result.Value.Summary
        If result.Value.BackupFile.Length > 0 Then
            text &= vbLf & vbLf & "Backup copy saved to:" & vbLf & result.Value.BackupFile &
                    vbLf & "Keep it, and the issued-licences.log beside your key file."
        End If
        If result.Value.Warning.Length > 0 Then
            text &= vbLf & vbLf & result.Value.Warning
        End If
        MessageBox.Show(Me, text, "Licence issued", MessageBoxButtons.OK, MessageBoxIcon.Information)
        DialogResult = DialogResult.OK
    End Sub
End Class
