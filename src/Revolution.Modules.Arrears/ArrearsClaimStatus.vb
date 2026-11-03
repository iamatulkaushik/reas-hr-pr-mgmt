''' <summary>Status of an arrears claim, as stored in pay.ArrearsClaim.StatusID. A claim is never deleted: a wrong one is cancelled.</summary>
Public Enum ArrearsClaimStatus
    Open = 1
    Cancelled = 2
    ''' <summary>Paid by an arrears run (RunNo 2 and up). It cannot be cancelled and its months cannot be claimed again.</summary>
    Paid = 3
End Enum
