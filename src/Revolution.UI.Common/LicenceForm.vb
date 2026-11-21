Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' The Developer's licence screen (ADR 0018): the installation id to quote when issuing a licence, every Associate's current licence with
''' its state, and importing a licence file. The file is verified before it is stored; nothing here can make a licence.
''' </summary>
Public NotInheritable Class LicenceForm
    Inherits BaseForm

    Private ReadOnly _admin As ILicenceAdminService
    Private ReadOnly _issuer As ILicenceIssueService
    Private ReadOnly _installationBox As New TextBox()
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _copyButton As New Button()
    Private ReadOnly _issueButton As New Button()
    Private ReadOnly _keyButton As New Button()
    Private ReadOnly _importButton As New Button()
    Private ReadOnly _revokeButton As New Button()
    Private ReadOnly _reinstateButton As New Button()
    Private ReadOnly _refreshButton As New Button()
    Private ReadOnly _closeButton As New Button()

    ''' <param name="issuer">The Developer's licence issuing. Without it the screen can only import licence files.</param>
    Public Sub New(admin As ILicenceAdminService, Optional issuer As ILicenceIssueService = Nothing)
        MyBase.New("Licences", "Revolution HRMS")
        If admin Is Nothing Then Throw New ArgumentNullException(NameOf(admin))
        _admin = admin
        _issuer = issuer
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        ClientSize = New Size(900, 620)
        _installationBox.ReadOnly = True
        _grid.Dock = DockStyle.Fill
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
        Dim installation As New FieldGrid() With {.MaxColumns = 1}
        installation.AddField("Installation id", _installationBox)
        _copyButton.Text = "Copy installation id"
        _importButton.Text = "Import licence file..."
        _revokeButton.Text = "Revoke..."
        _reinstateButton.Text = "Reinstate"
        _refreshButton.Text = "Refresh"
        _closeButton.Text = "Close"
        _closeButton.DialogResult = DialogResult.Cancel
        Dim listPanel As New TableLayoutPanel With {.ColumnCount = 1, .RowCount = 2, .Margin = New Padding(0)}
        listPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        listPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        listPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
        listPanel.Controls.Add(New Label With {.Text = "Current licences", .AutoSize = True, .Margin = New Padding(0, 0, 0, 2)}, 0, 0)
        listPanel.Controls.Add(_grid, 0, 1)
        ' The buttons run right to left, so Close is at the right end and the actions read left to right.
        Dim actions As New List(Of Button) From {_closeButton, _refreshButton, _reinstateButton, _revokeButton, _importButton, _copyButton}
        If _issuer IsNot Nothing Then
            _issueButton.Text = "Issue licence..."
            _keyButton.Text = "Create signing key..."
            actions.Add(_issueButton)
            actions.Add(_keyButton)
            AddHandler _issueButton.Click, AddressOf OnIssueClick
            AddHandler _keyButton.Click, AddressOf OnCreateKeyClick
        End If
        BuildStandardLayout(installation, actions, listPanel)
        CancelButton = _closeButton
        AddHandler _copyButton.Click, AddressOf OnCopyClick
        AddHandler _importButton.Click, AddressOf OnImportClick
        AddHandler _revokeButton.Click, AddressOf OnRevokeClick
        AddHandler _reinstateButton.Click, AddressOf OnReinstateClick
        AddHandler _refreshButton.Click, AddressOf OnRefreshClick
        AddHandler Shown, AddressOf OnFormShown
    End Sub

    Private Sub AddColumn(header As String, propertyName As String)
        _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.HeaderText = header, .DataPropertyName = propertyName})
    End Sub

    Private Async Sub OnFormShown(sender As Object, args As EventArgs)
        Await LoadAsync()
    End Sub

    Private Async Sub OnIssueClick(sender As Object, args As EventArgs)
        ClearValidation()
        Dim selected = SelectedLicence()
        Dim associate As String = If(selected Is Nothing, String.Empty, selected.AssociateUserName)
        Dim plan As PlanTier = If(selected Is Nothing, PlanTier.Copper, selected.PlanCap)
        Dim issued As Boolean
        Using dialog As New IssueLicenceForm(_issuer, associate, plan)
            issued = dialog.ShowDialog(Me) = DialogResult.OK
        End Using
        If issued Then
            Await LoadAsync()
            ShowStatus("Licence issued and imported.")
        End If
    End Sub

    Private Sub OnCreateKeyClick(sender As Object, args As EventArgs)
        Using dialog As New CreateSigningKeyForm(_issuer)
            dialog.ShowDialog(Me)
        End Using
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

    ''' <summary>The licence on the selected row, or Nothing when no row is selected.</summary>
    Private Function SelectedLicence() As LicenceOverview
        If _grid.CurrentRow Is Nothing Then
            Return Nothing
        End If
        Return TryCast(_grid.CurrentRow.DataBoundItem, LicenceOverview)
    End Function

    Private Function IsRevoked(licence As LicenceOverview) As Boolean
        Return licence.State.HasValue AndAlso licence.State.Value = LicenceState.Revoked
    End Function

    Private Async Sub OnRevokeClick(sender As Object, args As EventArgs)
        ClearValidation()
        Dim selected = SelectedLicence()
        If selected Is Nothing Then
            ShowError("Select an Associate's licence in the list first.")
            Return
        End If
        If IsRevoked(selected) Then
            ShowError("This licence is already revoked.")
            Return
        End If
        Dim why = Microsoft.VisualBasic.Interaction.InputBox(
            $"Why is the licence of {selected.AssociateUserName} being revoked? (required, up to 200 characters)", "Revoke licence")
        If why.Trim().Length = 0 Then
            Return
        End If
        Dim answer = MessageBox.Show(Me,
            $"Revoke the licence of {selected.AssociateUserName}? Their companies become read-only (view and print only) within a few minutes. You can reinstate it later.",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
        If answer <> DialogResult.Yes Then
            Return
        End If
        Await ChangeAsync(Function() _admin.Revoke(selected.AssociateUserName, why), "Licence revoked.")
    End Sub

    Private Async Sub OnReinstateClick(sender As Object, args As EventArgs)
        ClearValidation()
        Dim selected = SelectedLicence()
        If selected Is Nothing Then
            ShowError("Select an Associate's licence in the list first.")
            Return
        End If
        If Not IsRevoked(selected) Then
            ShowError("This licence is not revoked.")
            Return
        End If
        Await ChangeAsync(Function() _admin.Reinstate(selected.AssociateUserName), "Licence reinstated.")
    End Sub

    ''' <summary>Runs one change on a worker thread, reports a failure in plain words, reloads the list and shows the done message.</summary>
    Private Async Function ChangeAsync(change As Func(Of OperationResult), doneMessage As String) As Task
        Dim result As OperationResult = Nothing
        Try
            Await RunBusyAsync(Function()
                                   Return Task.Run(Sub()
                                                       result = change()
                                                   End Sub)
                               End Function)
        Catch
            ShowError("The change could not be saved. Check the database connection and try again.")
            Return
        End Try
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Await LoadAsync()
        ShowStatus(doneMessage)
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
