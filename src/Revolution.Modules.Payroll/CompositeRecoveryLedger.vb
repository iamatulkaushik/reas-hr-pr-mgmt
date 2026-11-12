''' <summary>
''' Lets a regular run take two kinds of recovery through the one ledger the run service knows: overpaid arrears (ids above zero) and lost or damaged assets (the recovery id, negated, so the two id
''' spaces cannot meet). Due lists both, Reserve sends each id to its own ledger, Release releases both. Reserve is all or none: when the asset ledger refuses, the arrears claims just held are put back.
''' </summary>
Public NotInheritable Class CompositeRecoveryLedger
    Implements IArrearsRecoveryLedger

    Private ReadOnly _arrears As IArrearsRecoveryLedger
    Private ReadOnly _assets As IArrearsRecoveryLedger

    Public Sub New(arrears As IArrearsRecoveryLedger, assets As IArrearsRecoveryLedger)
        If arrears Is Nothing Then Throw New ArgumentNullException(NameOf(arrears))
        If assets Is Nothing Then Throw New ArgumentNullException(NameOf(assets))
        _arrears = arrears
        _assets = assets
    End Sub

    Public Function Due(runId As Integer) As IReadOnlyList(Of ArrearsRecoveryDue) Implements IArrearsRecoveryLedger.Due
        Dim all As New List(Of ArrearsRecoveryDue)(_arrears.Due(runId))
        For Each item In _assets.Due(runId)
            all.Add(New ArrearsRecoveryDue(-item.ClaimId, item.EmployeeId, item.EmpCode, item.Amount))
        Next
        Return all
    End Function

    Public Sub Reserve(runId As Integer, claimIds As IReadOnlyCollection(Of Integer)) Implements IArrearsRecoveryLedger.Reserve
        If claimIds Is Nothing Then Throw New ArgumentNullException(NameOf(claimIds))
        Dim claims As List(Of Integer) = claimIds.Where(Function(id) id > 0).ToList()
        Dim recoveries As List(Of Integer) = claimIds.Where(Function(id) id < 0).Select(Function(id) -id).ToList()
        If claims.Count > 0 Then _arrears.Reserve(runId, claims)
        If recoveries.Count = 0 Then Return
        Dim failure As PayrollRuleException = Nothing
        Try
            _assets.Reserve(runId, recoveries)
        Catch ex As PayrollRuleException
            failure = ex
        End Try
        If failure Is Nothing Then Return
        If claims.Count > 0 Then ReleaseArrearsQuietly(runId)
        Throw failure
    End Sub

    ''' <summary>Puts the arrears claims just held back. When the run no longer allows that, the asset failure is still the one reported.</summary>
    Private Sub ReleaseArrearsQuietly(runId As Integer)
        Dim refused As PayrollRuleException = Nothing
        Try
            _arrears.Release(runId)
        Catch ex As PayrollRuleException
            refused = ex
        End Try
    End Sub

    Public Sub Release(runId As Integer) Implements IArrearsRecoveryLedger.Release
        _arrears.Release(runId)
        _assets.Release(runId)
    End Sub
End Class
