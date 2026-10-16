''' <summary>Why a licence was refused (ADR 0018).</summary>
Public Enum LicenceProblem
    None = 0
    ''' <summary>Not a licence: bad text, bad encoding or unreadable content.</summary>
    Malformed = 1
    ''' <summary>Signed with a key id this app does not trust.</summary>
    UnknownKey = 2
    ''' <summary>The content was changed or was not signed by the trusted key.</summary>
    BadSignature = 3
    ''' <summary>Genuine, but issued for a different installation.</summary>
    WrongInstallation = 4
End Enum
