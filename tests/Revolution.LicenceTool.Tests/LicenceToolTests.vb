Imports System.Security.Cryptography
Imports Revolution.Core
Imports Revolution.LicenceTool
Imports Xunit

Public Class LicenceToolTests
    Private Const Password As String = "correct horse battery"

    Private Shared Function TempFile() As String
        Return IO.Path.Combine(IO.Path.GetTempPath(), Guid.NewGuid().ToString("N") & ".key")
    End Function

    <Fact>
    Public Sub KeyFile_RoundTrips_WithTheRightPassword()
        Dim keyFile = TempFile()
        Try
            Dim original = SigningKey.Generate("k1")
            original.Save(keyFile, Password)
            Dim loaded = SigningKey.Load(keyFile, Password)
            Assert.Equal("k1", loaded.KeyId)
            Assert.Equal(original.PublicKeyText, loaded.PublicKeyText)
        Finally
            IO.File.Delete(keyFile)
        End Try
    End Sub

    <Fact>
    Public Sub KeyFile_RefusesAWrongPassword_AndAShortOne()
        Dim keyFile = TempFile()
        Try
            Dim original = SigningKey.Generate("k1")
            original.Save(keyFile, Password)
            Assert.Throws(Of CryptographicException)(Function() SigningKey.Load(keyFile, "another long password"))
            Assert.Throws(Of ArgumentException)(Sub() original.Save(TempFile(), "short"))
        Finally
            IO.File.Delete(keyFile)
        End Try
    End Sub

    <Fact>
    Public Sub IssuedLicence_Verifies_WithDefaultsFromThePlan()
        Dim key = SigningKey.Generate("k1")
        Dim installation = Guid.NewGuid()
        Dim issued = LicenceIssuer.Issue(key, "associate1", installation, PlanTier.Silver, 25, Nothing, {ModuleKey.Employees},
                                         New DateOnly(2026, 11, 1), Nothing, "developer", String.Empty)
        Dim trusted As New Dictionary(Of String, String) From {{"k1", key.PublicKeyText}}
        Dim check = LicenceVerifier.Verify(issued.Text, trusted, installation)
        Assert.True(check.Succeeded)
        Dim grant = check.Grant
        Assert.Equal(15, grant.MaxCompanies)
        Assert.Equal(25, grant.MaxActiveUsers)
        Assert.Equal(New DateOnly(2027, 10, 31), grant.ValidTo)
        Assert.Equal(PlanTier.Silver, grant.PlanCap)
        Assert.True(grant.Includes(ModuleKey.Employees))
        Assert.False(grant.Includes(ModuleKey.Payroll))
        For Each always In {ModuleKey.Security, ModuleKey.Licence, ModuleKey.Audit}
            Assert.True(grant.Includes(always))
        Next
    End Sub

    <Fact>
    Public Sub IssuedLicence_IsRefused_ByAnotherKey()
        Dim key = SigningKey.Generate("k1")
        Dim other = SigningKey.Generate("k1")
        Dim installation = Guid.NewGuid()
        Dim issued = LicenceIssuer.Issue(key, "associate1", installation, PlanTier.Gold, 5, 7, {ModuleKey.Payroll},
                                         New DateOnly(2026, 11, 1), New DateOnly(2027, 1, 31), "developer", "note")
        Dim trusted As New Dictionary(Of String, String) From {{"k1", other.PublicKeyText}}
        Assert.Equal(LicenceProblem.BadSignature, LicenceVerifier.Verify(issued.Text, trusted, installation).Problem)
        Assert.Equal(7, issued.Grant.MaxCompanies)
    End Sub

    <Fact>
    Public Sub PlanQuotas_MatchTheDatabase()
        Assert.Equal(5, LicenceIssuer.PlanQuota(PlanTier.Copper))
        Assert.Equal(15, LicenceIssuer.PlanQuota(PlanTier.Silver))
        Assert.Equal(35, LicenceIssuer.PlanQuota(PlanTier.Gold))
        Assert.Equal(90, LicenceIssuer.PlanQuota(PlanTier.Platinum))
    End Sub

    <Fact>
    Public Sub Issue_RequiresAnInstallationId()
        Dim key = SigningKey.Generate("k1")
        Assert.Throws(Of ArgumentException)(Function() LicenceIssuer.Issue(key, "a", Guid.Empty, PlanTier.Gold, 5, Nothing, {ModuleKey.Payroll},
                                                                           New DateOnly(2026, 11, 1), Nothing, "d", String.Empty))
    End Sub

    <Fact>
    Public Sub Arguments_ParseCommandOptionsAndWords()
        Dim parsed = Arguments.Parse({"ISSUE", "--plan", "Gold", "--users", "3", "extra"})
        Assert.Equal("issue", parsed.Command)
        Assert.Equal("Gold", parsed.Need("plan"))
        Assert.Equal("3", parsed.Find("USERS"))
        Assert.Null(parsed.Find("note"))
        Assert.Equal({"extra"}, parsed.Positional.ToArray())
        Assert.Throws(Of ArgumentException)(Function() parsed.Need("associate"))
        Assert.Throws(Of ArgumentException)(Function() Arguments.Parse({"issue", "--plan"}))
    End Sub
End Class
