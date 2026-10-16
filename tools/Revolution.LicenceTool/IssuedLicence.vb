''' <summary>A licence the tool has just made: what it grants and the file text.</summary>
Public NotInheritable Class IssuedLicence
    Public Sub New(grant As LicenceGrant, text As String)
        Me.Grant = grant
        Me.Text = text
    End Sub

    Public ReadOnly Property Grant As LicenceGrant
    Public ReadOnly Property Text As String
End Class
