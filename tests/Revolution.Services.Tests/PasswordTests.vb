Imports Revolution.Services
Imports Xunit

Public Class PasswordTests
    <Fact>
    Public Sub Hash_VerifiesCorrectPassword_RejectsWrongOne()
        Dim hasher As New PasswordHasher()
        Dim h = hasher.Create("Str0ng!Passw0rd")
        Assert.True(hasher.Verify("Str0ng!Passw0rd", h.Hash, h.Salt, h.Iterations))
        Assert.False(hasher.Verify("Str0ng!Passw0rd2", h.Hash, h.Salt, h.Iterations))
    End Sub

    <Fact>
    Public Sub SamePassword_GetsDifferentSalt()
        Dim hasher As New PasswordHasher()
        Dim a = hasher.Create("Str0ng!Passw0rd")
        Dim b = hasher.Create("Str0ng!Passw0rd")
        Assert.NotEqual(a.Salt, b.Salt)
        Assert.NotEqual(a.Hash, b.Hash)
    End Sub

    <Theory>
    <InlineData("Sh0rt!a")>
    <InlineData("alllowercase1!")>
    <InlineData("ALLUPPERCASE1!")>
    <InlineData("NoDigitsHere!!")>
    <InlineData("NoSymbol12345Ab")>
    <InlineData("")>
    Public Sub Policy_RejectsWeakPasswords(password As String)
        Assert.False(PasswordPolicy.Validate(password).Succeeded)
    End Sub

    <Fact>
    Public Sub Policy_AcceptsStrongPassword()
        Assert.True(PasswordPolicy.Validate("Str0ng!Passw0rd").Succeeded)
    End Sub
End Class
