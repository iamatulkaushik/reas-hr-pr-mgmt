Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Shown to an Associate who has not opened a company yet. Open a company, create one, or add the Owner login of one.
''' Returns DialogResult.OK once a company is open in the session.
''' </summary>
Public NotInheritable Class CompanyPickerForm
    Inherits BaseForm

    Private ReadOnly _auth As IAuthService
    Private ReadOnly _admin As IAssociateAdminService
    Private ReadOnly _list As New ListBox()
    Private ReadOnly _open As New Button()
    Private ReadOnly _newCompany As New Button()
    Private ReadOnly _addOwner As New Button()
    Private ReadOnly _exit As New Button()

    Public Sub New(auth As IAuthService, admin As IAssociateAdminService)
        MyBase.New("Choose a company", "Revolution HRMS")
        _auth = auth
        _admin = admin
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ClientSize = New Size(520, 480)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        _list.DisplayMember = "CompanyName"
        _list.Height = 280
        Dim layout = CreateFormLayout()
        AddField(layout, "Your companies", _list)
        Dim bar = CreateButtonBar()
        _open.Text = "Open"
        _newCompany.Text = "New company..."
        _addOwner.Text = "Add Owner login..."
        _exit.Text = "Exit"
        _exit.DialogResult = DialogResult.Cancel
        bar.Controls.Add(_open)
        bar.Controls.Add(_exit)
        bar.Controls.Add(_addOwner)
        bar.Controls.Add(_newCompany)
        AcceptButton = _open
        CancelButton = _exit
        UpdateButtons()
        AddHandler _list.SelectedIndexChanged, AddressOf OnSelectionChanged
        AddHandler _list.DoubleClick, AddressOf OnOpenClick
        AddHandler _open.Click, AddressOf OnOpenClick
        AddHandler _newCompany.Click, AddressOf OnNewCompanyClick
        AddHandler _addOwner.Click, AddressOf OnAddOwnerClick
        AddHandler Shown, AddressOf OnFormShown
    End Sub

    Private Function Selected() As CompanySummary
        Return TryCast(_list.SelectedItem, CompanySummary)
    End Function

    Private Sub UpdateButtons()
        Dim has = Selected() IsNot Nothing
        _open.Enabled = has
        _addOwner.Enabled = has
    End Sub

    Private Sub OnSelectionChanged(sender As Object, e As EventArgs)
        UpdateButtons()
    End Sub

    Private Async Sub OnFormShown(sender As Object, e As EventArgs)
        Await LoadCompaniesAsync(0)
    End Sub

    Private Async Function LoadCompaniesAsync(selectCompanyId As Integer) As Task
        ClearValidation()
        Dim result As OperationResult(Of IReadOnlyList(Of CompanySummary)) = Nothing
        Try
            Await RunBusyAsync(Function()
                                   Return Task.Run(Sub()
                                                       result = _auth.ListCompanies()
                                                   End Sub)
                               End Function)
        Catch
            ShowError("Could not load your companies. Check the database connection and try again.")
            Return
        End Try
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Dim companies = result.Value.ToList()
        _list.DataSource = companies
        _list.DisplayMember = "CompanyName"
        Dim index = companies.FindIndex(Function(c) c.CompanyId = selectCompanyId)
        If index >= 0 Then
            _list.SelectedIndex = index
        ElseIf companies.Count > 0 Then
            _list.SelectedIndex = 0
        End If
        UpdateButtons()
    End Function

    Private Async Sub OnOpenClick(sender As Object, e As EventArgs)
        Dim chosen = Selected()
        If chosen Is Nothing Then
            Return
        End If
        ClearValidation()
        Dim result As OperationResult(Of UserSession) = Nothing
        Try
            Await RunBusyAsync(Function()
                                   Return Task.Run(Sub()
                                                       result = _auth.SelectCompany(chosen.CompanyId)
                                                   End Sub)
                               End Function)
        Catch
            ShowError("Could not open the company. Check the database connection and try again.")
            Return
        End Try
        If result.Succeeded Then
            DialogResult = DialogResult.OK
        Else
            ShowError(result.Message)
        End If
    End Sub

    Private Async Sub OnNewCompanyClick(sender As Object, e As EventArgs)
        Dim createdId = 0
        Using dialog As New NewCompanyForm(_admin)
            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If
            createdId = dialog.CreatedCompanyId
        End Using
        Await LoadCompaniesAsync(createdId)
    End Sub

    Private Sub OnAddOwnerClick(sender As Object, e As EventArgs)
        Dim chosen = Selected()
        If chosen Is Nothing Then
            Return
        End If
        Using dialog As New OwnerLoginForm(_admin, chosen.CompanyId, chosen.CompanyName)
            If dialog.ShowDialog(Me) = DialogResult.OK Then
                ShowStatus("Owner login created.")
            End If
        End Using
    End Sub
End Class
