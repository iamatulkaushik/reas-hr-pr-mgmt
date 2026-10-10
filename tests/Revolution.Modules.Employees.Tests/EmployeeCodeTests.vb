Imports Revolution.Modules.Employees
Imports Xunit

Public Class EmployeeCodeTests
    <Fact>
    Public Sub Build_PadsToFiveDigits()
        Assert.Equal("EMP-00042", EmployeeCode.Build("emp", 42))
    End Sub

    <Fact>
    Public Sub Build_RejectsZeroSequence()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() EmployeeCode.Build("EMP", 0))
    End Sub

    <Fact>
    Public Sub Build_RejectsBlankPrefix()
        Assert.Throws(Of ArgumentException)(Function() EmployeeCode.Build(" ", 1))
    End Sub

    <Fact>
    Public Sub TryParse_ReadsSequence()
        Dim seq As Integer
        Assert.True(EmployeeCode.TryParseSequence("EMP-00042", "EMP", seq))
        Assert.Equal(42, seq)
    End Sub

    <Theory>
    <InlineData("ABC-00042")>
    <InlineData("EMP-0004X")>
    <InlineData("EMP-00000")>
    <InlineData("EMP--0001")>
    <InlineData("")>
    Public Sub TryParse_RejectsBadCodes(code As String)
        Dim seq As Integer
        Assert.False(EmployeeCode.TryParseSequence(code, "EMP", seq))
    End Sub
End Class
