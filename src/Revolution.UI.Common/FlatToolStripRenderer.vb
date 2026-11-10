Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Flat renderer for MenuStrip, ToolStrip, StatusStrip and drop-downs. ThemeStyler sets it once as the program default.</summary>
Friend NotInheritable Class FlatToolStripRenderer
    Inherits ToolStripProfessionalRenderer

    Public Sub New()
        MyBase.New(New FlatColorTable())
        RoundedEdges = False
    End Sub

    Protected Overrides Sub OnRenderToolStripBorder(e As ToolStripRenderEventArgs)
        Dim area As Rectangle = e.ToolStrip.ClientRectangle
        Using linePen As New Pen(Theme.BorderSoft)
            If TypeOf e.ToolStrip Is ToolStripDropDown Then
                e.Graphics.DrawRectangle(linePen, 0, 0, area.Width - 1, area.Height - 1)
            ElseIf TypeOf e.ToolStrip Is StatusStrip Then
                e.Graphics.DrawLine(linePen, 0, 0, area.Width, 0)
            Else
                e.Graphics.DrawLine(linePen, 0, area.Height - 1, area.Width, area.Height - 1)
            End If
        End Using
    End Sub
End Class
