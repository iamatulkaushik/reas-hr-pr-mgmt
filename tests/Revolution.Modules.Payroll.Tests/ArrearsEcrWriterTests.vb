Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Arrear ECR: eight fields, #~# between them, no negative amounts.</summary>
Public Class ArrearsEcrWriterTests
    <Fact>
    Public Sub Line_HasEightFields()
        Dim row As New ArrearsEcrRow("123456789012", "Ram Kumar", 2000D, 2000D, 2000D, 240D, 167D, 73D)
        Assert.Equal("123456789012#~#RAM KUMAR#~#2000#~#2000#~#2000#~#240#~#167#~#73", ArrearsEcrWriter.Line(row))
    End Sub

    <Fact>
    Public Sub NegativeAmount_Refused()
        Dim row As New ArrearsEcrRow("123456789012", "Ram Kumar", -1D, 0D, 0D, 0D, 0D, 0D)
        Assert.Throws(Of InvalidOperationException)(Function() ArrearsEcrWriter.Build(New List(Of ArrearsEcrRow) From {row}))
    End Sub

    <Fact>
    Public Sub ShortUan_Refused()
        Dim row As New ArrearsEcrRow("1234", "Ram Kumar", 1D, 1D, 1D, 1D, 1D, 1D)
        Assert.Throws(Of InvalidOperationException)(Function() ArrearsEcrWriter.Build(New List(Of ArrearsEcrRow) From {row}))
    End Sub

    <Fact>
    Public Sub AllZeroRow_LeftOut()
        Dim row As New ArrearsEcrRow("123456789012", "Ram Kumar", 0D, 0D, 0D, 0D, 0D, 0D)
        Assert.Equal(String.Empty, ArrearsEcrWriter.Build(New List(Of ArrearsEcrRow) From {row}))
    End Sub
End Class
