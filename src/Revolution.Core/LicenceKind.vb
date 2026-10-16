''' <summary>Which rules govern the signed-in session (ADR 0018).</summary>
Public Enum LicenceKind
    ''' <summary>The Developer, or nobody signed in: no licence limits apply.</summary>
    NotLimited = 1
    ''' <summary>A genuine licence for the Associate who holds the company.</summary>
    Licensed = 2
    ''' <summary>No usable licence: Copper limits for 28 days from the installation date, then read-only.</summary>
    Trial = 3
End Enum
