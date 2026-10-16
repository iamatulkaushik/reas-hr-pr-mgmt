''' <summary>A licence file as stored for an Associate. The text still has to be verified before anything in it is trusted.</summary>
Public NotInheritable Class StoredLicence
    Public Sub New(associateUserName As String, licenceText As String)
        Me.AssociateUserName = associateUserName
        Me.LicenceText = licenceText
    End Sub

    Public ReadOnly Property AssociateUserName As String
    Public ReadOnly Property LicenceText As String
End Class
