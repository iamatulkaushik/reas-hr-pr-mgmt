Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Applies the Theme to a form and its controls. BaseForm calls this on load, so forms need no styling code.</summary>
Public NotInheritable Class ThemeStyler
    ''' <summary>Set a control's Tag to this to keep the styler out of it and its children.</summary>
    Public Const SkipTag As String = "theme:skip"

    Private Sub New()
    End Sub

    Public Shared Sub Apply(form As Form)
        form.BackColor = Theme.Background
        form.ForeColor = Theme.Text
        form.Font = Theme.BaseFont
        StyleChildren(form, form.AcceptButton)
    End Sub

    Private Shared Sub StyleChildren(parent As Control, primary As IButtonControl)
        For Each child As Control In parent.Controls
            If Not Object.Equals(child.Tag, SkipTag) Then
                StyleControl(child, primary)
                StyleChildren(child, primary)
            End If
        Next
    End Sub

    Private Shared Sub StyleControl(c As Control, primary As IButtonControl)
        If TypeOf c Is Button Then
            StyleButton(DirectCast(c, Button), Object.ReferenceEquals(c, primary))
        ElseIf TypeOf c Is TextBoxBase Then
            c.BackColor = Theme.Surface
            c.ForeColor = Theme.Text
            DirectCast(c, TextBoxBase).BorderStyle = BorderStyle.FixedSingle
        ElseIf TypeOf c Is Label Then
            c.ForeColor = Theme.Text
        ElseIf TypeOf c Is StatusStrip Then
            c.BackColor = Theme.SurfaceAlt
        ElseIf TypeOf c Is ToolStrip Then
            c.BackColor = Theme.Surface
        End If
    End Sub

    Private Shared Sub StyleButton(b As Button, isPrimary As Boolean)
        b.FlatStyle = FlatStyle.Flat
        b.UseVisualStyleBackColor = False
        b.FlatAppearance.BorderColor = If(isPrimary, Theme.Accent, Theme.Border)
        b.BackColor = If(isPrimary, Theme.Accent, Theme.Surface)
        b.ForeColor = If(isPrimary, Theme.AccentText, Theme.Text)
        b.Padding = New Padding(Theme.Space1, 0, Theme.Space1, 0)
        b.MinimumSize = New Size(96, 32)
    End Sub
End Class
