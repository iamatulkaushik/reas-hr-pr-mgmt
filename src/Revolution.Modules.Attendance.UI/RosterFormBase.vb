Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Standard roster screen: list on the left, editor on the right, Save / New / Delete / Close at the bottom.
''' A roster form supplies the columns, the editor fields and the actions; this class does layout, selection and saving.
''' </summary>
Friend MustInherit Class RosterFormBase
    Inherits BaseForm

    Private Const ListWidth As Integer = 460
    Private Const FormWidth As Integer = 960
    Private Const FormHeight As Integer = 560
    Private Const NotAllowedText As String = "You are not allowed to change this."

    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _allowDelete As Boolean
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _topBar As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .AutoSize = True}
    Private ReadOnly _editor As New TableLayoutPanel()
    Private ReadOnly _saveButton As New Button() With {.Text = "Save", .AutoSize = True}
    Private ReadOnly _newButton As New Button() With {.Text = "New", .AutoSize = True}
    Private ReadOnly _deleteButton As New Button() With {.Text = "Delete", .AutoSize = True}
    Private ReadOnly _closeButton As New Button() With {.Text = "Close", .AutoSize = True, .DialogResult = DialogResult.Cancel}
    Private _loading As Boolean

    Protected Sub New(title As String, subtitle As String, headers As String(), canEdit As Boolean, allowDelete As Boolean)
        MyBase.New(title, subtitle)
        _canEdit = canEdit
        _allowDelete = allowDelete
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout(headers)
    End Sub

    ''' <summary>A bar above the list for filters (for example the year). Add controls in BuildEditor.</summary>
    Protected ReadOnly Property TopBar As FlowLayoutPanel
        Get
            Return _topBar
        End Get
    End Property

    Protected MustOverride Sub BuildEditor(layout As TableLayoutPanel)

    Protected MustOverride Function LoadRows() As OperationResult(Of IReadOnlyList(Of RosterRow))

    Protected MustOverride Sub ShowRow(id As Integer)

    Protected MustOverride Sub ClearEditor()

    ''' <summary>Saves the editor. Returns the id of the row to select afterwards (0 for the first row), or the message to show.</summary>
    Protected MustOverride Function SaveEditor() As OperationResult(Of Integer)

    Protected Overridable Function DeleteRow(id As Integer) As OperationResult
        Return OperationResult.Fail("This item cannot be deleted.")
    End Function

    ''' <summary>Reloads the list, keeping the selected row when it still exists.</summary>
    Protected Sub ReloadList()
        Reload(CurrentId())
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        BuildEditor(_editor)
        MyBase.OnLoad(e)
        Reload(0)
    End Sub

    Protected Overrides Sub OnSaveRequested()
        SaveCurrent()
    End Sub

    Protected Overrides Sub OnRefreshRequested()
        Reload(CurrentId())
    End Sub

    Private Sub BuildLayout(headers As String())
        Dim details As New Panel With {.Dock = DockStyle.Fill, .Padding = New Padding(Theme.Space2)}
        _editor.ColumnCount = 1
        _editor.Dock = DockStyle.Top
        _editor.AutoSize = True
        _editor.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
        details.Controls.Add(_editor)

        Dim listPanel As New Panel With {.Dock = DockStyle.Left, .Width = ListWidth}
        ConfigureGrid(headers)
        listPanel.Controls.Add(_grid)
        listPanel.Controls.Add(_topBar)

        Content.Controls.Add(details)
        Content.Controls.Add(listPanel)
        Dim bar = CreateButtonBar()
        _saveButton.Enabled = _canEdit
        _newButton.Enabled = _canEdit
        _deleteButton.Enabled = _canEdit
        bar.Controls.Add(_saveButton)
        bar.Controls.Add(_newButton)
        If _allowDelete Then bar.Controls.Add(_deleteButton)
        bar.Controls.Add(_closeButton)
        CancelButton = _closeButton
        AddHandler _saveButton.Click, Sub(sender As Object, e As EventArgs) SaveCurrent()
        AddHandler _newButton.Click, Sub(sender As Object, e As EventArgs) StartNew()
        AddHandler _deleteButton.Click, Sub(sender As Object, e As EventArgs) DeleteCurrent()
        AddHandler _closeButton.Click, Sub(sender As Object, e As EventArgs) Close()
    End Sub

    Private Sub ConfigureGrid(headers As String())
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.RowHeadersVisible = False
        _grid.MultiSelect = False
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        For Each header In headers
            _grid.Columns.Add(header, header)
        Next
        AddHandler _grid.SelectionChanged, AddressOf OnSelectionChanged
    End Sub

    Private Function CurrentId() As Integer
        If _grid.CurrentRow Is Nothing Then Return 0
        Return CInt(_grid.CurrentRow.Tag)
    End Function

    Private Sub Reload(selectId As Integer)
        ClearValidation()
        Dim result = LoadRows()
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _loading = True
        Try
            _grid.Rows.Clear()
            For Each line In result.Value
                Dim at = _grid.Rows.Add(CType(line.Cells, Object()))
                _grid.Rows(at).Tag = line.Id
            Next
            SelectById(selectId)
        Finally
            _loading = False
        End Try
        ShowCurrent()
        ShowStatus($"{_grid.Rows.Count} row(s).")
    End Sub

    Private Sub SelectById(id As Integer)
        _grid.ClearSelection()
        For Each gridRow As DataGridViewRow In _grid.Rows
            If id <> 0 AndAlso CInt(gridRow.Tag) = id Then
                _grid.CurrentCell = gridRow.Cells(0)
                gridRow.Selected = True
                Return
            End If
        Next
        If id = 0 AndAlso _grid.Rows.Count > 0 Then _grid.CurrentCell = _grid.Rows(0).Cells(0)
    End Sub

    Private Sub ShowCurrent()
        If _grid.CurrentRow Is Nothing Then
            ClearEditor()
        Else
            ShowRow(CInt(_grid.CurrentRow.Tag))
        End If
    End Sub

    Private Sub OnSelectionChanged(sender As Object, e As EventArgs)
        If _loading Then Return
        ClearValidation()
        ShowCurrent()
    End Sub

    Private Sub StartNew()
        ClearValidation()
        _loading = True
        _grid.ClearSelection()
        _grid.CurrentCell = Nothing
        _loading = False
        ClearEditor()
    End Sub

    Private Sub SaveCurrent()
        If Not _canEdit Then
            ShowError(NotAllowedText)
            Return
        End If
        ClearValidation()
        Dim result = SaveEditor()
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        ShowStatus("Saved.")
        Reload(result.Value)
    End Sub

    Private Sub DeleteCurrent()
        If Not _canEdit Then
            ShowError(NotAllowedText)
            Return
        End If
        Dim id As Integer = CurrentId()
        If id = 0 Then
            ShowError("Select a row first.")
            Return
        End If
        If MessageBox.Show(Me, "Delete the selected row?", "Revolution HRMS", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
        Dim result = DeleteRow(id)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        Reload(0)
        ShowStatus("Deleted.")
    End Sub
End Class
