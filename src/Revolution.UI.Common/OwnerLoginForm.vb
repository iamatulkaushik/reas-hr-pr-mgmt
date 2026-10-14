Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Creates the Owner login of one company. The Owner must choose a new password at first sign-in.</summary>
Public NotInheritable Class OwnerLoginForm
    Inherits BaseForm

    Private ReadOnly _admin As IAssociateAdminService
    Private ReadOnly _companyId As Integer
    Private ReadOnly _userBox As New TextBox()
    Private ReadOnly _passwordBox As New TextBox()
    Private ReadOnly _confirmBox As New TextBox()
    Private ReadOnly _save As New Button()
    Private ReadOnly _cancel As New Button()

    Public Sub New(admin As IAssociateAdminService, companyId As Integer, companyName As String)
        MyBase.New("Owner login", companyName)
        _admin = admin
        _companyId = companyId
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ClientSize = New Size(420, 400)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        _userBox.MaxLength = 100
        For Each box In {_passwordBox, _confirmBox}
            box.UseSystemPasswordChar = True
            box.MaxLength = 128
        Next
        Dim layout = CreateFormLayout()
        AddField(layout, "Owner user name", _userBox)
        AddField(layout, "Temporary password", _passwordBox)
        AddField(layout, "Confirm temporary password", _confirmBox)
        layout.Controls.Add(New Label With {
            .Text = "Use at least 10 characters with upper case, lower case, a digit and a symbol. The Owner sets a new password at first sign-in.",
            .AutoSize = True,
            .MaximumSize = New Size(380, 0),
            .ForeColor = Theme.TextMuted
        })
        Dim bar = CreateButtonBar()
        _save.Text = "Create"
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
        If String.IsNullOrWhiteSpace(_userBox.Text) Then
            SetFieldError(_userBox, "Enter the Owner's user name.")
            Return
        End If
        If _passwordBox.Text <> _confirmBox.Text Then
            SetFieldError(_confirmBox, "The two passwords do not match.")
            Return
        End If
        Dim name = _userBox.Text
        Dim password = _passwordBox.Text
        Dim result As OperationResult(Of Integer) = Nothing
        Try
            Await RunBusyAsync(Function()
                                   Return Task.Run(Sub()
                                                       result = _admin.CreateOwner(_companyId, name, password)
                                                   End Sub)
                               End Function)
        Catch
            ShowError("The Owner login could not be created. Check the database connection and try again.")
            Return
        End Try
        _passwordBox.Clear()
        _confirmBox.Clear()
        If result.Succeeded Then
            DialogResult = DialogResult.OK
        Else
            ShowError(result.Message)
            _userBox.Focus()
        End If
    End Sub
End Class
