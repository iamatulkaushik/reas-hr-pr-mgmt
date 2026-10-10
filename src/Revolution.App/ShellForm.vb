Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Phase 0 stand-in shell. Replaced by the real main window in Phase 1.</summary>
Public Class ShellForm
    Inherits Form

    Public Sub New()
        Text = "Revolution HRMS"
        StartPosition = FormStartPosition.CenterScreen
        ClientSize = New Size(960, 600)
        Dim banner As New Label With {
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Text = "Revolution HRMS - foundation build"
        }
        Controls.Add(banner)
    End Sub
End Class
