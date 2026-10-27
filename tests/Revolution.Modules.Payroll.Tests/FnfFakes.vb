Imports Revolution.Modules.Payroll

Friend NotInheritable Class FakeFnfRepository
    Implements IFnfRepository

    Public Settlement As FnfSettlementView
    Public ReadOnly Saved As New List(Of IReadOnlyList(Of FnfLine))()
    Public ReadOnly Changes As New List(Of String)()
    Public FailWith As String

    Public Function Save(separationId As Integer, lines As IReadOnlyList(Of FnfLine)) As Integer Implements IFnfRepository.Save
        ThrowIfFailing()
        Saved.Add(lines)
        Return 42
    End Function

    Public Function [Get](separationId As Integer) As FnfSettlementView Implements IFnfRepository.Get
        Return Settlement
    End Function

    Public Sub ChangeStatus(fnfId As Integer, newStatus As FnfStatus) Implements IFnfRepository.ChangeStatus
        ThrowIfFailing()
        Changes.Add($"{fnfId}:{newStatus}")
    End Sub

    Friend Shared Function Make(status As FnfStatus, makerUserId As Integer?) As FnfSettlementView
        Return New FnfSettlementView(42, 101, 9, New Date(2026, 10, 14), status, New FnfTotals(1000D, 0D), makerUserId, Nothing, New List(Of FnfLine)())
    End Function

    Private Sub ThrowIfFailing()
        If FailWith IsNot Nothing Then Throw New PayrollRuleException(FailWith, 50358, Nothing)
    End Sub
End Class
