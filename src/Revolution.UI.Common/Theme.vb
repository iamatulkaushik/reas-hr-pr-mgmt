Imports System.Drawing

''' <summary>The one place for colours, spacing, fonts and corner radius (ADR 0006). Forms never set these themselves. Modern flat light look.</summary>
Public NotInheritable Class Theme
    Private Sub New()
    End Sub

    ' 8 px grid
    Public Const Space1 As Integer = 8
    Public Const Space2 As Integer = 16
    Public Const Space3 As Integer = 24
    Public Const CornerRadius As Integer = 4

    ' Grid sizes at 96 dpi. ThemeStyler scales them to the monitor.
    Public Const RowHeight As Integer = 32
    Public Const HeaderHeight As Integer = 36

    Public Shared ReadOnly Background As Color = Color.FromArgb(245, 247, 250)
    Public Shared ReadOnly Surface As Color = Color.White
    Public Shared ReadOnly SurfaceAlt As Color = Color.FromArgb(241, 245, 249)
    Public Shared ReadOnly Border As Color = Color.FromArgb(203, 213, 225)
    Public Shared ReadOnly BorderSoft As Color = Color.FromArgb(226, 232, 240)
    Public Shared ReadOnly Text As Color = Color.FromArgb(15, 23, 42)
    Public Shared ReadOnly TextMuted As Color = Color.FromArgb(100, 116, 139)
    Public Shared ReadOnly Accent As Color = Color.FromArgb(37, 99, 235)
    Public Shared ReadOnly AccentHover As Color = Color.FromArgb(29, 78, 216)
    Public Shared ReadOnly AccentSoft As Color = Color.FromArgb(219, 234, 254)
    Public Shared ReadOnly AccentText As Color = Color.White
    Public Shared ReadOnly Danger As Color = Color.FromArgb(185, 28, 28)
    Public Shared ReadOnly DangerSoft As Color = Color.FromArgb(254, 242, 242)
    Public Shared ReadOnly Success As Color = Color.FromArgb(21, 128, 61)
    Public Shared ReadOnly GridAltRow As Color = Color.FromArgb(248, 250, 252)
    Public Shared ReadOnly InputFocus As Color = Color.FromArgb(239, 246, 255)

    Public Shared ReadOnly BaseFont As New Font("Segoe UI", 10.0F)
    Public Shared ReadOnly SmallFont As New Font("Segoe UI", 9.0F)
    Public Shared ReadOnly HeaderFont As New Font("Segoe UI Semibold", 10.0F)
    Public Shared ReadOnly TitleFont As New Font("Segoe UI Semibold", 14.0F)
End Class
