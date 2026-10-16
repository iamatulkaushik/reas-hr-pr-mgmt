Imports System.Security.Cryptography

''' <summary>
''' Checks a licence file (ADR 0018): the shape, the trusted key, the ECDSA P-256 signature, then the installation id.
''' A public key is written as base64 of the 64 bytes X then Y. Dates are not checked here: LicenceEvaluator does that.
''' </summary>
Public NotInheritable Class LicenceVerifier
    Private Const CoordinateLength As Integer = 32

    Private Sub New()
    End Sub

    Public Shared Function Verify(licenceText As String, trustedKeys As IReadOnlyDictionary(Of String, String),
                                  installationId As Guid) As LicenceCheck
        If trustedKeys Is Nothing Then Throw New ArgumentNullException(NameOf(trustedKeys))
        If String.IsNullOrWhiteSpace(licenceText) Then
            Return LicenceCheck.Fail(LicenceProblem.Malformed, "There is no licence.")
        End If
        Dim parts = licenceText.Trim().Split("."c)
        If parts.Length <> 2 Then
            Return LicenceCheck.Fail(LicenceProblem.Malformed, "This is not a licence file.")
        End If
        Dim payload As Byte() = Nothing
        Dim signature As Byte() = Nothing
        Dim grant As LicenceGrant = Nothing
        Try
            payload = LicenceFormat.FromBase64Url(parts(0))
            signature = LicenceFormat.FromBase64Url(parts(1))
            grant = LicenceFormat.ReadPayload(payload)
        Catch ex As FormatException
            Return LicenceCheck.Fail(LicenceProblem.Malformed, "This is not a licence file.")
        End Try
        Dim keyText As String = Nothing
        If Not trustedKeys.TryGetValue(grant.KeyId, keyText) Then
            Return LicenceCheck.Fail(LicenceProblem.UnknownKey, "This licence was signed with a key this app does not trust.")
        End If
        If Not SignatureIsValid(payload, signature, keyText) Then
            Return LicenceCheck.Fail(LicenceProblem.BadSignature, "The licence has been changed or was not issued by the Developer.")
        End If
        If grant.InstallationId <> installationId Then
            Return LicenceCheck.Fail(LicenceProblem.WrongInstallation, "This licence was issued for a different installation.")
        End If
        Return LicenceCheck.Ok(grant)
    End Function

    Private Shared Function SignatureIsValid(payload As Byte(), signature As Byte(), keyText As String) As Boolean
        Try
            Dim raw = Convert.FromBase64String(keyText)
            If raw.Length <> CoordinateLength * 2 Then
                Return False
            End If
            Dim parameters As New ECParameters() With {.Curve = ECCurve.NamedCurves.nistP256}
            parameters.Q.X = raw.Take(CoordinateLength).ToArray()
            parameters.Q.Y = raw.Skip(CoordinateLength).ToArray()
            Using key = ECDsa.Create()
                key.ImportParameters(parameters)
                Return key.VerifyData(payload, signature, HashAlgorithmName.SHA256)
            End Using
        Catch ex As Exception When TypeOf ex Is CryptographicException OrElse TypeOf ex Is FormatException OrElse TypeOf ex Is ArgumentException
            Return False
        End Try
    End Function
End Class
