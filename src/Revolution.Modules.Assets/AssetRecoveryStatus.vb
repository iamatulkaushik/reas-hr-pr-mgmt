''' <summary>State of a recovery. Values match ast.AssetRecovery.StatusID. Recovered means a regular run holds it, or the full and final settlement took it.</summary>
Public Enum AssetRecoveryStatus
    Open = 1
    Cancelled = 2
    Recovered = 3
End Enum
