''' <summary>The services of the Biometric module that screens use.</summary>
Public NotInheritable Class BiometricServices
    Public Sub New(biometric As BiometricService)
        If biometric Is Nothing Then Throw New ArgumentNullException(NameOf(biometric))
        Me.Biometric = biometric
    End Sub

    Public ReadOnly Property Biometric As BiometricService
End Class
