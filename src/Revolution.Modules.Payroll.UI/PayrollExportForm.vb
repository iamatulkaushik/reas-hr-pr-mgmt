Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports Revolution.Modules.Payroll

''' <summary>
''' One export screen for the ECR text, the ESIC upload sheet or the bank file (ADR 0019). Pick an approved run, choose where the
''' file goes, then build. The place is asked before anything is built, so ids are never released for a file that has nowhere to go.
''' Plain ids exist only in the built file; nothing here logs, caches or shows them. A failed save removes a half-written file.
''' </summary>
Friend NotInheritable Class PayrollExportForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 780
    Private Const FormHeight As Integer = 660
    Private Const MaxBatchReference As Integer = 11
    Private Const PlainIdWarning As String = "The file holds plain ids (UAN, ESI IP number or account number). Send or upload it securely and delete it afterwards."
    Private Const SaveFailed As String = "The file could not be saved. Check the folder and try again."
    Private Const StampDateFormat As String = "yyyyMM"

    Private ReadOnly _services As PayrollServices
    Private ReadOnly _kind As PayrollFileKind
    Private ReadOnly _canExport As Boolean
    Private ReadOnly _runs As New ComboBox()
    Private ReadOnly _bank As New ComboBox()
    Private ReadOnly _debitAccount As New TextBox()
    Private ReadOnly _branch As New TextBox()
    Private ReadOnly _batch As New TextBox()
    Private ReadOnly _description As New TextBox()
    Private ReadOnly _paymentDate As New DateTimePicker()
    Private ReadOnly _mode As New ComboBox()
    Private ReadOnly _problems As New ListBox()
    Private ReadOnly _buildButton As New Button()
    Private ReadOnly _exitButton As New Button()

    Public Sub New(services As PayrollServices, kind As PayrollFileKind, canExport As Boolean)
        MyBase.New(TitleFor(kind), SubtitleFor(kind))
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _kind = kind
        _canExport = canExport
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Shared Function TitleFor(kind As PayrollFileKind) As String
        Select Case kind
            Case PayrollFileKind.Ecr
                Return "ECR file (EPFO)"
            Case PayrollFileKind.Esic
                Return "ESIC upload sheet"
            Case Else
                Return "Bank file"
        End Select
    End Function

    Private Shared Function SubtitleFor(kind As PayrollFileKind) As String
        Select Case kind
            Case PayrollFileKind.Ecr
                Return "Monthly contribution text for the EPFO portal. EPF members of an approved run."
            Case PayrollFileKind.Esic
                Return "Excel 97-2003 sheet for the ESIC portal. ESI members of an approved run."
            Case Else
                Return "Salary payment files for the bank. Employees paid by bank in an approved run."
        End Select
    End Function

    Private Sub BuildLayout()
        _runs.DropDownStyle = ComboBoxStyle.DropDownList
        _problems.Dock = DockStyle.Fill
        Content.Controls.Add(_problems)

        Dim layout As TableLayoutPanel = CreateFormLayout()
        AddField(layout, "Payroll run", _runs)
        If _kind = PayrollFileKind.BankFile Then
            BuildBankFields(layout)
        End If
        Dim warning As New Label With {
            .Text = PlainIdWarning,
            .AutoSize = True,
            .MaximumSize = New Size(FormWidth - 60, 0),
            .ForeColor = Theme.Danger,
            .Margin = New Padding(0, Theme.Space1, 0, Theme.Space1)}
        layout.Controls.Add(warning)
        layout.Controls.Add(New Label With {.Text = "Employees left out of the file (fix them, then build again)", .AutoSize = True})

        Dim bar As FlowLayoutPanel = CreateButtonBar()
        _buildButton.Text = "Build and save..."
        _buildButton.AutoSize = True
        _exitButton.Text = "Exit"
        _exitButton.DialogResult = DialogResult.Cancel
        bar.Controls.Add(_exitButton)
        bar.Controls.Add(_buildButton)
        CancelButton = _exitButton
        AddHandler _buildButton.Click, Sub(sender As Object, e As EventArgs) BuildAndSave()
    End Sub

    Private Sub BuildBankFields(layout As TableLayoutPanel)
        _bank.DropDownStyle = ComboBoxStyle.DropDownList
        For Each writer As IBankFileWriter In _services.BankWriters
            _bank.Items.Add(writer)
        Next
        _bank.DisplayMember = NameOf(IBankFileWriter.BankName)
        _bank.SelectedIndex = 0
        _mode.DropDownStyle = ComboBoxStyle.DropDownList
        _mode.Items.AddRange(New Object() {"NEFT", "RTGS"})
        _mode.SelectedIndex = 0
        _paymentDate.Format = DateTimePickerFormat.Custom
        _paymentDate.CustomFormat = "dd-MM-yyyy"
        _batch.MaxLength = MaxBatchReference
        _description.MaxLength = 50
        AddField(layout, "Bank", _bank)
        AddField(layout, "Company debit account number", _debitAccount)
        AddField(layout, "Company branch code or IFSC", _branch)
        AddField(layout, $"Batch reference (at most {MaxBatchReference} characters)", _batch)
        AddField(layout, "Description", _description)
        AddField(layout, "Payment date", _paymentDate)
        AddField(layout, "Payment mode", _mode)
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadRuns()
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        LoadRuns()
    End Sub

    Private Sub LoadRuns()
        ClearValidation()
        _problems.Items.Clear()
        _buildButton.Enabled = False
        Dim listed As OperationResult(Of IReadOnlyList(Of PayrollRunItem)) = _services.Files.ListRuns(_kind)
        If Not listed.Succeeded Then
            ShowError(listed.Message)
            Return
        End If
        If listed.Value.Count = 0 Then
            ShowStatus("No approved payroll run yet.")
            Return
        End If
        _runs.DataSource = Nothing
        _runs.DisplayMember = NameOf(PayrollRunItem.Label)
        _runs.DataSource = listed.Value.ToList()
        _buildButton.Enabled = _canExport
        If Not _canExport Then
            ShowError("You can see the runs but you are not allowed to export.")
        End If
    End Sub

    Private Sub BuildAndSave()
        ClearValidation()
        _problems.Items.Clear()
        Dim run As PayrollRunItem = TryCast(_runs.SelectedItem, PayrollRunItem)
        If run Is Nothing Then
            SetFieldError(_runs, "Pick a payroll run.")
            Return
        End If
        Select Case _kind
            Case PayrollFileKind.Ecr
                ExportEcr(run)
            Case PayrollFileKind.Esic
                ExportEsic(run)
            Case Else
                ExportBank(run)
        End Select
    End Sub

    Private Sub ExportEcr(run As PayrollRunItem)
        Dim path As String = AskFile("ECR text file (*.txt)|*.txt", $"ECR_{Stamp(run)}.txt")
        If path Is Nothing Then
            Return
        End If
        Dim rate As OperationResult(Of Revolution.Core.EpfRate) = _services.Rates.GetEpfRate(run.PeriodTo)
        If Not rate.Succeeded Then
            ShowError(rate.Message)
            Return
        End If
        Dim built As OperationResult(Of FileOutput(Of String)) = Nothing
        UseWaitCursor = True
        Try
            built = _services.Files.BuildEcr(run.RunId, rate.Value.WageCeiling)
        Finally
            UseWaitCursor = False
        End Try
        If Not built.Succeeded Then
            ShowError(built.Message)
            Return
        End If
        ShowProblems(built.Value.Problems)
        Dim saved As OperationResult = SaveText(path, built.Value.Content)
        If Not saved.Succeeded Then
            ShowError(saved.Message)
            Return
        End If
        Finished(path, built.Value.Problems.Count)
    End Sub

    Private Sub ExportEsic(run As PayrollRunItem)
        Dim path As String = AskFile("Excel 97-2003 (*.xls)|*.xls", $"ESIC_{Stamp(run)}.xls")
        If path Is Nothing Then
            Return
        End If
        Dim built As OperationResult(Of FileOutput(Of IReadOnlyList(Of ExportSheet))) = Nothing
        UseWaitCursor = True
        Try
            built = _services.Files.BuildEsic(run.RunId)
        Finally
            UseWaitCursor = False
        End Try
        If Not built.Succeeded Then
            ShowError(built.Message)
            Return
        End If
        ShowProblems(built.Value.Problems)
        Dim saved As OperationResult = _services.Exporter.WriteXls(path, built.Value.Content)
        If Not saved.Succeeded Then
            ShowError(saved.Message)
            Return
        End If
        Finished(path, built.Value.Problems.Count)
    End Sub

    Private Sub ExportBank(run As PayrollRunItem)
        Dim problem As String = CheckBankFields()
        If problem.Length > 0 Then
            ShowError(problem)
            Return
        End If
        Dim folder As String = AskFolder()
        If folder Is Nothing Then
            Return
        End If
        Dim beneficiaryPath As String = Path.Combine(folder, $"BANK_{Stamp(run)}_beneficiary.txt")
        Dim transactionPath As String = Path.Combine(folder, $"BANK_{Stamp(run)}_transaction.txt")
        If (File.Exists(beneficiaryPath) OrElse File.Exists(transactionPath)) AndAlso
           MessageBox.Show(Me, "Bank files for this run already exist in that folder. Replace them?", Text,
                           MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then
            Return
        End If

        Dim writer As IBankFileWriter = DirectCast(_bank.SelectedItem, IBankFileWriter)
        Dim mode As PaymentMode = If(_mode.SelectedIndex = 1, PaymentMode.Rtgs, PaymentMode.Neft)
        Dim built As OperationResult(Of FileOutput(Of BankFiles)) = Nothing
        UseWaitCursor = True
        Try
            built = _services.Files.BuildBank(run.RunId, writer, _debitAccount.Text.Trim(), _branch.Text.Trim(),
                                              _paymentDate.Value.Date, _batch.Text.Trim(), _description.Text.Trim(), mode)
        Finally
            UseWaitCursor = False
        End Try
        If Not built.Succeeded Then
            ShowError(built.Message)
            Return
        End If
        ShowProblems(built.Value.Problems)
        Dim savedFirst As OperationResult = SaveText(beneficiaryPath, built.Value.Content.BeneficiaryText)
        Dim saved As OperationResult = If(savedFirst.Succeeded, SaveText(transactionPath, built.Value.Content.TransactionText), savedFirst)
        If Not saved.Succeeded Then
            If savedFirst.Succeeded Then
                TryDelete(beneficiaryPath)
            End If
            ShowError(saved.Message)
            Return
        End If
        Finished(folder, built.Value.Problems.Count)
    End Sub

    Private Function CheckBankFields() As String
        If _debitAccount.Text.Trim().Length = 0 Then
            Return "Enter the company debit account number."
        End If
        If _branch.Text.Trim().Length = 0 Then
            Return "Enter the company branch code or IFSC."
        End If
        If _batch.Text.Trim().Length = 0 OrElse _batch.Text.Trim().Length > MaxBatchReference Then
            Return $"Enter a batch reference of 1 to {MaxBatchReference} characters."
        End If
        If _description.Text.Trim().Length = 0 Then
            Return "Enter a description."
        End If
        Return String.Empty
    End Function

    Private Sub ShowProblems(problems As IReadOnlyList(Of String))
        For Each line As String In problems
            _problems.Items.Add(line)
        Next
    End Sub

    Private Sub Finished(place As String, leftOut As Integer)
        ShowStatus($"Saved {Path.GetFileName(place.TrimEnd(Path.DirectorySeparatorChar))}. Left out: {leftOut.ToString(CultureInfo.InvariantCulture)}.")
        MessageBox.Show(Me, PlainIdWarning, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub

    Private Function AskFile(filter As String, suggested As String) As String
        Using dialog As New SaveFileDialog With {
            .Title = Text, .Filter = filter, .FileName = suggested, .AddExtension = True, .OverwritePrompt = True}
            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return Nothing
            End If
            Return dialog.FileName
        End Using
    End Function

    Private Function AskFolder() As String
        Using dialog As New FolderBrowserDialog With {.Description = "Choose the folder for the bank files.", .UseDescriptionForTitle = True}
            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return Nothing
            End If
            Return dialog.SelectedPath
        End Using
    End Function

    ''' <summary>Same stamp as the service uses for the file name: wage month and run id.</summary>
    Private Shared Function Stamp(run As PayrollRunItem) As String
        Return run.PeriodTo.ToString(StampDateFormat, CultureInfo.InvariantCulture) & "_" & run.RunId.ToString(CultureInfo.InvariantCulture)
    End Function

    ''' <summary>Writes the text without a byte order mark. A failed write removes a file this call created.</summary>
    Private Shared Function SaveText(path As String, text As String) As OperationResult
        Dim existed As Boolean = File.Exists(path)
        Try
            File.WriteAllText(path, text, New UTF8Encoding(False))
            Return OperationResult.Ok()
        Catch ex As IOException
            Return FailedSave(path, existed)
        Catch ex As UnauthorizedAccessException
            Return FailedSave(path, existed)
        End Try
    End Function

    Private Shared Function FailedSave(path As String, existed As Boolean) As OperationResult
        If Not existed Then
            TryDelete(path)
        End If
        Return OperationResult.Fail(SaveFailed)
    End Function

    Private Shared Sub TryDelete(path As String)
        Try
            File.Delete(path)
        Catch ex As IOException
            ' Nothing more can be done; the message already tells the person the save failed.
        Catch ex As UnauthorizedAccessException
            ' Same as above.
        End Try
    End Sub
End Class
