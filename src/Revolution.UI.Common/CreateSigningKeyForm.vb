Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Makes the Developer's licence signing key (once, ADR 0018). The private half goes to a password-protected file that only the Developer keeps;
''' the public key line shown afterwards is safe to share and is pasted into LicenceKeys.Trusted before the next build.
''' </summary>
Public NotInheritable Class CreateSigningKeyForm
    Inherits BaseForm

    Private Const MinPasswordLength As Integer = 12

    Private ReadOnly _issuer As ILicenceIssueService
    Private ReadOnly _keyIdBox As New TextBox() With {.MaxLength = 40}
    Private ReadOnly _fileBox As New TextBox()
    Private ReadOnly _browseButton As New Button()
    Private ReadOnly _passwordBox As New TextBox() With {.UseSystemPasswordChar = True, .MaxLength = 128}
    Private ReadOnly _confirmBox As New TextBox() With {.UseSystemPasswordChar = True, .MaxLength = 128}
    Private ReadOnly _resultBox As New TextBox() With {.Multiline = True, .ReadOnly = True, .WordWrap = True, .Height = 96, .ScrollBars = ScrollBars.Vertical}
    Private ReadOnly _createButton As New Button()
    Private ReadOnly _copyButton As New Button()
    Private ReadOnly _closeButton As New Button()
    Private _trustedLine As String = String.Empty

    Public Sub New(issuer As ILicenceIssueService)
        MyBase.New("Create signing key", "Revolution HRMS")
        If issuer Is Nothing Then Throw New ArgumentNullException(NameOf(issuer))
        _issuer = issuer
        _keyIdBox.Text = "revolution-" & DateTime.Now.Year.ToString(Globalization.CultureInfo.InvariantCulture)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ClientSize = New Size(540, 640)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        Dim layout = CreateDialogLayout()
        layout.Controls.Add(New Label With {
            .Text = "Do this once. The key file and its password stay with you only: back the file up in two places, and never give it to an Associate or put it in the repository. If it is lost, no new licences can be signed under this key id. After creating the key, paste the public key line into LicenceKeys.Trusted in Revolution.Core, build, and install that build.",
            .AutoSize = True,
            .MaximumSize = New Size(480, 0),
            .ForeColor = Theme.TextMuted,
            .Margin = New Padding(0, 0, 0, Theme.Space1)
        })
        AddField(layout, "Key id", _keyIdBox)
        Dim fileRow As New TableLayoutPanel With {.ColumnCount = 2, .RowCount = 1, .AutoSize = True, .Margin = New Padding(0)}
        fileRow.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        fileRow.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        _fileBox.Dock = DockStyle.Fill
        _browseButton.Text = "Browse..."
        _browseButton.AutoSize = True
        fileRow.Controls.Add(_fileBox, 0, 0)
        fileRow.Controls.Add(_browseButton, 1, 0)
        AddField(layout, "Save the key file as (a new file)", fileRow)
        AddField(layout, $"Password for the key file (at least {MinPasswordLength} characters)", _passwordBox)
        AddField(layout, "Type the password again", _confirmBox)
        AddField(layout, "Public key line (safe to share)", _resultBox)
        Dim bar = CreateDialogButtonBar()
        _createButton.Text = "Create key"
        _copyButton.Text = "Copy public key line"
        _copyButton.Enabled = False
        _closeButton.Text = "Close"
        _closeButton.DialogResult = DialogResult.Cancel
        bar.Controls.Add(_createButton)
        bar.Controls.Add(_copyButton)
        bar.Controls.Add(_closeButton)
        AcceptButton = _createButton
        CancelButton = _closeButton
        AddHandler _browseButton.Click, AddressOf OnBrowseClick
        AddHandler _createButton.Click, AddressOf OnCreateClick
        AddHandler _copyButton.Click, AddressOf OnCopyClick
    End Sub

    Protected Overrides Sub OnSaveRequested()
        _createButton.PerformClick()
    End Sub

    Private Sub OnBrowseClick(sender As Object, e As EventArgs)
        Using dialog As New SaveFileDialog() With {.Title = "Save the signing key file", .OverwritePrompt = False, .FileName = "licence-signing-key.key",
                                                   .Filter = "Key files (*.key)|*.key|All files (*.*)|*.*"}
            If dialog.ShowDialog(Me) = DialogResult.OK Then
                _fileBox.Text = dialog.FileName
                _passwordBox.Focus()
            End If
        End Using
    End Sub

    Private Async Sub OnCreateClick(sender As Object, e As EventArgs)
        ClearValidation()
        If _passwordBox.Text <> _confirmBox.Text Then
            SetFieldError(_confirmBox, "The two passwords do not match.")
            Return
        End If
        Dim keyId = _keyIdBox.Text
        Dim filePath = _fileBox.Text
        Dim password = _passwordBox.Text
        Dim result As OperationResult(Of SigningKeyInfo) = Nothing
        Try
            Await RunBusyAsync(Function()
                                   Return Task.Run(Sub()
                                                       result = _issuer.CreateSigningKey(keyId, filePath, password)
                                                   End Sub)
                               End Function)
        Catch
            ShowError("The key could not be created. Try again.")
            Return
        End Try
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _passwordBox.Clear()
        _confirmBox.Clear()
        _trustedLine = result.Value.TrustedLine
        _resultBox.Text = "Key file saved to " & result.Value.FilePath & vbCrLf & _trustedLine
        _createButton.Enabled = False
        _copyButton.Enabled = True
        _copyButton.Focus()
    End Sub

    Private Sub OnCopyClick(sender As Object, e As EventArgs)
        If _trustedLine.Length > 0 Then
            Clipboard.SetText(_trustedLine)
        End If
    End Sub
End Class
