Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Resets the password of a user the database lists for the signed-in user (ADR 0022). The new password is temporary: the user must change it at the next sign-in.
''' </summary>
Public NotInheritable Class ResetPasswordForm
    Inherits BaseForm

    Private ReadOnly _admin As IUserPasswordAdminService
    Private ReadOnly _users As New ComboBox()
    Private ReadOnly _passwordBox As New TextBox()
    Private ReadOnly _confirmBox As New TextBox()
    Private ReadOnly _save As New Button()
    Private ReadOnly _close As New Button()

    Public Sub New(admin As IUserPasswordAdminService, signedInUserName As String)
        MyBase.New("Reset user password", signedInUserName)
        If admin Is Nothing Then Throw New ArgumentNullException(NameOf(admin))
        _admin = admin
        BuildLayout()
        LoadUsers()
    End Sub

    Private Sub BuildLayout()
        ClientSize = New Size(460, 420)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        _users.DropDownStyle = ComboBoxStyle.DropDownList
        For Each box In {_passwordBox, _confirmBox}
            box.UseSystemPasswordChar = True
            box.MaxLength = 128
        Next
        Dim layout = CreateFormLayout()
        AddField(layout, "User", _users)
        AddField(layout, "Temporary password", _passwordBox)
        AddField(layout, "Confirm temporary password", _confirmBox)
        layout.Controls.Add(New Label With {
            .Text = "Use at least 10 characters with upper case, lower case, a digit and a symbol. Tell the user the temporary password in person or by phone: they must set a new one at the next sign-in. A locked account is unlocked.",
            .AutoSize = True,
            .MaximumSize = New Size(420, 0),
            .ForeColor = Theme.TextMuted
        })
        Dim bar = CreateButtonBar()
        _save.Text = "Reset"
        _close.Text = "Close"
        _close.DialogResult = DialogResult.Cancel
        bar.Controls.Add(_save)
        bar.Controls.Add(_close)
        AcceptButton = _save
        CancelButton = _close
        AddHandler _save.Click, AddressOf OnSaveClick
    End Sub

    Private Sub LoadUsers()
        Dim listed = _admin.ListResettable()
        If Not listed.Succeeded Then
            ShowError(listed.Message)
            _save.Enabled = False
            Return
        End If
        For Each user In listed.Value
            _users.Items.Add(New UserChoice(user))
        Next
        If _users.Items.Count = 0 Then
            ShowStatus("There is no user whose password you can reset here.")
            _save.Enabled = False
        Else
            _users.SelectedIndex = 0
        End If
    End Sub

    Protected Overrides Sub OnSaveRequested()
        _save.PerformClick()
    End Sub

    Private Async Sub OnSaveClick(sender As Object, e As EventArgs)
        ClearValidation()
        Dim choice = TryCast(_users.SelectedItem, UserChoice)
        If choice Is Nothing Then
            SetFieldError(_users, "Choose a user.")
            Return
        End If
        If _passwordBox.Text <> _confirmBox.Text Then
            SetFieldError(_confirmBox, "The two passwords do not match.")
            Return
        End If
        Dim userId = choice.User.UserId
        Dim password = _passwordBox.Text
        Dim result As OperationResult = Nothing
        Try
            Await RunBusyAsync(Function()
                                   Return Task.Run(Sub()
                                                       result = _admin.ResetPassword(userId, password)
                                                   End Sub)
                               End Function)
        Catch
            ShowError("The password could not be reset. Check the database connection and try again.")
            Return
        End Try
        _passwordBox.Clear()
        _confirmBox.Clear()
        If result.Succeeded Then
            ShowStatus($"The password of {choice.User.UserName} is reset. They must choose a new one at the next sign-in.")
        Else
            ShowError(result.Message)
            _passwordBox.Focus()
        End If
    End Sub

    ''' <summary>What the user list shows: the user name, with the role and a note when the account is locked.</summary>
    Private NotInheritable Class UserChoice
        Public Sub New(user As ResettableUser)
            Me.User = user
        End Sub

        Public ReadOnly Property User As ResettableUser

        Public Overrides Function ToString() As String
            Return $"{User.UserName} ({User.Role}){If(User.IsLocked, ", locked", String.Empty)}"
        End Function
    End Class
End Class
