Imports System.Drawing
Imports System.IO
Imports Revolution.UI.Common
Imports Xunit

Public Class AppWindowIconTests
    <Fact>
    Public Sub IcoBytes_AreAnIconFileWithEightPictures()
        Dim data As Byte() = AppWindowIcon.BuildIcoBytes()
        Assert.Equal(0, BitConverter.ToUInt16(data, 0))
        Assert.Equal(1, BitConverter.ToUInt16(data, 2))
        Assert.Equal(8, BitConverter.ToUInt16(data, 4))
    End Sub

    <Fact>
    Public Sub IcoBytes_LoadAsAnIcon()
        Using stream As New MemoryStream(AppWindowIcon.BuildIcoBytes())
            Using loaded As New Icon(stream)
                Assert.True(loaded.Width > 0)
                Assert.True(loaded.Height > 0)
            End Using
        End Using
    End Sub

    <Fact>
    Public Sub WindowIcon_IsBuiltOnceAndShared()
        Assert.NotNull(AppWindowIcon.WindowIcon)
        Assert.Same(AppWindowIcon.WindowIcon, AppWindowIcon.WindowIcon)
    End Sub

    <Fact>
    Public Sub AppLogo_UsesTheBrandGlyph()
        Assert.Equal(&HE003, IconProvider.GlyphCode(AppIcon.AppLogo))
        Assert.Equal(&HF0, IconProvider.GlyphCode(AppIcon.Backup))
    End Sub
End Class
