''' <summary>
''' The public keys this build trusts to sign licences (ADR 0018): key id to base64 of the 64 bytes X then Y of the P-256 public key.
''' Empty until the Developer runs the licence tool's keygen and pastes the public key here. While it is empty every licence is refused
''' as UnknownKey and the app runs as a trial. Only public keys ever go here.
''' </summary>
Public NotInheritable Class LicenceKeys
    Private Sub New()
    End Sub

    Public Shared ReadOnly Trusted As IReadOnlyDictionary(Of String, String) = New Dictionary(Of String, String)()
End Class
