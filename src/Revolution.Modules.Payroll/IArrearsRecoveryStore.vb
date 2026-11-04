''' <summary>
''' Overpaid arrears claims to be recovered through the regular run (Owner, 2026-11-03). PendingByEmployee is what each employee still owes, passed to the run as other deductions.
''' Apply marks the covered claims as recovered by the run (called at Lock, safe to repeat); Reverse puts them back to open (called at Reopen).
''' </summary>
Public Interface IArrearsRecoveryStore
    Function PendingByEmployee() As IReadOnlyDictionary(Of Integer, Decimal)
    Sub Apply(runId As Integer)
    Sub Reverse(runId As Integer)
End Interface
