Imports System.Drawing
Imports System.Windows.Forms
Imports Revolution.UI.Common

''' <summary>Placeholder main window on BaseForm. Replaced by the real shell when the first module lands.</summary>
Public Class ShellForm
    Inherits BaseForm

    Public Sub New(session As ISessionContext)
        MyBase.New("Revolution HRMS", "Foundation build")
        StartPosition = FormStartPosition.CenterScreen
        ClientSize = New Size(960, 600)
        Dim user = session.Current
        Dim banner As New Label With {
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Text = If(user Is Nothing, "Not signed in", $"Signed in as {user.UserName} ({user.Role}, {user.Plan})")
        }
        Content.Controls.Add(banner)
    End Sub
End Class
