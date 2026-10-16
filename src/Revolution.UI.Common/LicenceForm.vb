Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' The Developer's licence screen (ADR 0018): the installation id to quote when issuing a licence, every Associate's current licence with
''' its state, and importing a licence file. The file is verified before it is stored; nothing here can make a licence.
''' </summary>
Public NotInheritable Class LicenceForm
    Inherits BaseForm

    Private ReadOnly _admin As ILicenceAdminService
    Private ReadOnly _installationBox As New TextBox()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _copyButton As New Button()
    Private ReadOnly _importButton As New Button()
    Private ReadOnly _refreshButton As New Button()
    Private ReadOnly _closeButton As New Button()

    Public Sub New(admin As ILicenceAdminService)
        MyBase.New("Licences", "Revolution HRMS")
        If admin Is Nothing Then Throw New ArgumentNullException(NameOf(admin))
        _admin = admin
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ClientSize = New Size(820, 520)
        _installationBox.ReadOnly = True
        _grid.Height = 280
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _grid.AutoGenerateColumns = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        AddColumn("Associate", "AssociateUserName")
        AddColumn("Plan", "PlanCap")
        AddColumn("Last day", "ValidToText")
        AddColumn("Companies", "MaxCompanies")
        AddColumn("Users", "MaxActiveUsers")
        AddColumn("Status", "StatusText")
        Dim layout = CreateFormLayout()
        AddField(layout, "Installation id", _installationBox)
        AddField(layout, "Current licences", _grid)
        Dim bar = CreateButtonBar()
        _copyButton.Text = "Copy installation id"
        _importButton.Text = "Import licence file..."
        _refreshButton.Text = "Refresh"
        _closeButton.Text = "Close"
        _closeButton.DialogResult = DialogResult.Cancel
        For Each actionButton In {_copyButton, _importButton, _refreshButton, _closeButton}
            actionButton.AutoSize = True
            bar.Controls.Add(actionButton)
        Next
        CancelButton = _closeButton
        AddHandler _copyButton.Click, AddressOf OnCopyClick
        AddHandler _importButton.Click, AddressOf OnImportClick
        AddHandler _refreshButton.Click, AddressOf OnRefreshClick
        AddHandler Shown, AddressOf OnFormShown
    End Sub

    Private Sub AddColumn(header As String, propertyName As String)
        _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = header, .DataPropertyName = propertyName})
    End Sub

    Private Async Sub OnFormShown(sender As Object, args As EventArgs)
        Await LoadAsync()
    End Sub

    Private Async Sub OnRefreshClick(sender As Object, args As EventArgs)
        Await LoadAsync()
    End Sub

    Private Sub OnCopyClick(sender As Object, args As EventArgs)
        If _installationBox.Text.Length > 0 Then
            Clipboard.SetText(_installationBox.Text)
            ShowStatus("Installation id copied.")
        End If
    End Sub

    Private Async Function LoadAsync() As Task
        ClearValidation()
        Dim installationId As Guid = Guid.Empty
        Dim listed As OperationResult(Of IReadOnlyList(Of LicenceOverview)) = Nothing
        Try
            Await RunBusyAsync(Function()
                                   Return Task.Run(Sub()
                                                       installationId = _admin.GetInstallationId()
                                                       listed = _admin.ListCurrent()
                                                   End Sub)
                               End Function)
        Catch
            ShowError("Could not load the licences. Check the database connection and try again.")
            Return
        End Try
        _installationBox.Text = installationId.ToString()
        If Not listed.Succeeded Then
            ShowError(listed.Message)
            Return
        End If
        _grid.DataSource = listed.Value.ToList()
        ShowStatus($"{listed.Value.Count} current licence(s).")
    End Function

    Private Async Sub OnImportClick(sender As Object, args As EventArgs)
        ClearValidation()
        Dim licenceText As String = Nothing
        Using dialog As New OpenFileDialog() With {.Title = "Choose a licence file",
                                                   .Filter = "Licence files (*.lic;*.txt)|*.lic;*.txt|All files (*.*)|*.*",
                                                   .CheckFileExists = True}
            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If
            Try
                licenceText = IO.File.ReadAllText(dialog.FileName)
            Catch ex As IO.IOException
                ShowError("The file could not be read: " & ex.Message)
                Return
            Catch ex As UnauthorizedAccessException
                ShowError("The file could not be read: " & ex.Message)
                Return
            End Try
        End Using
        Dim result As OperationResult = Nothing
        Try
            Await RunBusyAsync(Function()
                                   Return Task.Run(Sub()
                                                       result = _admin.Import(licenceText)
                                                   End Sub)
                               End Function)
        Catch
            ShowError("The licence could not be imported. Check the database connection and try again.")
            Return
        End Try
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Await LoadAsync()
        ShowStatus("Licence imported. It applies the next time that Associate's session checks it.")
    End Sub
End Class
