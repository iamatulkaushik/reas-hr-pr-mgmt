Imports System.Security.Cryptography
Imports Revolution.Core
Imports Xunit

Public Class LicenceTests
    Private Shared ReadOnly Installation As New Guid("11111111-2222-3333-4444-555555555555")

    ''' <summary>Stands in for the Developer's licence tool: holds a key pair and signs licences.</summary>
    Private NotInheritable Class TestIssuer
        Public ReadOnly KeyId As String
        Private ReadOnly _key As ECDsa = ECDsa.Create(ECCurve.NamedCurves.nistP256)

        Public Sub New(keyId As String)
            Me.KeyId = keyId
        End Sub

        Public Function TrustedKeys() As IReadOnlyDictionary(Of String, String)
            Dim parameters = _key.ExportParameters(False)
            Dim raw = parameters.Q.X.Concat(parameters.Q.Y).ToArray()
            Return New Dictionary(Of String, String) From {{KeyId, Convert.ToBase64String(raw)}}
        End Function

        Public Function Issue(grant As LicenceGrant) As String
            Dim payload = LicenceFormat.PayloadBytes(grant)
            Return LicenceFormat.Encode(payload, _key.SignData(payload, HashAlgorithmName.SHA256))
        End Function
    End Class

    Private Shared Function MakeGrant(keyId As String, Optional maxCompanies As Integer = 15, Optional installationId As Guid = Nothing) As LicenceGrant
        Dim target = If(installationId = Guid.Empty, Installation, installationId)
        Return New LicenceGrant(Guid.NewGuid(), keyId, "developer", New DateOnly(2026, 10, 15), target, "associate1",
                                New DateOnly(2026, 11, 1), New DateOnly(2027, 10, 31), PlanTier.Gold,
                                {ModuleKey.Employees, ModuleKey.Payroll, ModuleKey.Attendance}, maxCompanies, 40, "test")
    End Function

    <Fact>
    Public Sub Genuine_Licence_Verifies_AndKeepsEveryField()
        Dim issuer As New TestIssuer("k1")
        Dim grant = MakeGrant("k1")
        Dim check = LicenceVerifier.Verify(issuer.Issue(grant), issuer.TrustedKeys(), Installation)
        Assert.True(check.Succeeded)
        Dim read = check.Grant
        Assert.Equal(grant.LicenceId, read.LicenceId)
        Assert.Equal("associate1", read.Associate)
        Assert.Equal("developer", read.IssuedBy)
        Assert.Equal(PlanTier.Gold, read.PlanCap)
        Assert.Equal(15, read.MaxCompanies)
        Assert.Equal(40, read.MaxActiveUsers)
        Assert.Equal(New DateOnly(2027, 10, 31), read.ValidTo)
        Assert.Equal("test", read.Note)
        Assert.True(read.Includes(ModuleKey.Payroll))
        Assert.False(read.Includes(ModuleKey.Statutory))
    End Sub

    <Fact>
    Public Sub ChangedContent_IsRefused_AsBadSignature()
        Dim issuer As New TestIssuer("k1")
        Dim genuine = issuer.Issue(MakeGrant("k1", 15))
        Dim greedy = LicenceFormat.ToBase64Url(LicenceFormat.PayloadBytes(MakeGrant("k1", 500)))
        Dim forged = greedy & "." & genuine.Split("."c)(1)
        Assert.Equal(LicenceProblem.BadSignature, LicenceVerifier.Verify(forged, issuer.TrustedKeys(), Installation).Problem)
    End Sub

    <Fact>
    Public Sub SignedByAnotherKey_WithTheSameKeyId_IsRefused()
        Dim trusted As New TestIssuer("k1")
        Dim impostor As New TestIssuer("k1")
        Dim check = LicenceVerifier.Verify(impostor.Issue(MakeGrant("k1")), trusted.TrustedKeys(), Installation)
        Assert.Equal(LicenceProblem.BadSignature, check.Problem)
    End Sub

    <Fact>
    Public Sub UnknownKeyId_IsRefused()
        Dim issuer As New TestIssuer("k1")
        Dim other As New TestIssuer("k2")
        Dim check = LicenceVerifier.Verify(other.Issue(MakeGrant("k2")), issuer.TrustedKeys(), Installation)
        Assert.Equal(LicenceProblem.UnknownKey, check.Problem)
    End Sub

    <Fact>
    Public Sub OtherInstallation_IsRefused()
        Dim issuer As New TestIssuer("k1")
        Dim text = issuer.Issue(MakeGrant("k1"))
        Dim check = LicenceVerifier.Verify(text, issuer.TrustedKeys(), Guid.NewGuid())
        Assert.Equal(LicenceProblem.WrongInstallation, check.Problem)
    End Sub

    <Theory>
    <InlineData("")>
    <InlineData("   ")>
    <InlineData("no-dot-here")>
    <InlineData("a.b.c")>
    <InlineData("!!!.???")>
    <InlineData("e30.AAAA")>
    Public Sub NotALicence_IsMalformed(text As String)
        Dim issuer As New TestIssuer("k1")
        Assert.Equal(LicenceProblem.Malformed, LicenceVerifier.Verify(text, issuer.TrustedKeys(), Installation).Problem)
    End Sub

    <Fact>
    Public Sub Grant_RejectsBadValues()
        Dim modules = {ModuleKey.Employees}
        Dim on1 = New DateOnly(2026, 11, 1)
        Dim on2 = New DateOnly(2027, 10, 31)
        Assert.Throws(Of ArgumentException)(Function() New LicenceGrant(Guid.NewGuid(), "k", "d", on1, Installation, "a", on2, on1, PlanTier.Gold, modules, 5, 5))
        Assert.Throws(Of ArgumentException)(Function() New LicenceGrant(Guid.NewGuid(), "k", "d", on1, Installation, "a", on1, on2, PlanTier.Gold, modules, 501, 5))
        Assert.Throws(Of ArgumentException)(Function() New LicenceGrant(Guid.NewGuid(), "k", "d", on1, Installation, "a", on1, on2, PlanTier.Gold, modules, 0, 5))
        Assert.Throws(Of ArgumentException)(Function() New LicenceGrant(Guid.NewGuid(), "k", "d", on1, Installation, "a", on1, on2, PlanTier.Gold, modules, 5, 0))
        Assert.Throws(Of ArgumentException)(Function() New LicenceGrant(Guid.NewGuid(), "k", "d", on1, Installation, "a", on1, on2, PlanTier.Gold, New ModuleKey() {}, 5, 5))
        Assert.Throws(Of ArgumentException)(Function() New LicenceGrant(Guid.NewGuid(), " ", "d", on1, Installation, "a", on1, on2, PlanTier.Gold, modules, 5, 5))
    End Sub

    <Theory>
    <InlineData(300, LicenceState.Valid)>
    <InlineData(31, LicenceState.Valid)>
    <InlineData(30, LicenceState.Expiring)>
    <InlineData(1, LicenceState.Expiring)>
    <InlineData(0, LicenceState.Expiring)>
    <InlineData(-1, LicenceState.Grace)>
    <InlineData(-15, LicenceState.Grace)>
    <InlineData(-16, LicenceState.Expired)>
    <InlineData(-500, LicenceState.Expired)>
    Public Sub State_FollowsDaysLeft(daysLeft As Integer, expected As LicenceState)
        Dim grant = MakeGrant("k1")
        Dim today = grant.ValidTo.AddDays(-daysLeft)
        Assert.Equal(daysLeft, LicenceEvaluator.DaysLeft(grant, today))
        Assert.Equal(expected, LicenceEvaluator.StateOn(grant, today))
    End Sub

    <Fact>
    Public Sub State_BeforeTheStartDate_IsNotYetValid()
        Dim grant = MakeGrant("k1")
        Assert.Equal(LicenceState.NotYetValid, LicenceEvaluator.StateOn(grant, grant.ValidFrom.AddDays(-1)))
        Assert.Equal(LicenceState.Valid, LicenceEvaluator.StateOn(grant, grant.ValidFrom))
    End Sub

    <Fact>
    Public Sub Base64Url_RoundTrips_AllLengths()
        For length = 0 To 40
            Dim bytes(length - 1) As Byte
            For i = 0 To bytes.Length - 1
                bytes(i) = CByte((i * 37 + 250) Mod 256)
            Next
            Assert.Equal(bytes, LicenceFormat.FromBase64Url(LicenceFormat.ToBase64Url(bytes)))
        Next
    End Sub
End Class
