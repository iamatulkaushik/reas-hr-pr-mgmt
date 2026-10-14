Imports System.ComponentModel
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Asks for a company name and creates the company for the signed-in Associate. A new company starts on the Copper plan.</summary>
Public NotInheritable Class NewCompanyForm
    Inherits BaseForm

    Private ReadOnly _admin As IAssociateAdminService
    Private ReadOnly _nameBox As New TextBox()
    Private ReadOnly _save As New Button()
    Private ReadOnly _cancel As New Button()

    Public Sub New(admin As IAssociateAdminService)
        MyBase.New("New company", "Revolution HRMS")
        _admin = admin
        BuildLayout()
    End Sub

    ''' <summary>The id of the company created, after DialogResult.OK.</summary>
    <Browsable(False), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property CreatedCompanyId As Integer

    Private Sub BuildLayout()
        ClientSize = New Size(420, 260)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        _nameBox.MaxLength = 200
        Dim layout = CreateFormLayout()
        AddField(layout, "Company name", _nameBox)
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
        If String.IsNullOrWhiteSpace(_nameBox.Text) Then
            SetFieldError(_nameBox, "Enter the company name.")
            Return
        End If
        Dim name = _nameBox.Text
        Dim result As OperationResult(Of Integer) = Nothing
        Try
            Await RunBusyAsync(Function()
                                   Return Task.Run(Sub()
                                                       result = _admin.CreateCompany(name)
                                                   End Sub)
                               End Function)
        Catch
            ShowError("The company could not be created. Check the database connection and try again.")
            Return
        End Try
        If result.Succeeded Then
            CreatedCompanyId = result.Value
            DialogResult = DialogResult.OK
        Else
            ShowError(result.Message)
            _nameBox.Focus()
        End If
    End Sub
End Class
