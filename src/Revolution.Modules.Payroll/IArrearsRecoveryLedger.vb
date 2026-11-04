''' <summary>
''' Overpaid arrears claims waiting to be recovered through a regular run (pay.usp_ArrearsRecovery_*). Optional on PayrollRunService: without it a regular run takes no recoveries.
''' A claim is held (status Recovered) by a regular run from the moment the run is processed, so no second run can take it; it is put back when the run goes back to Draft or is reopened.
''' </summary>
Public Interface IArrearsRecoveryLedger
    ''' <summary>The open overpaid claims, and the claims this run already holds, one row per claim.</summary>
    Function Due(runId As Integer) As IReadOnlyList(Of ArrearsRecoveryDue)

    ''' <summary>Holds the claims for the regular run, all or none. A claim the run already holds counts as held.</summary>
    Sub Reserve(runId As Integer, claimIds As IReadOnlyCollection(Of Integer))

    ''' <summary>Puts every claim the run holds back to Open. Refused by the database once the run is approved, locked or paid.</summary>
    Sub Release(runId As Integer)
End Interface
