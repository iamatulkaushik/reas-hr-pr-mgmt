Imports System.Windows.Forms
Imports Revolution.Modules.Employees

''' <summary>
''' The IDs tab: UAN, ESI IP, PAN and Aadhaar. The full numbers are never shown unless the user asks and confirms
''' (the service logs every reveal). Leave a box empty to keep the saved number; tick Clear to remove it.
''' </summary>
Friend NotInheritable Class EmployeeIdsPanel
    Inherits UserControl

    Private Const NoneText As String = "-"
    Private Const MaskPrefix As String = "XXXX"
    Private Const RevealSeconds As Integer = 30
    Private Const Caption As String = "Revolution HRMS"

    Private ReadOnly _services As EmployeesServices
    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _current As New Dictionary(Of IdField, Label)()
    Private ReadOnly _typed As New Dictionary(Of IdField, TextBox)()
    Private ReadOnly _clear As New Dictionary(Of IdField, CheckBox)()
    Private ReadOnly _hideTimer As New Timer() With {.Interval = RevealSeconds * 1000}
    Private ReadOnly _saveButton As New Button() With {.Text = "Save IDs", .AutoSize = True}
    Private ReadOnly _revealButton As New Button() With {.Text = "Show full IDs", .AutoSize = True}
    Private _employeeId As Integer

    Public Event Notice(text As String, isError As Boolean)

    Public Sub New(services As EmployeesServices, canEdit As Boolean)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _canEdit = canEdit
        BuildLayout()
        AddHandler _hideTimer.Tick, Sub(sender As Object, e As EventArgs) ShowEmployee(_employeeId)
    End Sub

    Public Sub ShowEmployee(employeeId As Integer)
        _hideTimer.Stop()
        _employeeId = employeeId
        ClearInputs()
        Enabled = employeeId > 0
        If employeeId <= 0 Then
            ShowMasked(New IdentifierLast4())
            Return
        End If
        Dim result = _services.Identifiers.GetMasked(employeeId)
        If result.Succeeded Then
            ShowMasked(result.Value)
        Else
            ShowMasked(New IdentifierLast4())
            RaiseEvent Notice(result.Message, True)
        End If
    End Sub

    Private Sub BuildLayout()
        Dim grid As New TableLayoutPanel With {.ColumnCount = 4, .Dock = DockStyle.Top, .AutoSize = True, .Padding = New Padding(Theme.Space2)}
        grid.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        grid.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        grid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        grid.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        AddHeader(grid)
        AddRow(grid, "UAN", IdField.Uan, 12)
        AddRow(grid, "ESI IP number", IdField.EsiIp, 10)
        AddRow(grid, "PAN", IdField.Pan, 10)
        AddRow(grid, "Aadhaar", IdField.Aadhaar, 12)

        Dim bar As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .FlowDirection = FlowDirection.LeftToRight, .Padding = New Padding(Theme.Space2)}
        bar.Controls.Add(_saveButton)
        bar.Controls.Add(_revealButton)
        _saveButton.Enabled = _canEdit
        AddHandler _saveButton.Click, Sub(sender As Object, e As EventArgs) SaveIds()
        AddHandler _revealButton.Click, Sub(sender As Object, e As EventArgs) RevealIds()
        Controls.Add(bar)
        Controls.Add(grid)
    End Sub

    Private Shared Sub AddHeader(grid As TableLayoutPanel)
        For Each heading As String In New String() {"Number", "Saved", "New value", "Clear"}
            grid.Controls.Add(New Label With {.Text = heading, .AutoSize = True, .ForeColor = Theme.TextMuted, .Margin = New Padding(0, 0, Theme.Space2, 2)})
        Next
    End Sub

    Private Sub AddRow(grid As TableLayoutPanel, caption As String, field As IdField, maxLength As Integer)
        Dim saved As New Label With {.AutoSize = True, .Text = NoneText, .Margin = New Padding(0, 6, Theme.Space2, 0)}
        Dim typed As New TextBox With {.MaxLength = maxLength, .Dock = DockStyle.Fill}
        Dim clear As New CheckBox With {.AutoSize = True}
        _current(field) = saved
        _typed(field) = typed
        _clear(field) = clear
        grid.Controls.Add(New Label With {.Text = caption, .AutoSize = True, .Margin = New Padding(0, 6, Theme.Space2, 0)})
        grid.Controls.Add(saved)
        grid.Controls.Add(typed)
        grid.Controls.Add(clear)
    End Sub

    Private Sub ClearInputs()
        For Each box In _typed.Values
            box.Clear()
        Next
        For Each check In _clear.Values
            check.Checked = False
        Next
    End Sub

    Private Sub ShowMasked(last4 As IdentifierLast4)
        _current(IdField.Uan).Text = Mask(last4.Uan)
        _current(IdField.EsiIp).Text = Mask(last4.EsiIp)
        _current(IdField.Pan).Text = Mask(last4.Pan)
        _current(IdField.Aadhaar).Text = Mask(last4.Aadhaar)
    End Sub

    Private Shared Function Mask(last4 As String) As String
        Return If(String.IsNullOrEmpty(last4), NoneText, MaskPrefix & last4)
    End Function

    Private Function ReadEdit(field As IdField, Optional upperCase As Boolean = False) As IdEdit
        If _clear(field).Checked Then Return IdEdit.Cleared
        Dim text As String = _typed(field).Text.Trim()
        Return IdEdit.FromText(If(upperCase, text.ToUpperInvariant(), text))
    End Function

    Private Sub SaveIds()
        If _employeeId <= 0 Then Return
        Dim input As New IdentifierInput With {
            .Uan = ReadEdit(IdField.Uan),
            .EsiIp = ReadEdit(IdField.EsiIp),
            .Pan = ReadEdit(IdField.Pan, True),
            .Aadhaar = ReadEdit(IdField.Aadhaar)}
        If Not input.HasChanges Then
            RaiseEvent Notice("No ID was changed.", False)
            Return
        End If
        Dim result = _services.Identifiers.Save(_employeeId, input)
        If Not result.Succeeded Then
            RaiseEvent Notice(result.Message, True)
            Return
        End If
        ShowEmployee(_employeeId)
        RaiseEvent Notice("IDs saved.", False)
    End Sub

    Private Sub RevealIds()
        If _employeeId <= 0 Then Return
        Dim ask As String = $"Show the full ID numbers for {RevealSeconds} seconds? This is recorded in the audit log."
        If MessageBox.Show(Me, ask, Caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
        Dim result = _services.Identifiers.Reveal(_employeeId, IdField.All)
        If Not result.Succeeded Then
            RaiseEvent Notice(result.Message, True)
            Return
        End If
        _current(IdField.Uan).Text = ShowFull(result.Value.Uan)
        _current(IdField.EsiIp).Text = ShowFull(result.Value.EsiIp)
        _current(IdField.Pan).Text = ShowFull(result.Value.Pan)
        _current(IdField.Aadhaar).Text = ShowFull(result.Value.Aadhaar)
        _hideTimer.Start()
    End Sub

    Private Shared Function ShowFull(value As String) As String
        Return If(String.IsNullOrEmpty(value), NoneText, value)
    End Function

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then _hideTimer.Dispose()
        MyBase.Dispose(disposing)
    End Sub
End Class
