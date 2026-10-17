Imports System.Windows.Forms
Imports Revolution.Modules.Employees

''' <summary>The Bank tab: payment mode, bank, IFSC and account number. The full account number is shown only after a confirmed, logged reveal.</summary>
Friend NotInheritable Class EmployeeBankPanel
    Inherits UserControl

    Private Const NoneText As String = "-"
    Private Const MaskPrefix As String = "XXXX"
    Private Const RevealSeconds As Integer = 30
    Private Const Caption As String = "Revolution HRMS"

    Private ReadOnly _services As EmployeesServices
    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _mode As New ComboBox()
    Private ReadOnly _bank As New ComboBox()
    Private ReadOnly _ifsc As New TextBox() With {.MaxLength = 11}
    Private ReadOnly _saved As New Label() With {.AutoSize = True, .Text = NoneText}
    Private ReadOnly _account As New TextBox() With {.MaxLength = 18}
    Private ReadOnly _clearAccount As New CheckBox() With {.Text = "Remove the saved account number", .AutoSize = True}
    Private ReadOnly _hideTimer As New Timer() With {.Interval = RevealSeconds * 1000}
    Private ReadOnly _saveButton As New Button() With {.Text = "Save bank", .AutoSize = True}
    Private ReadOnly _revealButton As New Button() With {.Text = "Show full account", .AutoSize = True}
    Private _employeeId As Integer

    Public Event Notice(text As String, isError As Boolean)

    Public Sub New(services As EmployeesServices, canEdit As Boolean)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _canEdit = canEdit
        BuildLayout()
        AddHandler _hideTimer.Tick, Sub(sender As Object, e As EventArgs) ShowEmployee(_employeeId)
    End Sub

    Private Sub BuildLayout()
        FieldLayout.Bind(_mode, EmployeeLookups.PaymentModes)
        LoadBanks()
        Dim layout = FieldLayout.Create()
        layout.Padding = New Padding(Theme.Space2)
        FieldLayout.Add(layout, "Payment mode", _mode)
        FieldLayout.Add(layout, "Bank", _bank)
        FieldLayout.Add(layout, "IFSC", _ifsc)
        FieldLayout.Add(layout, "Saved account number", _saved)
        FieldLayout.Add(layout, "New account number (leave empty to keep)", _account)
        FieldLayout.Add(layout, " ", _clearAccount)
        Dim bar As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .Padding = New Padding(Theme.Space2)}
        bar.Controls.Add(_saveButton)
        bar.Controls.Add(_revealButton)
        _saveButton.Enabled = _canEdit
        AddHandler _saveButton.Click, Sub(sender As Object, e As EventArgs) SaveBank()
        AddHandler _revealButton.Click, Sub(sender As Object, e As EventArgs) RevealAccount()
        Controls.Add(bar)
        Controls.Add(layout)
    End Sub

    Private Sub LoadBanks()
        Dim pairs As New List(Of KeyValuePair(Of Integer, String))()
        Dim result = _services.Reference.ListBanks()
        If result.Succeeded Then
            For Each item In result.Value
                pairs.Add(New KeyValuePair(Of Integer, String)(item.BankId, item.Name))
            Next
        End If
        FieldLayout.Bind(_bank, FieldLayout.WithBlank(pairs))
    End Sub

    Public Sub ShowEmployee(employeeId As Integer)
        _hideTimer.Stop()
        _employeeId = employeeId
        _account.Clear()
        _clearAccount.Checked = False
        Enabled = employeeId > 0
        _saved.Text = NoneText
        If employeeId <= 0 Then
            _mode.SelectedIndex = 0
            FieldLayout.SelectValue(_bank, Nothing)
            _ifsc.Clear()
            Return
        End If
        Dim result = _services.Bank.GetMasked(employeeId)
        If Not result.Succeeded Then
            RaiseEvent Notice(result.Message, True)
            Return
        End If
        Dim view = result.Value
        _mode.SelectedValue = CInt(view.PaymentMode)
        FieldLayout.SelectValue(_bank, view.BankId)
        _ifsc.Text = view.Ifsc
        _saved.Text = If(view.HasAccount, MaskPrefix & view.AccountLast4, NoneText)
    End Sub

    Private Sub SaveBank()
        If _employeeId <= 0 Then Return
        Dim account As IdEdit = IdEdit.FromText(_account.Text.Trim())
        If _clearAccount.Checked Then account = IdEdit.Cleared
        Dim details As New BankDetails With {
            .PaymentMode = CType(FieldLayout.IntValue(_mode), PaymentMode),
            .BankId = FieldLayout.OptionalValue(_bank),
            .Ifsc = _ifsc.Text.Trim().ToUpperInvariant(),
            .Account = account}
        Dim result = _services.Bank.Save(_employeeId, details)
        If Not result.Succeeded Then
            RaiseEvent Notice(result.Message, True)
            Return
        End If
        ShowEmployee(_employeeId)
        RaiseEvent Notice("Bank details saved.", False)
    End Sub

    Private Sub RevealAccount()
        If _employeeId <= 0 Then Return
        Dim ask As String = $"Show the full account number for {RevealSeconds} seconds? This is recorded in the audit log."
        If MessageBox.Show(Me, ask, Caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
        Dim result = _services.Bank.RevealAccount(_employeeId)
        If Not result.Succeeded Then
            RaiseEvent Notice(result.Message, True)
            Return
        End If
        _saved.Text = If(String.IsNullOrEmpty(result.Value), NoneText, result.Value)
        _hideTimer.Start()
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then _hideTimer.Dispose()
        MyBase.Dispose(disposing)
    End Sub
End Class
