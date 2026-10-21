''' <summary>Where a licence stands on a given day (ADR 0018). Dates come from SQL Server, not the PC clock.</summary>
Public Enum LicenceState
    ''' <summary>More than 30 days left.</summary>
    Valid = 1
    ''' <summary>30 days or fewer left: everything works, with a banner.</summary>
    Expiring = 2
    ''' <summary>Up to 15 days past the end date: everything works, with a banner.</summary>
    Grace = 3
    ''' <summary>Past the grace period: read-only, view and print only, no export.</summary>
    Expired = 4
    ''' <summary>The start date has not arrived yet.</summary>
    NotYetValid = 5
    ''' <summary>The Developer revoked the licence: read-only at once, with no grace period (ADR 0018, migration 0025).</summary>
    Revoked = 6
End Enum
