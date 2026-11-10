''' <summary>The results of checking or importing a list of rows, in row order, with counts and the sheet of rejected rows to correct and load again.</summary>
Public NotInheritable Class ImportReport
    Private ReadOnly _results As IReadOnlyList(Of ImportRowResult)

    Public Sub New(results As IReadOnlyList(Of ImportRowResult))
        If results Is Nothing Then Throw New ArgumentNullException(NameOf(results))
        _results = results
    End Sub

    Public ReadOnly Property Results As IReadOnlyList(Of ImportRowResult)
        Get
            Return _results
        End Get
    End Property

    Public ReadOnly Property Total As Integer
        Get
            Return _results.Count
        End Get
    End Property

    Public ReadOnly Property ValidCount As Integer
        Get
            Return _results.Count(Function(r) r.Outcome = ImportOutcome.Valid)
        End Get
    End Property

    Public ReadOnly Property SavedCount As Integer
        Get
            Return _results.Count(Function(r) r.Outcome = ImportOutcome.Saved)
        End Get
    End Property

    Public ReadOnly Property RejectedCount As Integer
        Get
            Return _results.Count(Function(r) r.Outcome = ImportOutcome.Rejected)
        End Get
    End Property

    ''' <summary>One sentence for the status line, for example "12 rows: 10 saved, 2 rejected."</summary>
    Public Function Summary() As String
        Dim parts As New List(Of String)()
        If ValidCount > 0 Then parts.Add($"{ValidCount} ready")
        If SavedCount > 0 Then parts.Add($"{SavedCount} saved")
        If RejectedCount > 0 Then parts.Add($"{RejectedCount} rejected")
        Return $"{Total} rows: {String.Join(", ", parts)}."
    End Function

    ''' <summary>
    ''' A sheet with the rejected rows (template columns, then Error) to correct and load again. It holds the same values as the rows, including any ids,
    ''' so the caller tells the user to delete the file when done.
    ''' </summary>
    Public Function ToErrorSheet(template As ImportTemplate, rows As IReadOnlyList(Of ImportRow)) As ExportSheet
        If template Is Nothing Then Throw New ArgumentNullException(NameOf(template))
        If rows Is Nothing Then Throw New ArgumentNullException(NameOf(rows))
        Dim table As New List(Of String())()
        Dim header As New List(Of String)()
        For Each column In template.Columns
            header.Add(column.Name)
        Next
        header.Add("Error")
        table.Add(header.ToArray())
        Dim byRow As New Dictionary(Of Integer, ImportRow)()
        For Each row In rows
            byRow(row.RowNumber) = row
        Next
        For Each result In _results
            Dim source As ImportRow = Nothing
            If result.Outcome <> ImportOutcome.Rejected OrElse Not byRow.TryGetValue(result.RowNumber, source) Then
                Continue For
            End If
            Dim cells As New List(Of String)()
            For Each column In template.Columns
                cells.Add(source.Get(column.Name))
            Next
            cells.Add(result.Message)
            table.Add(cells.ToArray())
        Next
        Return New ExportSheet("Rejected rows", table)
    End Function
End Class
