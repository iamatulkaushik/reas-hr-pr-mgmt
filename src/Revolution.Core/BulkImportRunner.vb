''' <summary>
''' Checks and saves a list of rows through a module's IBulkRowHandler (ADR 0025). Row by row: rows that pass are saved, rows that fail stay in the list with a reason,
''' so one bad row never blocks the others. A row listed twice (same RowKey) is refused from the second time. An exception inside the handler becomes a plain message
''' and never leaves the runner, so a cell value cannot reach a log or a message through an exception text.
''' </summary>
Public NotInheritable Class BulkImportRunner
    Private ReadOnly _handler As IBulkRowHandler

    Public Sub New(handler As IBulkRowHandler)
        If handler Is Nothing Then Throw New ArgumentNullException(NameOf(handler))
        _handler = handler
    End Sub

    ''' <summary>Checks every row and saves nothing.</summary>
    Public Function Check(rows As IReadOnlyList(Of ImportRow)) As OperationResult(Of ImportReport)
        Dim problem As String = SizeProblem(rows)
        If problem.Length > 0 Then
            Return OperationResult(Of ImportReport).Fail(problem)
        End If
        Return OperationResult(Of ImportReport).Ok(New ImportReport(Validate(rows)))
    End Function

    ''' <summary>Checks every row, then saves the ones that passed, in order. Progress reports the number of rows done.</summary>
    Public Function Import(rows As IReadOnlyList(Of ImportRow), Optional progress As IProgress(Of Integer) = Nothing) As OperationResult(Of ImportReport)
        Dim problem As String = SizeProblem(rows)
        If problem.Length > 0 Then
            Return OperationResult(Of ImportReport).Fail(problem)
        End If
        Dim results As List(Of ImportRowResult) = Validate(rows)
        For i As Integer = 0 To rows.Count - 1
            If results(i).Outcome = ImportOutcome.Valid Then
                Dim saved As OperationResult(Of String)
                Try
                    saved = _handler.Save(rows(i))
                Catch ex As Exception
                    saved = OperationResult(Of String).Fail("The row could not be saved.")
                End Try
                If saved.Succeeded Then
                    results(i) = New ImportRowResult(rows(i).RowNumber, ImportOutcome.Saved, saved.Value)
                Else
                    results(i) = New ImportRowResult(rows(i).RowNumber, ImportOutcome.Rejected, saved.Message)
                End If
            End If
            If progress IsNot Nothing Then
                progress.Report(i + 1)
            End If
        Next
        Return OperationResult(Of ImportReport).Ok(New ImportReport(results))
    End Function

    Private Shared Function SizeProblem(rows As IReadOnlyList(Of ImportRow)) As String
        If rows Is Nothing OrElse rows.Count = 0 Then
            Return "There are no rows to check."
        End If
        If rows.Count > ImportLimits.MaxRows Then
            Return $"There are {rows.Count} rows. One import takes at most {ImportLimits.MaxRows}. Split the list."
        End If
        Return String.Empty
    End Function

    Private Function Validate(rows As IReadOnlyList(Of ImportRow)) As List(Of ImportRowResult)
        Dim results As New List(Of ImportRowResult)()
        Dim firstSeen As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        For Each row In rows
            Dim problem As String
            Try
                problem = If(_handler.Validate(row), String.Empty)
            Catch ex As Exception
                problem = "The row could not be checked."
            End Try
            If problem.Length = 0 Then
                Dim key As String = String.Empty
                Try
                    key = If(_handler.RowKey(row), String.Empty).Trim()
                Catch ex As Exception
                    key = String.Empty
                End Try
                If key.Length > 0 Then
                    Dim earlier As Integer
                    If firstSeen.TryGetValue(key, earlier) Then
                        problem = $"The same person or item is already in row {earlier} of this list."
                    Else
                        firstSeen.Add(key, row.RowNumber)
                    End If
                End If
            End If
            If problem.Length = 0 Then
                results.Add(New ImportRowResult(row.RowNumber, ImportOutcome.Valid, String.Empty))
            Else
                results.Add(New ImportRowResult(row.RowNumber, ImportOutcome.Rejected, problem))
            End If
        Next
        Return results
    End Function
End Class
