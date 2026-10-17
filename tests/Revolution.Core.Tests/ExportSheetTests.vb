Imports Revolution.Core
Imports Xunit

Public Class ExportSheetTests
    <Fact>
    Public Sub Keeps_NameAndTable()
        Dim table As IReadOnlyList(Of String()) = {New String() {"Header"}, New String() {"cell"}}
        Dim sheet As New ExportSheet("ESIC upload", table)
        Assert.Equal("ESIC upload", sheet.Name)
        Assert.Equal(2, sheet.Table.Count)
    End Sub

    <Theory>
    <InlineData("")>
    <InlineData("   ")>
    <InlineData("0123456789012345678901234567890X")>
    <InlineData("a:b")>
    <InlineData("a/b")>
    <InlineData("a\b")>
    <InlineData("a?b")>
    <InlineData("a*b")>
    <InlineData("[a]")>
    Public Sub Name_FollowsTheXlsRules(name As String)
        Dim table As IReadOnlyList(Of String()) = New List(Of String())()
        Assert.Throws(Of ArgumentException)(Function() New ExportSheet(name, table))
    End Sub

    <Fact>
    Public Sub Name_OfExactly31Characters_IsAccepted()
        Dim table As IReadOnlyList(Of String()) = New List(Of String())()
        Assert.Equal(31, New ExportSheet(New String("a"c, 31), table).Name.Length)
    End Sub

    <Fact>
    Public Sub Table_IsRequired()
        Assert.Throws(Of ArgumentNullException)(Function() New ExportSheet("x", Nothing))
    End Sub
End Class
