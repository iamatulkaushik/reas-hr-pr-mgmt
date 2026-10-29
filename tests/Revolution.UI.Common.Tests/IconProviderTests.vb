Imports System.Drawing
Imports Revolution.UI.Common
Imports Xunit

Public Class IconProviderTests
    <Fact>
    Public Sub EveryIcon_HasAGlyphAndDrawsSomething()
        For Each icon As AppIcon In [Enum].GetValues(Of AppIcon)()
            Dim bmp As Bitmap = IconProvider.GetBitmap(icon, 32, Color.Black)
            Assert.True(HasInk(bmp), $"{icon} drew nothing.")
        Next
    End Sub

    <Fact>
    Public Sub SameRequest_ReturnsTheCachedBitmap()
        Dim first As Bitmap = IconProvider.GetBitmap(AppIcon.Save, 24, Color.Black)
        Dim second As Bitmap = IconProvider.GetBitmap(AppIcon.Save, 24, Color.Black)
        Assert.Same(first, second)
    End Sub

    <Fact>
    Public Sub DpiOverload_ScalesTheSize()
        Dim bmp As Bitmap = IconProvider.GetBitmap(AppIcon.Save, 20, 144, Color.Black)
        Assert.Equal(30, bmp.Width)
        Assert.Equal(30, bmp.Height)
    End Sub

    <Fact>
    Public Sub NonPositiveSize_IsRejected()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() IconProvider.GetBitmap(AppIcon.Add, 0, Color.Black))
    End Sub

    Private Shared Function HasInk(bmp As Bitmap) As Boolean
        For y As Integer = 0 To bmp.Height - 1
            For x As Integer = 0 To bmp.Width - 1
                If bmp.GetPixel(x, y).A > 0 Then Return True
            Next
        Next
        Return False
    End Function
End Class
