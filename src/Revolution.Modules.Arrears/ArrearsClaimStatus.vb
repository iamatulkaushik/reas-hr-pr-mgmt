''' <summary>Status of an arrears claim, as stored in pay.ArrearsClaim.StatusID. A claim is never deleted: a wrong one is cancelled.</summary>
Public Enum ArrearsClaimStatus
    Open = 1
    Cancelled = 2
End Enum
