Imports System.Drawing

''' <summary>The one place for colours, spacing, fonts and corner radius (ADR 0006). Forms never set these themselves.</summary>
Public NotInheritable Class Theme
    Private Sub New()
    End Sub

    ' 8 px grid
    Public Const Space1 As Integer = 8
    Public Const Space2 As Integer = 16
    Public Const Space3 As Integer = 24
    Public Const CornerRadius As Integer = 4

    Public Shared ReadOnly Background As Color = Color.FromArgb(245, 246, 248)
    Public Shared ReadOnly Surface As Color = Color.White
    Public Shared ReadOnly SurfaceAlt As Color = Color.FromArgb(236, 239, 243)
    Public Shared ReadOnly Border As Color = Color.FromArgb(200, 205, 213)
    Public Shared ReadOnly Text As Color = Color.FromArgb(31, 41, 55)
    Public Shared ReadOnly TextMuted As Color = Color.FromArgb(107, 114, 128)
    Public Shared ReadOnly Accent As Color = Color.FromArgb(31, 78, 121)
    Public Shared ReadOnly AccentText As Color = Color.White
    Public Shared ReadOnly Danger As Color = Color.FromArgb(185, 28, 28)
    Public Shared ReadOnly Success As Color = Color.FromArgb(22, 101, 52)

    Public Shared ReadOnly BaseFont As New Font("Segoe UI", 10.0F)
    Public Shared ReadOnly SmallFont As New Font("Segoe UI", 9.0F)
    Public Shared ReadOnly TitleFont As New Font("Segoe UI Semibold", 16.0F)
End Class
