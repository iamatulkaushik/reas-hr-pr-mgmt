Imports Revolution.Modules.Payroll

Friend NotInheritable Class FakeRecoveryLedger
    Implements IArrearsRecoveryLedger

    Public ReadOnly DueRows As New List(Of ArrearsRecoveryDue)()
    Public ReadOnly Held As New Dictionary(Of Integer, List(Of Integer))()
    Public ReleasedRuns As New List(Of Integer)()
    Public FailReserve As String
    Public FailRelease As String
    Public FailDue As String

    Public Function Due(runId As Integer) As IReadOnlyList(Of ArrearsRecoveryDue) Implements IArrearsRecoveryLedger.Due
        If FailDue IsNot Nothing Then Throw New PayrollRuleException(FailDue, 53110, Nothing)
        Return DueRows
    End Function

    Public Sub Reserve(runId As Integer, claimIds As IReadOnlyCollection(Of Integer)) Implements IArrearsRecoveryLedger.Reserve
        If FailReserve IsNot Nothing Then Throw New PayrollRuleException(FailReserve, 53110, Nothing)
        Held(runId) = claimIds.ToList()
    End Sub

    Public Sub Release(runId As Integer) Implements IArrearsRecoveryLedger.Release
        If FailRelease IsNot Nothing Then Throw New PayrollRuleException(FailRelease, 53111, Nothing)
        ReleasedRuns.Add(runId)
        Held.Remove(runId)
    End Sub
End Class

Friend NotInheritable Class FakeRunDirectory
    Implements IRunDirectory

    Public ReadOnly Runs As New List(Of PayrollRunHeader)()
    Public LastPeriod As RunPeriod

    Public Function ListForPeriod(period As RunPeriod) As IReadOnlyList(Of PayrollRunHeader) Implements IRunDirectory.ListForPeriod
        LastPeriod = period
        Return Runs
    End Function
End Class
