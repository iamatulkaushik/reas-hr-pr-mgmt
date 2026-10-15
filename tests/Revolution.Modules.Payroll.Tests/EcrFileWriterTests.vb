Imports Revolution.Modules.Payroll
Imports Xunit

Public Class EcrFileWriterTests
    Private Const Ceiling As Decimal = 15000D

    Private Shared Function Member(Optional uan As String = "100123456789", Optional name As String = "Ravi Kumar",
                                   Optional gross As Decimal = 18000D, Optional epf As Decimal = 15000D,
                                   Optional eps As Decimal = 15000D, Optional edli As Decimal = 15000D) As EcrMember
        Return New EcrMember(uan, name, gross, epf, eps, edli, 1800D, 1250D, 550D, 2, 0D)
    End Function

    <Fact>
    Public Sub Line_HasElevenFieldsInEpfoOrder()
        Dim parts As String() = EcrFileWriter.Line(Member()).Split({"#~#"}, StringSplitOptions.None)
        Assert.Equal(11, parts.Length)
        Assert.Equal("100123456789", parts(0))
        Assert.Equal("RAVI KUMAR", parts(1))
        Assert.Equal("18000", parts(2))
        Assert.Equal("1800", parts(6))
        Assert.Equal("1250", parts(7))
        Assert.Equal("550", parts(8))
        Assert.Equal("2", parts(9))
        Assert.Equal("0", parts(10))
    End Sub

    <Fact>
    Public Sub Build_OneLinePerMember_NoHeader()
        Dim text As String = EcrFileWriter.Build({Member(), Member("100123456790", "Sita Devi")}, Ceiling)
        Dim lines As String() = text.Split({vbCrLf}, StringSplitOptions.RemoveEmptyEntries)
        Assert.Equal(2, lines.Length)
        Assert.StartsWith("100123456789", lines(0), StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub Name_SpecialCharactersRemoved()
        Assert.Equal("RAVI KUMAR", EcrNameCleaner.Clean("  Ravi   Kumar. "))
        Assert.Equal("ANNEMARIE", EcrNameCleaner.Clean("Anne-Marie"))
    End Sub

    <Fact>
    Public Sub Validate_ShortUan_Fails()
        Assert.NotEmpty(EcrValidator.Validate({Member(uan:="12345")}, Ceiling))
    End Sub

    <Fact>
    Public Sub Validate_EpfAboveGross_Fails()
        Assert.NotEmpty(EcrValidator.Validate({Member(gross:=10000D)}, Ceiling))
    End Sub

    <Fact>
    Public Sub Validate_EpsAboveEpf_Fails()
        Assert.NotEmpty(EcrValidator.Validate({Member(epf:=12000D, eps:=13000D)}, Ceiling))
    End Sub

    <Fact>
    Public Sub Validate_EdliAboveCeiling_Fails()
        Assert.NotEmpty(EcrValidator.Validate({Member(edli:=16000D)}, Ceiling))
    End Sub

    <Fact>
    Public Sub Validate_ValidLine_Passes()
        Assert.Empty(EcrValidator.Validate({Member()}, Ceiling))
    End Sub

    <Fact>
    Public Sub Build_InvalidLine_Throws()
        Assert.Throws(Of InvalidOperationException)(Function() EcrFileWriter.Build({Member(uan:="1")}, Ceiling))
    End Sub

    <Fact>
    Public Sub Build_NoMembers_Throws()
        Assert.Throws(Of InvalidOperationException)(Function() EcrFileWriter.Build(New List(Of EcrMember)(), Ceiling))
    End Sub
End Class
