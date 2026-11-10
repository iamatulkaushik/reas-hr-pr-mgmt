Imports Revolution.Core
Imports Xunit

Public Class BulkImportRunnerTests
    Private NotInheritable Class FakeHandler
        Implements IBulkRowHandler

        Public ReadOnly Saved As New List(Of Integer)()
        Public FailSaveFor As Integer = -1
        Public ThrowOnSaveFor As Integer = -1
        Public ThrowOnValidateFor As Integer = -1

        Public Function Validate(row As ImportRow) As String Implements IBulkRowHandler.Validate
            If row.RowNumber = ThrowOnValidateFor Then
                Throw New InvalidOperationException("secret 123456789012")
            End If
            Return If(row.Get("Name").Length = 0, "Enter the name.", String.Empty)
        End Function

        Public Function RowKey(row As ImportRow) As String Implements IBulkRowHandler.RowKey
            Return row.Get("Code")
        End Function

        Public Function Save(row As ImportRow) As OperationResult(Of String) Implements IBulkRowHandler.Save
            If row.RowNumber = ThrowOnSaveFor Then
                Throw New InvalidOperationException("secret 123456789012")
            End If
            If row.RowNumber = FailSaveFor Then
                Return OperationResult(Of String).Fail("Already exists.")
            End If
            Saved.Add(row.RowNumber)
            Return OperationResult(Of String).Ok("Code E" & row.RowNumber)
        End Function
    End Class

    Private NotInheritable Class RecordingProgress
        Implements IProgress(Of Integer)

        Public ReadOnly Reports As New List(Of Integer)()

        Public Sub Report(value As Integer) Implements IProgress(Of Integer).Report
            Reports.Add(value)
        End Sub
    End Class

    Private Shared Function Row(number As Integer, name As String, code As String) As ImportRow
        Dim values As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        values("Name") = name
        values("Code") = code
        Return New ImportRow(number, values)
    End Function

    Private Shared Function Template() As ImportTemplate
        Return New ImportTemplate("People", New List(Of ImportColumn) From {New ImportColumn("Name", True, "x"), New ImportColumn("Code", False, "y")})
    End Function

    Private Shared Function FourRows() As List(Of ImportRow)
        Return New List(Of ImportRow) From {Row(1, "Asha", "A1"), Row(2, "", "B2"), Row(3, "Ravi", "A1"), Row(4, "Meena", "C4")}
    End Function

    <Fact>
    Public Sub Check_MarksEachRow_FlagsARepeatedKey_AndSavesNothing()
        Dim handler As New FakeHandler()
        Dim report = New BulkImportRunner(handler).Check(FourRows())
        Assert.True(report.Succeeded)
        Dim results = report.Value.Results
        Assert.Equal(ImportOutcome.Valid, results(0).Outcome)
        Assert.Equal(ImportOutcome.Rejected, results(1).Outcome)
        Assert.Equal("Enter the name.", results(1).Message)
        Assert.Equal(ImportOutcome.Rejected, results(2).Outcome)
        Assert.Contains("row 1", results(2).Message)
        Assert.Equal(ImportOutcome.Valid, results(3).Outcome)
        Assert.Empty(handler.Saved)
        Assert.Equal(2, report.Value.ValidCount)
        Assert.Equal(2, report.Value.RejectedCount)
    End Sub

    <Fact>
    Public Sub Import_SavesOnlyTheRowsThatPass_InOrder_AndReportsProgress()
        Dim handler As New FakeHandler()
        Dim progress As New RecordingProgress()
        Dim report = New BulkImportRunner(handler).Import(FourRows(), progress)
        Assert.True(report.Succeeded)
        Assert.Equal(New Integer() {1, 4}, handler.Saved.ToArray())
        Assert.Equal(2, report.Value.SavedCount)
        Assert.Equal(2, report.Value.RejectedCount)
        Assert.Equal("Code E1", report.Value.Results(0).Message)
        Assert.Equal(New Integer() {1, 2, 3, 4}, progress.Reports.ToArray())
        Assert.Equal("4 rows: 2 saved, 2 rejected.", report.Value.Summary())
    End Sub

    <Fact>
    Public Sub Import_WhenASaveIsRefused_ThatRowIsRejectedWithTheReason_AndTheOthersAreSaved()
        Dim handler As New FakeHandler() With {.FailSaveFor = 4}
        Dim report = New BulkImportRunner(handler).Import(FourRows())
        Assert.Equal(New Integer() {1}, handler.Saved.ToArray())
        Assert.Equal(ImportOutcome.Rejected, report.Value.Results(3).Outcome)
        Assert.Equal("Already exists.", report.Value.Results(3).Message)
    End Sub

    <Fact>
    Public Sub AnExceptionInTheHandler_BecomesAPlainMessage_WithoutItsText()
        Dim onValidate As New FakeHandler() With {.ThrowOnValidateFor = 1}
        Dim checked = New BulkImportRunner(onValidate).Check(FourRows())
        Assert.Equal("The row could not be checked.", checked.Value.Results(0).Message)
        Dim onSave As New FakeHandler() With {.ThrowOnSaveFor = 4}
        Dim saved = New BulkImportRunner(onSave).Import(FourRows())
        Assert.Equal("The row could not be saved.", saved.Value.Results(3).Message)
        For Each result In checked.Value.Results.Concat(saved.Value.Results)
            Assert.DoesNotContain("secret", result.Message)
        Next
    End Sub

    <Fact>
    Public Sub NoRows_OrTooManyRows_FailsBeforeAnythingIsChecked()
        Dim handler As New FakeHandler()
        Dim runner As New BulkImportRunner(handler)
        Assert.False(runner.Check(New List(Of ImportRow)()).Succeeded)
        Dim many As New List(Of ImportRow)()
        For i As Integer = 1 To ImportLimits.MaxRows + 1
            many.Add(Row(i, "Person", "K" & i))
        Next
        Dim tooMany = runner.Import(many)
        Assert.False(tooMany.Succeeded)
        Assert.Contains(ImportLimits.MaxRows.ToString(), tooMany.Message)
        Assert.Empty(handler.Saved)
    End Sub

    <Fact>
    Public Sub ErrorSheet_HoldsOnlyTheRejectedRows_WithTheirValuesAndTheReason()
        Dim rows = FourRows()
        Dim report = New BulkImportRunner(New FakeHandler()).Check(rows).Value
        Dim sheet = report.ToErrorSheet(Template(), rows)
        Assert.Equal("Rejected rows", sheet.Name)
        Assert.Equal(New String() {"Name", "Code", "Error"}, sheet.Table(0))
        Assert.Equal(3, sheet.Table.Count)
        Assert.Equal(New String() {"", "B2", "Enter the name."}, sheet.Table(1))
        Assert.Equal("Ravi", sheet.Table(2)(0))
    End Sub
End Class
