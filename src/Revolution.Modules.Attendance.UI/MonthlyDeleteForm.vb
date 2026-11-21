Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>Delete saved monthly attendance rows (ADR 0026): pick a month, tick the employees whose row is wrong, Delete. A closed month is refused. Needs the Delete permission.</summary>
Friend NotInheritable Class MonthlyDeleteForm
    Inherits BaseForm

    Private Const FormWidth As Integer = 760
    Private Const FormHeight As Integer = 560
    Private Const MonthFormat As String = "MMMM yyyy"
    Private Const ColPick As String = "colPick"

    Private ReadOnly _services As AttendanceServices
    Private ReadOnly _canDelete As Boolean
    Private ReadOnly _month As New DateTimePicker() With {.Format = DateTimePickerFormat.Custom, .CustomFormat = MonthFormat, .ShowUpDown = True, .MaxDate = Date.Today, .Width = 150}
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _deleteButton As New Button() With {.Text = "Delete ticked", .AutoSize = True}
    Private ReadOnly _closeButton As New Button() With {.Text = "Close", .AutoSize = True, .DialogResult = DialogResult.Cancel}

    Public Sub New(services As AttendanceServices, canDelete As Boolean)
        MyBase.New("Delete attendance", "Remove a wrongly entered month.")
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _services = services
        _canDelete = canDelete
        ClientSize = New Size(FormWidth, FormHeight)
        _month.Value = services.Monthly.DefaultPeriod()
        BuildLayout()
    End Sub

    Private Sub BuildLayout()
        Dim top As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True}
        top.Controls.Add(New Label With {.Text = "Month", .AutoSize = True, .Margin = New Padding(0, 6, 6, 0)})
        top.Controls.Add(_month)
        _grid.Dock = DockStyle.Fill
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        _grid.Columns.Add(New DataGridViewCheckBoxColumn With {.Name = ColPick, .HeaderText = "Delete", .FillWeight = 10.0F})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Code", .FillWeight = 20.0F, .ReadOnly = True})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Employee", .FillWeight = 45.0F, .ReadOnly = True})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Days entered", .FillWeight = 25.0F, .ReadOnly = True})
        Dim bar = CreateButtonBar()
        _deleteButton.Enabled = _canDelete
        bar.Controls.Add(_deleteButton)
        bar.Controls.Add(_closeButton)
        CancelButton = _closeButton
        Content.Controls.Add(_grid)
        Content.Controls.Add(top)
        AddHandler _month.ValueChanged, Sub(sender As Object, e As EventArgs) LoadRows()
        AddHandler _deleteButton.Click, Sub(sender As Object, e As EventArgs) DeleteTicked()
        AddHandler _grid.CurrentCellDirtyStateChanged, Sub(sender As Object, e As EventArgs) CommitTick()
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        LoadRows()
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        LoadRows()
    End Sub

    Private Function Period() As Date
        Return New Date(_month.Value.Year, _month.Value.Month, 1)
    End Function

    Private Sub CommitTick()
        If _grid.IsCurrentCellDirty Then _grid.CommitEdit(DataGridViewDataErrorContexts.Commit)
    End Sub

    Private Sub LoadRows()
        ClearValidation()
        Dim result = _services.Monthly.LoadMonth(Period())
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _grid.Rows.Clear()
        For Each person In result.Value.Where(Function(r) r.HasRow)
            Dim at = _grid.Rows.Add(False, person.EmpCode, person.EmployeeName, $"{person.TotalDays} of {person.WindowDays}")
            _grid.Rows(at).Tag = person.EmployeeId
        Next
        ShowStatus($"{_grid.Rows.Count} saved row(s) in {Period().ToString(MonthFormat, CultureInfo.InvariantCulture)}.")
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
            ShowError("Tick at least one employee to delete.")
            Return
        End If
        Dim question = $"Delete the attendance of {ids.Count} employee(s) for {Period().ToString(MonthFormat, CultureInfo.InvariantCulture)}? This cannot be undone."
        If MessageBox.Show(Me, question, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
        Dim deleted As Integer = 0
        Dim problem As String = String.Empty
        For Each id In ids
            Dim result = _services.Monthly.Delete(id, Period())
            If result.Succeeded Then
                deleted += 1
            Else
                problem = result.Message
                Exit For
            End If
        Next
        LoadRows()
        If problem.Length = 0 Then ShowStatus($"{deleted} row(s) deleted.") Else ShowError(problem)
    End Sub
End Class
