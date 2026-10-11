Imports System.Drawing
Imports System.Windows.Forms

''' <summary>The sign-in dialog. Returns DialogResult.OK with Session set. Forces a password change when required.</summary>
Public NotInheritable Class LoginForm
    Inherits BaseForm

    Private ReadOnly _auth As IAuthService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _userBox As New TextBox()
    Private ReadOnly _passwordBox As New TextBox()
    Private ReadOnly _signIn As New Button()
    Private ReadOnly _cancel As New Button()

    Public Sub New(auth As IAuthService, session As ISessionContext)
        MyBase.New("Sign in", "Revolution HRMS")
        _auth = auth
        _session = session
        BuildLayout()
    End Sub

    ''' <summary>The signed-in session after DialogResult.OK.</summary>
    Public Property SignedInSession As UserSession

    Private Sub BuildLayout()
        ClientSize = New Size(420, 340)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        _userBox.MaxLength = 100
        _passwordBox.UseSystemPasswordChar = True
        _passwordBox.MaxLength = 128
        Dim layout = CreateFormLayout()
        AddField(layout, "User name", _userBox)
        AddField(layout, "Password", _passwordBox)
        Dim bar = CreateButtonBar()
        _signIn.Text = "Sign in"
        _cancel.Text = "Exit"
        _cancel.DialogResult = DialogResult.Cancel
        bar.Controls.Add(_signIn)
        bar.Controls.Add(_cancel)
        AcceptButton = _signIn
        CancelButton = _cancel
        AddHandler _signIn.Click, AddressOf OnSignInClick
    End Sub

    Private Async Sub OnSignInClick(sender As Object, e As EventArgs)
        ClearValidation()
        If String.IsNullOrWhiteSpace(_userBox.Text) Then
            SetFieldError(_userBox, "Enter your user name.")
            Return
        End If
        If _passwordBox.Text.Length = 0 Then
            SetFieldError(_passwordBox, "Enter your password.")
            Return
        End If
        Dim name = _userBox.Text
        Dim password = _passwordBox.Text
        Dim result As OperationResult(Of UserSession) = Nothing
        Try
            Await RunBusyAsync(Function()
                                   Return Task.Run(Sub()
                                                       result = _auth.Login(name, password)
                                                   End Sub)
                               End Function)
        Catch
            ShowError("Could not sign in. Check the database connection and try again.")
            Return
        End Try
        _passwordBox.Clear()
        If Not result.Succeeded Then
            ShowError(result.Message)
            _passwordBox.Focus()
            Return
        End If
        If result.Value.MustChangePassword AndAlso Not ForcePasswordChange(result.Value.UserName, password) Then
            Return
        End If
        SignedInSession = _session.Current
        DialogResult = DialogResult.OK
    End Sub

    Private Function ForcePasswordChange(userName As String, currentPassword As String) As Boolean
        Using dialog As New ChangePasswordForm(_auth, userName, currentPassword)
            If dialog.ShowDialog(Me) = DialogResult.OK Then
                Return True
            End If
        End Using
        _session.SignOut()
        ShowError("You must set a new password before you can continue.")
        Return False
    End Function
End Class
