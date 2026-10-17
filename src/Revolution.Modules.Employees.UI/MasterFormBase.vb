Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Standard master screen (ADR 0006): list on the left, editor on the right, Save / New / Close at the bottom.
''' A master form supplies the columns, the editor fields and four actions; this class does the layout, selection and saving.
''' </summary>
Friend MustInherit Class MasterFormBase
    Inherits BaseForm

    Private Const ListWidth As Integer = 440
    Private Const FormWidth As Integer = 960
    Private Const FormHeight As Integer = 560
    Private Const NotAllowedText As String = "You are not allowed to change this master."

    Private ReadOnly _canEdit As Boolean
    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _showInactive As New CheckBox()
    Private ReadOnly _editor As New TableLayoutPanel()
    Private ReadOnly _saveButton As New Button()
    Private ReadOnly _newButton As New Button()
    Private ReadOnly _closeButton As New Button()
    Private _loading As Boolean

    Protected Sub New(title As String, subtitle As String, headers As String(), canEdit As Boolean)
        MyBase.New(title, subtitle)
        _canEdit = canEdit
        ClientSize = New Size(FormWidth, FormHeight)
        BuildLayout(headers)
    End Sub

    ''' <summary>Adds the editor fields to the layout (use AddField). Called once, before the first load.</summary>
    Protected MustOverride Sub BuildEditor(layout As TableLayoutPanel)

    Protected MustOverride Function LoadRows(includeInactive As Boolean) As OperationResult(Of IReadOnlyList(Of MasterRow))

    ''' <summary>Fills the editor from the row with this id.</summary>
    Protected MustOverride Sub ShowRow(id As Integer)

    ''' <summary>Empties the editor for a new row.</summary>
    Protected MustOverride Sub ClearEditor()

    ''' <summary>Saves the editor. Returns the id of the saved row, or the message to show.</summary>
    Protected MustOverride Function SaveEditor() As OperationResult(Of Integer)

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
        _showInactive.Text = "Show inactive"
        _showInactive.AutoSize = True
        _showInactive.Dock = DockStyle.Top
        AddHandler _showInactive.CheckedChanged, Sub(sender As Object, e As EventArgs) Reload(CurrentId())
        listPanel.Controls.Add(_grid)
        listPanel.Controls.Add(_showInactive)

        Content.Controls.Add(details)
        Content.Controls.Add(listPanel)
        Dim bar = CreateButtonBar()
        _saveButton.Text = "Save"
        _newButton.Text = "New"
        _closeButton.Text = "Close"
        _closeButton.DialogResult = DialogResult.Cancel
        _saveButton.Enabled = _canEdit
        _newButton.Enabled = _canEdit
        bar.Controls.Add(_saveButton)
        bar.Controls.Add(_newButton)
        bar.Controls.Add(_closeButton)
        CancelButton = _closeButton
        AddHandler _saveButton.Click, Sub(sender As Object, e As EventArgs) SaveCurrent()
        AddHandler _newButton.Click, Sub(sender As Object, e As EventArgs) StartNew()
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
        Dim result = LoadRows(_showInactive.Checked)
        If Not result.Succeeded Then
            ShowError(result.Message)
            Return
        End If
        _loading = True
        Try
            _grid.Rows.Clear()
            For Each row In result.Value
                Dim at = _grid.Rows.Add(CType(row.Cells, Object()))
                _grid.Rows(at).Tag = row.Id
                If Not row.IsActive Then _grid.Rows(at).DefaultCellStyle.ForeColor = Theme.TextMuted
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
        For Each row As DataGridViewRow In _grid.Rows
            If CInt(row.Tag) = id AndAlso id <> 0 Then
                _grid.CurrentCell = row.Cells(0)
                row.Selected = True
                Return
            End If
        Next
        If _grid.Rows.Count > 0 AndAlso id = 0 Then
            _grid.CurrentCell = _grid.Rows(0).Cells(0)
        End If
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
End Class
