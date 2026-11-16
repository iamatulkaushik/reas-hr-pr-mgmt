''' <summary>
''' What the employee chose for a rent row when the landlord PAN is not given (Form 124). Stored as a number: do not renumber.
''' PanGiven: a PAN is on the row, or the yearly rent does not need one. NoPanDeclared: the landlord has no PAN and the employee holds the
''' landlord's written declaration; the HRA exemption is still allowed. ExemptionNotClaimed: the row is saved without a PAN and no HRA exemption
''' is given for these months (the tax calculation treats the row as if no rent was paid).
''' </summary>
Public Enum TdsLandlordPanOption As Byte
    PanGiven = 0
    NoPanDeclared = 1
    ExemptionNotClaimed = 2
End Enum
