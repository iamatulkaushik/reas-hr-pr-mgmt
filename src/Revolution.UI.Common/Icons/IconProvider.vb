Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.IO
Imports System.Runtime.InteropServices

''' <summary>
''' The one way to get an icon (ADR 0020). Draws the glyph from the embedded icon font as a vector path, scaled to fit,
''' in the colour asked for, and caches the bitmap per icon, size and colour.
''' The returned bitmap is shared: do not dispose it or draw on it.
''' </summary>
Public NotInheritable Class IconProvider
    Private Const ResourceName As String = "Revolution.UI.Common.Infinitude.ttf"
    ''' <summary>Share of the bitmap kept empty on each side.</summary>
    Private Const Padding As Single = 0.06F

    Private Shared ReadOnly Fonts As New PrivateFontCollection()
    Private Shared ReadOnly Family As FontFamily = LoadFamily()
    Private Shared ReadOnly Cache As New Dictionary(Of (AppIcon, Integer, Integer), Bitmap)()

    Private Sub New()
    End Sub

    ''' <param name="sizePx">Edge of the square bitmap in pixels.</param>
    Public Shared Function GetBitmap(icon As AppIcon, sizePx As Integer, color As Color) As Bitmap
        If sizePx <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(sizePx), "The icon size must be positive.")
        Dim key = (icon, sizePx, color.ToArgb())
        SyncLock Cache
            Dim bmp As Bitmap = Nothing
            If Cache.TryGetValue(key, bmp) Then Return bmp
            bmp = Render(icon, sizePx, color)
            Cache(key) = bmp
            Return bmp
        End SyncLock
    End Function

    ''' <param name="sizeDip">Size at 96 dpi. Pass the control's DeviceDpi so the icon stays sharp under PerMonitorV2.</param>
    Public Shared Function GetBitmap(icon As AppIcon, sizeDip As Integer, deviceDpi As Integer, color As Color) As Bitmap
        Return GetBitmap(icon, Math.Max(1, CInt(Math.Round(sizeDip * deviceDpi / 96.0, MidpointRounding.AwayFromZero))), color)
    End Function

    Private Shared Function Render(icon As AppIcon, sizePx As Integer, color As Color) As Bitmap
        Dim glyph As String = IconMap.GlyphFor(icon)
        Dim bmp As New Bitmap(sizePx, sizePx)
        Try
            Using g As Graphics = Graphics.FromImage(bmp)
                Using path As New GraphicsPath(FillMode.Winding)
                    path.AddString(glyph, Family, CInt(FontStyle.Regular), 100.0F, New PointF(0.0F, 0.0F), StringFormat.GenericTypographic)
                    Dim bounds As RectangleF = path.GetBounds()
                    If bounds.Width <= 0.0F OrElse bounds.Height <= 0.0F Then
                        Throw New InvalidOperationException($"The icon font has no outline for {icon}.")
                    End If
                    Dim box As Single = sizePx * (1.0F - 2.0F * Padding)
                    Dim scale As Single = Math.Min(box / bounds.Width, box / bounds.Height)
                    Using m As New Matrix()
                        m.Translate(-bounds.X - bounds.Width / 2.0F, -bounds.Y - bounds.Height / 2.0F)
                        m.Scale(scale, scale, MatrixOrder.Append)
                        m.Translate(sizePx / 2.0F, sizePx / 2.0F, MatrixOrder.Append)
                        path.Transform(m)
                    End Using
                    g.SmoothingMode = SmoothingMode.AntiAlias
                    Using brush As New SolidBrush(color)
                        g.FillPath(brush, path)
                    End Using
                End Using
            End Using
        Catch
            bmp.Dispose()
            Throw
        End Try
        Return bmp
    End Function

    Private Shared Function LoadFamily() As FontFamily
        Using stream As Stream = GetType(IconProvider).Assembly.GetManifestResourceStream(ResourceName)
            If stream Is Nothing Then Throw New InvalidOperationException("The icon font is not embedded in Revolution.UI.Common.")
            Dim data(CInt(stream.Length) - 1) As Byte
            stream.ReadExactly(data, 0, data.Length)
            ' GDI+ keeps reading this memory for the life of the process, so it is never freed.
            Dim ptr As IntPtr = Marshal.AllocCoTaskMem(data.Length)
            Marshal.Copy(data, 0, ptr, data.Length)
            Fonts.AddMemoryFont(ptr, data.Length)
        End Using
        Return Fonts.Families(0)
    End Function
End Class
