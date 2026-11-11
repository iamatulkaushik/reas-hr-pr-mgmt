Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>Delete marked attendance days: pick a day, tick the employees whose mark is wrong, Delete. A closed month is refused. Needs the Delete permission.</summary>
Friend NotInheritable Class DeleteAttendanceForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 760
    Private Const FormHeight As Integer = 560
    Private Const DateFormat As String = "dd-MM-yyyy"
    Private Const ColPick As String = "colPick"

    Private ReadOnly _services As AttendanceServices
    Private ReadOnly _canDelete As Boolean
    Private ReadOnly _day As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = DateFormat, .MaxDate = Date.Today, .Width = 110}
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _deleteButton As New Button() With {.Text = "Delete ticked", .AutoSize = True}
    Private ReadOnly _closeButton As New Button() With {.Text = "Close", .AutoSize = True, .DialogResult = DialogResult.Cancel}

    Public Sub New(services As AttendanceServices, canDelete As Boolean)
        MyBase.New("Delete attendance", "Remove a wrongly marked day.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _canDelete = canDelete
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        Dim top As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True}
        top.Controls.Add(New Label With {.Text = "Day", .AutoSize = True, .Margin = New Padding(0, 6, 6, 0)})
        top.Controls.Add(_day)
        _grid.Dock = DockStyle.Fill
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        _grid.Columns.Add(New DataGridViewCheckBoxColumn With {.Name = ColPick, .HeaderText = "Delete", .FillWeight = 10.0F})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Code", .FillWeight = 20.0F, .ReadOnly = True})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Employee", .FillWeight = 50.0F, .ReadOnly = True})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Marked as", .FillWeight = 20.0F, .ReadOnly = True})
        Dim bar = CreateButtonBar()
        _deleteButton.Enabled = _canDelete
        bar.Controls.Add(_deleteButton)
        bar.Controls.Add(_closeButton)
        CancelButton = _closeButton
        Content.Controls.Add(_grid)
        Content.Controls.Add(top)
        AddHandler _day.ValueChanged, Sub(sender As Object, e As EventArgs) LoadDay()
        AddHandler _deleteButton.Click, Sub(sender As Object, e As EventArgs) DeleteTicked()
        AddHandler _grid.CurrentCellDirtyStateChanged, Sub(sender As Object, e As EventArgs) CommitTick()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadDay()
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        LoadDay()
    End Sub

    Private Sub CommitTick()
        If _grid.IsCurrentCellDirty Then _grid.CommitEdit(DataGridViewDataErrorContexts.Commit)
    End Sub

    Private Sub LoadDay()
        ClearValidation()
        Dim result = _services.Board.LoadDay(_day.Value.Date)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _grid.Rows.Clear()
        For Each person In result.Value.Where(Function(r) r.Status.HasValue)
            Dim at = _grid.Rows.Add(False, person.EmpCode, person.EmployeeName, person.Status.Value.ToString())
            _grid.Rows(at).Tag = person.EmployeeId
        Next
        ShowStatus($"{_grid.Rows.Count} marked day(s).")
    End Sub

    Private Sub DeleteTicked()
        If Not _canDelete Then
            ShowError("You are not allowed to delete attendance.")
            Return
        End If
        ClearValidation()
        Dim ids As New List(Of Integer)()
        For Each row As DataGridViewRow In _grid.Rows
            If Convert.ToBoolean(row.Cells(ColPick).Value, CultureInfo.InvariantCulture) Then ids.Add(Convert.ToInt32(row.Tag, CultureInfo.InvariantCulture))
        Next
        If ids.Count = 0 Then
            ShowError("Tick at least one day to delete.")
            Return
        End If
        Dim question = $"Delete {ids.Count} marked day(s) of {_day.Value.ToString(DateFormat, CultureInfo.InvariantCulture)}? This cannot be undone."
        If MessageBox.Show(Me, question, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
        Dim result = _services.Board.DeleteDays(_day.Value.Date, ids)
        LoadDay()
        If result.Succeeded Then ShowStatus($"{result.Value} day(s) deleted.") Else ShowError(result.Message)
    End Sub
End Class
