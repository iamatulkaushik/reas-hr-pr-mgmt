''' <summary>What a leave ledger row is. The numbers match pay.LeaveLedger.EntryTypeID.</summary>
Public Enum LeaveEntryKind
    Opening = 1
    Accrual = 2
    Taken = 3
    CarryForward = 4
    Encashed = 5
    Lapsed = 6
    Adjustment = 7
End Enum
