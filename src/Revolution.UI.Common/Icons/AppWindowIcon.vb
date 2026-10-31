Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO

''' <summary>
''' The window and taskbar icon of every form (ADR 0020): the AppLogo glyph in the accent colour.
''' Built once as an icon with several sizes, so Windows picks a sharp one for the title bar, the taskbar and Alt+Tab.
''' BaseForm sets it on every form; a form never sets its own icon.
''' </summary>
Public NotInheritable Class AppWindowIcon
    Private Shared ReadOnly Sizes As Integer() = {16, 20, 24, 32, 40, 48, 64, 256}
    Private Shared ReadOnly Sync As New Object()
    Private Shared _windowIcon As Icon

    Private Sub New()
    End Sub

    ''' <summary>The shared icon. Do not dispose it.</summary>
    Public Shared ReadOnly Property WindowIcon As Icon
        Get
            SyncLock Sync
                If _windowIcon Is Nothing Then
                    _windowIcon = Build()
                End If
                Return _windowIcon
            End SyncLock
        End Get
    End Property

    ''' <summary>The icon as the bytes of an .ico file (PNG pictures, Windows Vista and later).</summary>
    Public Shared Function BuildIcoBytes() As Byte()
        Dim pictures As New List(Of Byte())()
        For Each edge As Integer In Sizes
            Dim bmp As Bitmap = IconProvider.GetBitmap(AppIcon.AppLogo, edge, Theme.Accent)
            Using png As New MemoryStream()
                bmp.Save(png, ImageFormat.Png)
                pictures.Add(png.ToArray())
            End Using
        Next
        Using output As New MemoryStream()
            Using writer As New BinaryWriter(output)
                writer.Write(CUShort(0))
                writer.Write(CUShort(1))
                writer.Write(CUShort(Sizes.Length))
                Dim offset As Integer = 6 + 16 * Sizes.Length
                For i As Integer = 0 To Sizes.Length - 1
                    Dim edgeByte As Byte = If(Sizes(i) >= 256, CByte(0), CByte(Sizes(i)))
                    writer.Write(edgeByte)
                    writer.Write(edgeByte)
                    writer.Write(CByte(0))
                    writer.Write(CByte(0))
                    writer.Write(CUShort(1))
                    writer.Write(CUShort(32))
                    writer.Write(pictures(i).Length)
                    writer.Write(offset)
                    offset += pictures(i).Length
                Next
                For Each picture As Byte() In pictures
                    writer.Write(picture)
                Next
            End Using
            Return output.ToArray()
        End Using
    End Function

    Private Shared Function Build() As Icon
        Using stream As New MemoryStream(BuildIcoBytes())
            Return New Icon(stream)
        End Using
    End Function
End Class
