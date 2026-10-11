Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Sets a new password. Pass the current password when it is already known (forced change right after login).</summary>
Public NotInheritable Class ChangePasswordForm
    Inherits BaseForm

    Private ReadOnly _auth As IAuthService
    Private ReadOnly _userName As String
    Private ReadOnly _knownCurrent As String
    Private ReadOnly _currentBox As New TextBox()
    Private ReadOnly _newBox As New TextBox()
    Private ReadOnly _confirmBox As New TextBox()
    Private ReadOnly _save As New Button()
    Private ReadOnly _cancel As New Button()

    Public Sub New(auth As IAuthService, userName As String, Optional knownCurrentPassword As String = Nothing)
        MyBase.New("Change password", userName)
        _auth = auth
        _userName = userName
        _knownCurrent = knownCurrentPassword
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ClientSize = New Size(420, 380)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        For Each box In {_currentBox, _newBox, _confirmBox}
            box.UseSystemPasswordChar = True
            box.MaxLength = 128
        Next
        Dim layout = CreateFormLayout()
        If _knownCurrent Is Nothing Then
            AddField(layout, "Current password", _currentBox)
        End If
        AddField(layout, "New password", _newBox)
        AddField(layout, "Confirm new password", _confirmBox)
        layout.Controls.Add(New Label With {
            .Text = "Use at least 10 characters with upper case, lower case, a digit and a symbol.",
            .AutoSize = True,
            .MaximumSize = New Size(380, 0),
            .ForeColor = Theme.TextMuted
        })
        Dim bar = CreateButtonBar()
        _save.Text = "Save"
        _cancel.Text = "Cancel"
        _cancel.DialogResult = DialogResult.Cancel
        bar.Controls.Add(_save)
        bar.Controls.Add(_cancel)
        AcceptButton = _save
        CancelButton = _cancel
        AddHandler _save.Click, AddressOf OnSaveClick
    End Sub

    Protected Overrides Sub OnSaveRequested()
        _save.PerformClick()
    End Sub

    Private Async Sub OnSaveClick(sender As Object, e As EventArgs)
        ClearValidation()
        If _knownCurrent Is Nothing AndAlso _currentBox.Text.Length = 0 Then
            SetFieldError(_currentBox, "Enter your current password.")
            Return
        End If
        If _newBox.Text <> _confirmBox.Text Then
            SetFieldError(_confirmBox, "The two new passwords do not match.")
            Return
        End If
        Dim current = If(_knownCurrent, _currentBox.Text)
        Dim newPassword = _newBox.Text
        Dim result As OperationResult = Nothing
        Try
            Await RunBusyAsync(Function()
                                   Return Task.Run(Sub()
                                                       result = _auth.ChangePassword(_userName, current, newPassword)
                                                   End Sub)
                               End Function)
        Catch
            ShowError("The password could not be changed. Check the database connection and try again.")
            Return
        End Try
        If result.Succeeded Then
            DialogResult = DialogResult.OK
        Else
            ShowError(result.Message)
            _newBox.Focus()
        End If
    End Sub
End Class
