Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' One editable list for entering or importing many rows at once (ADR 0025). The same screen serves "Multiple" (type or paste into the list) and "Import"
''' (load a .xlsx, .xls or .csv file into the list, correct what is wrong, then save): the file only fills the list, so every row is fixed in one place.
''' Check shows what is wrong with each row without saving; Save saves the rows that pass and keeps the rest in the list with the reason; Save errors writes the rejected
''' rows to a workbook to correct elsewhere. What a row means, and how it is saved, comes from the module's IBulkRowHandler and ImportTemplate.
''' </summary>
Public NotInheritable Class BulkEntryForm
    Inherits BaseForm

    Private Const ResultColumn As String = "Result"
    Private Const SavedTag As String = "saved"
    Private Const BlankRowsToStart As Integer = 10

    Private ReadOnly _template As ImportTemplate
    Private ReadOnly _runner As BulkImportRunner
    Private ReadOnly _importer As ITableImporter
    Private ReadOnly _exporter As ITableExporter
    Private ReadOnly _startWithFile As Boolean
    Private ReadOnly _grid As New DataGridView()
    Private _loading As Boolean
    Private _lastReport As ImportReport
    Private _lastRows As IReadOnlyList(Of ImportRow)

    ''' <param name="startWithFile">True opens the file chooser as soon as the screen shows (the Import screen); False starts with blank rows (the Multiple screen).</param>
    Public Sub New(title As String, template As ImportTemplate, handler As IBulkRowHandler, importer As ITableImporter, exporter As ITableExporter,
                   startWithFile As Boolean, signedInUserName As String)
        MyBase.New(title, signedInUserName)
        If template Is Nothing Then Throw New ArgumentNullException(NameOf(template))
        If handler Is Nothing Then Throw New ArgumentNullException(NameOf(handler))
        If importer Is Nothing Then Throw New ArgumentNullException(NameOf(importer))
        If exporter Is Nothing Then Throw New ArgumentNullException(NameOf(exporter))
        _template = template
        _runner = New BulkImportRunner(handler)
        _importer = importer
        _exporter = exporter
        _startWithFile = startWithFile
        BuildLayout()
        If Not startWithFile Then
            AddBlankRows(BlankRowsToStart)
        End If
    End Sub

    Private Sub BuildLayout()
        ClientSize = New Size(1180, 660)
        MinimumSize = New Size(900, 480)
        StartPosition = FormStartPosition.CenterParent
        AddButton("Template", AddressOf OnTemplate)
        AddButton("Open file...", AddressOf OnOpenFile)
        If Not _startWithFile Then
            AddButton("Add rows", Sub(sender As Object, e As EventArgs) AddBlankRows(BlankRowsToStart))
        End If
        AddButton("Paste", AddressOf OnPaste)
        AddButton("Delete rows", AddressOf OnDeleteRows)
        Toolbar.Items.Add(New ToolStripSeparator())
        AddButton("Check", AddressOf OnCheck)
        AddButton("Save", AddressOf OnSave)
        AddButton("Save errors...", AddressOf OnSaveErrors)
        Toolbar.Items.Add(New ToolStripSeparator())
        AddButton("Close", Sub(sender As Object, e As EventArgs) Close())

        _grid.Dock = DockStyle.Fill
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.AllowUserToResizeRows = False
        _grid.RowHeadersVisible = True
        _grid.RowHeadersWidth = 56
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect
        _grid.MultiSelect = True
        For Each column In _template.Columns
            Dim gridColumn As New DataGridViewTextBoxColumn()
            gridColumn.Name = column.Name
            gridColumn.HeaderText = If(column.Required, column.Name & " *", column.Name)
            gridColumn.Width = 130
            gridColumn.SortMode = DataGridViewColumnSortMode.NotSortable
            _grid.Columns.Add(gridColumn)
        Next
        Dim result As New DataGridViewTextBoxColumn()
        result.Name = ResultColumn
        result.HeaderText = ResultColumn
        result.Width = 360
        result.ReadOnly = True
        result.SortMode = DataGridViewColumnSortMode.NotSortable
        _grid.Columns.Add(result)
        AddHandler _grid.CellValueChanged, AddressOf OnCellValueChanged
        AddHandler _grid.RowsAdded, Sub(sender As Object, e As DataGridViewRowsAddedEventArgs) Renumber()
        AddHandler _grid.RowsRemoved, Sub(sender As Object, e As DataGridViewRowsRemovedEventArgs) Renumber()

        Dim note As New Label()
        note.Dock = DockStyle.Top
        note.AutoSize = False
        note.Height = 40
        note.ForeColor = Theme.TextMuted
        note.Text = "Columns marked * must be filled. Type or paste (from Excel) into the list, or open a file. Check shows what is wrong without saving; Save saves the rows that are right " &
                    "and keeps the others here. Keep ids as text so leading zeros stay. Row numbers are the rows in this list."
        Content.Controls.Add(note)
        Content.Controls.Add(_grid)
        _grid.BringToFront()
    End Sub

    Private Sub AddButton(text As String, handler As EventHandler)
        Toolbar.Items.Add(New ToolStripButton(text, Nothing, handler))
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        If _startWithFile Then
            OnOpenFile(Me, EventArgs.Empty)
        End If
    End Sub

    Private Sub AddBlankRows(count As Integer)
        _loading = True
        Try
            For i As Integer = 1 To count
                _grid.Rows.Add()
            Next
        Finally
            _loading = False
        End Try
        Renumber()
    End Sub

    Private Sub Renumber()
        If _loading Then
            Return
        End If
        For i As Integer = 0 To _grid.Rows.Count - 1
            _grid.Rows(i).HeaderCell.Value = (i + 1).ToString()
        Next
    End Sub

    ''' <summary>Editing a row makes its old result stale, so the result and the colours are cleared. A saved row cannot be edited.</summary>
    Private Sub OnCellValueChanged(sender As Object, e As DataGridViewCellEventArgs)
        If _loading OrElse e.RowIndex < 0 OrElse e.ColumnIndex >= _template.Columns.Count Then
            Return
        End If
        ClearResult(_grid.Rows(e.RowIndex))
    End Sub

    Private Shared Sub ClearResult(gridRow As DataGridViewRow)
        gridRow.Cells(gridRow.Cells.Count - 1).Value = Nothing
        gridRow.DefaultCellStyle.BackColor = Color.Empty
    End Sub

    Private Sub OnTemplate(sender As Object, e As EventArgs)
        Using dialog As New SaveFileDialog()
            dialog.Filter = "Excel 97-2003 workbook (*.xls)|*.xls"
            dialog.FileName = _template.Name & " template.xls"
            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If
            Dim written = _exporter.WriteXls(dialog.FileName, _template.ToExportSheets())
            If written.Succeeded Then
                ShowStatus("The template is saved. Fill it in and open it here.")
            Else
                ShowError(written.Message)
            End If
        End Using
    End Sub

    Private Async Sub OnOpenFile(sender As Object, e As EventArgs)
        Dim path As String
        Using dialog As New OpenFileDialog()
            dialog.Filter = "Excel and CSV files|*.xlsx;*.xls;*.csv;*.tsv|All files|*.*"
            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If
            path = dialog.FileName
        End Using
        If HasEnteredData() AndAlso MessageBox.Show(Me, "Replace the rows in the list with the rows of the file?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
            Return
        End If
        ClearValidation()
        Dim read As OperationResult(Of ImportTable) = Nothing
        Try
            Await RunBusyAsync(Function() Task.Run(Sub() read = _importer.Read(path)))
        Catch
            ShowError("The file could not be read.")
            Return
        End Try
        If Not read.Succeeded Then
            ShowError(read.Message)
            Return
        End If
        Dim mapped = _template.Map(read.Value)
        If Not mapped.Succeeded Then
            ShowError(mapped.Message)
            Return
        End If
        Fill(mapped.Value.Rows)
        Dim note As String = $"{mapped.Value.Rows.Count} rows loaded. Press Check to see what is wrong."
        If mapped.Value.IgnoredColumns.Count > 0 Then
            note &= " Columns not used: " & String.Join(", ", mapped.Value.IgnoredColumns) & "."
        End If
        ShowStatus(note)
    End Sub

    Private Sub Fill(rows As IReadOnlyList(Of ImportRow))
        _loading = True
        Try
            _grid.Rows.Clear()
            _lastReport = Nothing
            _lastRows = Nothing
            For Each row In rows
                Dim index As Integer = _grid.Rows.Add()
                For c As Integer = 0 To _template.Columns.Count - 1
                    _grid.Rows(index).Cells(c).Value = row.Get(_template.Columns(c).Name)
                Next
            Next
        Finally
            _loading = False
        End Try
        Renumber()
    End Sub

    Private Function HasEnteredData() As Boolean
        For Each gridRow As DataGridViewRow In _grid.Rows
            If RowIsFilled(gridRow) Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Function RowIsFilled(gridRow As DataGridViewRow) As Boolean
        For c As Integer = 0 To _template.Columns.Count - 1
            Dim value = gridRow.Cells(c).Value
            If value IsNot Nothing AndAlso value.ToString().Trim().Length > 0 Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Sub OnPaste(sender As Object, e As EventArgs)
        If Not Clipboard.ContainsText() Then
            ShowStatus("There is no text to paste. Copy cells in Excel first.")
            Return
        End If
        Dim lines As String() = Clipboard.GetText().Replace(vbCrLf, vbLf).Split(New String() {vbLf}, StringSplitOptions.RemoveEmptyEntries)
        If lines.Length = 0 Then
            Return
        End If
        Dim startRow As Integer = If(_grid.CurrentCell Is Nothing, 0, _grid.CurrentCell.RowIndex)
        Dim startColumn As Integer = If(_grid.CurrentCell Is Nothing OrElse _grid.CurrentCell.ColumnIndex >= _template.Columns.Count, 0, _grid.CurrentCell.ColumnIndex)
        _loading = True
        Try
            For r As Integer = 0 To lines.Length - 1
                Dim target As Integer = startRow + r
                While _grid.Rows.Count <= target
                    _grid.Rows.Add()
                End While
                If SavedTag.Equals(CStr(_grid.Rows(target).Tag)) Then
                    Continue For
                End If
                Dim cells As String() = lines(r).Split(ControlChars.Tab)
                For c As Integer = 0 To cells.Length - 1
                    If startColumn + c >= _template.Columns.Count Then
                        Exit For
                    End If
                    _grid.Rows(target).Cells(startColumn + c).Value = cells(c).Trim()
                Next
                ClearResult(_grid.Rows(target))
            Next
        Finally
            _loading = False
        End Try
        Renumber()
        ShowStatus($"{lines.Length} rows pasted.")
    End Sub

    Private Sub OnDeleteRows(sender As Object, e As EventArgs)
        Dim indexes As New SortedSet(Of Integer)()
        For Each cell As DataGridViewCell In _grid.SelectedCells
            indexes.Add(cell.RowIndex)
        Next
        If indexes.Count = 0 Then
            ShowStatus("Select the rows to delete first.")
            Return
        End If
        For Each index In indexes.Reverse()
            _grid.Rows.RemoveAt(index)
        Next
        ShowStatus($"{indexes.Count} rows deleted.")
    End Sub

    ''' <summary>The filled rows that are not saved yet, numbered as the list shows them.</summary>
    Private Function CollectRows() As List(Of ImportRow)
        Dim rows As New List(Of ImportRow)()
        For Each gridRow As DataGridViewRow In _grid.Rows
            If SavedTag.Equals(CStr(gridRow.Tag)) OrElse Not RowIsFilled(gridRow) Then
                Continue For
            End If
            Dim values As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            For c As Integer = 0 To _template.Columns.Count - 1
                Dim value = gridRow.Cells(c).Value
                values(_template.Columns(c).Name) = If(value Is Nothing, String.Empty, value.ToString().Trim())
            Next
            rows.Add(New ImportRow(gridRow.Index + 1, values))
        Next
        Return rows
    End Function

    Private Async Sub OnCheck(sender As Object, e As EventArgs)
        Dim rows = CollectRows()
        If rows.Count = 0 Then
            ShowError("There are no rows to check. Fill in some rows or open a file.")
            Return
        End If
        ClearValidation()
        Dim report As OperationResult(Of ImportReport) = Nothing
        Try
            Await RunBusyAsync(Function() Task.Run(Sub() report = _runner.Check(rows)))
        Catch
            ShowError("The rows could not be checked. Check the database connection and try again.")
            Return
        End Try
        ShowReport(report, rows)
    End Sub

    Private Async Sub OnSave(sender As Object, e As EventArgs)
        Dim rows = CollectRows()
        If rows.Count = 0 Then
            ShowError("There are no rows to save.")
            Return
        End If
        If MessageBox.Show(Me, $"Save {rows.Count} rows now? Rows with problems are not saved and stay in the list.", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
            Return
        End If
        ClearValidation()
        Dim report As OperationResult(Of ImportReport) = Nothing
        Try
            Await RunBusyAsync(Function() Task.Run(Sub() report = _runner.Import(rows)))
        Catch
            ShowError("The rows could not be saved. Check the database connection and try again; rows saved before the failure are marked Saved.")
            Return
        End Try
        ShowReport(report, rows)
    End Sub

    Private Sub ShowReport(report As OperationResult(Of ImportReport), rows As IReadOnlyList(Of ImportRow))
        If Not report.Succeeded Then
            ShowError(report.Message)
            Return
        End If
        _lastReport = report.Value
        _lastRows = rows
        _loading = True
        Try
            For Each result In report.Value.Results
                If result.RowNumber < 1 OrElse result.RowNumber > _grid.Rows.Count Then
                    Continue For
                End If
                Dim gridRow = _grid.Rows(result.RowNumber - 1)
                Dim cell = gridRow.Cells(gridRow.Cells.Count - 1)
                Select Case result.Outcome
                    Case ImportOutcome.Saved
                        cell.Value = If(result.Message.Length = 0, "Saved", "Saved. " & result.Message)
                        cell.Style.ForeColor = Theme.Success
                        gridRow.DefaultCellStyle.BackColor = Color.FromArgb(240, 253, 244)
                        gridRow.Tag = SavedTag
                        gridRow.ReadOnly = True
                    Case ImportOutcome.Rejected
                        cell.Value = result.Message
                        cell.Style.ForeColor = Theme.Danger
                        gridRow.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242)
                    Case Else
                        cell.Value = "OK"
                        cell.Style.ForeColor = Theme.Success
                        gridRow.DefaultCellStyle.BackColor = Color.Empty
                End Select
            Next
        Finally
            _loading = False
        End Try
        ShowStatus(report.Value.Summary())
    End Sub

    Private Sub OnSaveErrors(sender As Object, e As EventArgs)
        If _lastReport Is Nothing OrElse _lastRows Is Nothing OrElse _lastReport.RejectedCount = 0 Then
            ShowStatus("There are no rejected rows to save. Press Check or Save first.")
            Return
        End If
        Using dialog As New SaveFileDialog()
            dialog.Filter = "Excel 97-2003 workbook (*.xls)|*.xls"
            dialog.FileName = "Rejected rows.xls"
            If dialog.ShowDialog(Me) <> DialogResult.OK Then
                Return
            End If
            Dim written = _exporter.WriteXls(dialog.FileName, New List(Of ExportSheet) From {_lastReport.ToErrorSheet(_template, _lastRows)})
            If written.Succeeded Then
                ShowStatus("The rejected rows are saved. The file holds the same ids as your list: delete it when you are done.")
            Else
                ShowError(written.Message)
            End If
        End Using
    End Sub
End Class
