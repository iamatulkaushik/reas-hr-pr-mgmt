Imports System.Security.Cryptography
Imports Revolution.Core

''' <summary>Stands in for the Developer's licence tool in tests: holds a key pair and signs licences.</summary>
Public NotInheritable Class LicenceTestIssuer
    Private ReadOnly _key As ECDsa = ECDsa.Create(ECCurve.NamedCurves.nistP256)

    Public Sub New(keyId As String)
        Me.KeyId = keyId
    End Sub

    Public ReadOnly Property KeyId As String

    Public Function TrustedKeys() As IReadOnlyDictionary(Of String, String)
        Dim parameters = _key.ExportParameters(False)
        Dim raw = parameters.Q.X.Concat(parameters.Q.Y).ToArray()
        Return New Dictionary(Of String, String) From {{KeyId, Convert.ToBase64String(raw)}}
    End Function

    Public Function Grant(installationId As Guid, associate As String, validFrom As DateOnly, validTo As DateOnly,
                          plan As PlanTier, ParamArray modules() As ModuleKey) As LicenceGrant
        Return New LicenceGrant(Guid.NewGuid(), KeyId, "developer", validFrom, installationId, associate, validFrom, validTo,
                                plan, modules, 15, 40)
    End Function

    Public Function Issue(licence As LicenceGrant) As String
        Dim payload = LicenceFormat.PayloadBytes(licence)
        Return LicenceFormat.Encode(payload, _key.SignData(payload, HashAlgorithmName.SHA256))
    End Function
End Class
