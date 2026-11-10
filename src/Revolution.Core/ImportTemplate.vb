''' <summary>
''' The columns of one kind of import (for example employees). The same template serves the grid and the file: it names the columns, matches a file's headers to them
''' (any order, any case, aliases allowed, extra columns ignored), and writes the blank template workbook with an instructions sheet (ADR 0025).
''' </summary>
Public NotInheritable Class ImportTemplate
    Public Const InstructionsSheetName As String = "Instructions"

    Public Sub New(name As String, columns As IReadOnlyList(Of ImportColumn))
        If columns Is Nothing OrElse columns.Count = 0 Then
            Throw New ArgumentException("A template needs at least one column.", NameOf(columns))
        End If
        If columns.Count > ImportLimits.MaxColumns Then
            Throw New ArgumentException($"A template has at most {ImportLimits.MaxColumns} columns.", NameOf(columns))
        End If
        ' The sheet name rules are the Excel ones; ExportSheet checks them.
        Dim nameCheck As New ExportSheet(name, New List(Of String())())
        If String.Equals(nameCheck.Name, InstructionsSheetName, StringComparison.OrdinalIgnoreCase) Then
            Throw New ArgumentException("The template cannot be called Instructions.", NameOf(name))
        End If
        Dim seen As New HashSet(Of String)()
        For Each column In columns
            For Each key In column.Keys()
                If Not seen.Add(key) Then
                    Throw New ArgumentException($"Two columns share the name or alias '{column.Name}'.", NameOf(columns))
                End If
            Next
        Next
        Me.Name = nameCheck.Name
        Me.Columns = columns
    End Sub

    Public ReadOnly Property Name As String
    Public ReadOnly Property Columns As IReadOnlyList(Of ImportColumn)

    ''' <summary>Letters and digits only, lower case: "Date of Birth", "date_of_birth" and "DATE OF BIRTH " compare equal.</summary>
    Public Shared Function Normalize(text As String) As String
        If String.IsNullOrEmpty(text) Then
            Return String.Empty
        End If
        Dim kept As New List(Of Char)()
        For Each c As Char In text
            If Char.IsLetterOrDigit(c) Then
                kept.Add(Char.ToLowerInvariant(c))
            End If
        Next
        Return New String(kept.ToArray())
    End Function

    ''' <summary>
    ''' Matches the file's headers to the columns. Fails, with the column names only (never a cell), when a required column is missing or a column appears twice.
    ''' Rows shorter than the header are padded with empty cells.
    ''' </summary>
    Public Function Map(table As ImportTable) As OperationResult(Of MappedImport)
        If table Is Nothing Then
            Throw New ArgumentNullException(NameOf(table))
        End If
        Dim headerKeys As New List(Of String)()
        For Each header In table.Headers
            headerKeys.Add(Normalize(header))
        Next
        Dim position(Columns.Count - 1) As Integer
        Dim claimed As New HashSet(Of Integer)()
        Dim missing As New List(Of String)()
        For i As Integer = 0 To Columns.Count - 1
            position(i) = -1
            Dim keys As IReadOnlyList(Of String) = Columns(i).Keys()
            Dim found As New List(Of Integer)()
            For h As Integer = 0 To headerKeys.Count - 1
                If headerKeys(h).Length > 0 AndAlso keys.Contains(headerKeys(h)) Then
                    found.Add(h)
                End If
            Next
            If found.Count > 1 Then
                Return OperationResult(Of MappedImport).Fail($"The file has more than one column for '{Columns(i).Name}'.")
            End If
            If found.Count = 1 Then
                position(i) = found(0)
                claimed.Add(found(0))
            ElseIf Columns(i).Required Then
                missing.Add(Columns(i).Name)
            End If
        Next
        If missing.Count > 0 Then
            Return OperationResult(Of MappedImport).Fail("The file has no column for: " & String.Join(", ", missing) & ". Download the template to see the columns.")
        End If
        Dim ignored As New List(Of String)()
        For h As Integer = 0 To table.Headers.Count - 1
            If Not claimed.Contains(h) AndAlso Not String.IsNullOrWhiteSpace(table.Headers(h)) Then
                ignored.Add(table.Headers(h).Trim())
            End If
        Next
        Dim rows As New List(Of ImportRow)()
        For Each raw In table.Rows
            Dim values As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            For i As Integer = 0 To Columns.Count - 1
                Dim text As String = String.Empty
                If position(i) >= 0 AndAlso position(i) < raw.Cells.Length AndAlso raw.Cells(position(i)) IsNot Nothing Then
                    text = raw.Cells(position(i)).Trim()
                End If
                values(Columns(i).Name) = text
            Next
            rows.Add(New ImportRow(raw.RowNumber, values))
        Next
        Return OperationResult(Of MappedImport).Ok(New MappedImport(rows, ignored))
    End Function

    ''' <summary>The blank template workbook: a sheet with the header row, and an Instructions sheet that says which columns are required and what to enter.</summary>
    Public Function ToExportSheets() As IReadOnlyList(Of ExportSheet)
        Dim header As New List(Of String)()
        For Each column In Columns
            header.Add(column.Name)
        Next
        Dim data As New List(Of String())()
        data.Add(header.ToArray())
        Dim notes As New List(Of String())()
        notes.Add(New String() {"Column", "Required", "What to enter", "Example"})
        For Each column In Columns
            notes.Add(New String() {column.Name, If(column.Required, "Yes", "No"), column.Description, column.Example})
        Next
        Return New List(Of ExportSheet) From {New ExportSheet(Name, data), New ExportSheet(InstructionsSheetName, notes)}
    End Function
End Class
