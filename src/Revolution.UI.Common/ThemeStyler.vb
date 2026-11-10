Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Applies the Theme to a form and its controls. BaseForm calls this on load, so forms need no styling code.</summary>
Public NotInheritable Class ThemeStyler
    ''' <summary>Set a control's Tag to this to keep the styler out of it and its children.</summary>
    Public Const SkipTag As String = "theme:skip"

    Private Const DesignDpi As Single = 96.0F

    Private Shared _rendererSet As Boolean

    Private Sub New()
    End Sub

    Public Shared Sub Apply(form As Form)
        UseFlatMenus()
        form.BackColor = Theme.Background
        form.ForeColor = Theme.Text
        form.Font = Theme.BaseFont
        StyleChildren(form, form.AcceptButton)
    End Sub

    ''' <summary>One flat renderer for every menu, toolbar, status bar and drop-down. Set once for the whole program.</summary>
    Private Shared Sub UseFlatMenus()
        If _rendererSet Then
            Return
        End If
        ToolStripManager.Renderer = New FlatToolStripRenderer()
        _rendererSet = True
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
            TintOnFocus(c)
        ElseIf TypeOf c Is NumericUpDown Then
            c.ForeColor = Theme.Text
            TintOnFocus(c)
        ElseIf TypeOf c Is ComboBox Then
            c.BackColor = Theme.Surface
            c.ForeColor = Theme.Text
        ElseIf TypeOf c Is DataGridView Then
            StyleGrid(DirectCast(c, DataGridView))
        ElseIf TypeOf c Is TabPage Then
            c.BackColor = Theme.Surface
        ElseIf TypeOf c Is Label Then
            c.ForeColor = Theme.Text
        ElseIf TypeOf c Is StatusStrip Then
            c.BackColor = Theme.Surface
        ElseIf TypeOf c Is ToolStrip Then
            c.BackColor = Theme.Surface
        End If
    End Sub

    Private Shared Sub TintOnFocus(c As Control)
        AddHandler c.Enter, Sub(sender As Object, e As EventArgs) c.BackColor = Theme.InputFocus
        AddHandler c.Leave, Sub(sender As Object, e As EventArgs) c.BackColor = Theme.Surface
    End Sub

    Private Shared Sub StyleButton(b As Button, isPrimary As Boolean)
        b.FlatStyle = FlatStyle.Flat
        b.UseVisualStyleBackColor = False
        b.Cursor = Cursors.Hand
        b.FlatAppearance.BorderSize = 1
        b.FlatAppearance.BorderColor = If(isPrimary, Theme.Accent, Theme.Border)
        b.FlatAppearance.MouseOverBackColor = If(isPrimary, Theme.AccentHover, Theme.AccentSoft)
        b.FlatAppearance.MouseDownBackColor = If(isPrimary, Theme.AccentHover, Theme.AccentSoft)
        b.BackColor = If(isPrimary, Theme.Accent, Theme.Surface)
        b.ForeColor = If(isPrimary, Theme.AccentText, Theme.Text)
        b.Padding = New Padding(Theme.Space1, 0, Theme.Space1, 0)
        b.MinimumSize = New Size(96, 32)
    End Sub

    ''' <summary>Flat grid: horizontal lines only, light header, soft blue selection, comfortable rows.</summary>
    Private Shared Sub StyleGrid(grid As DataGridView)
        grid.EnableHeadersVisualStyles = False
        grid.BackgroundColor = Theme.Surface
        grid.BorderStyle = BorderStyle.None
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        grid.GridColor = Theme.BorderSoft
        grid.RowTemplate.Height = Scaled(grid, Theme.RowHeight)
        StyleGridHeader(grid)
        StyleGridCells(grid)
    End Sub

    Private Shared Sub StyleGridHeader(grid As DataGridView)
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        grid.ColumnHeadersHeight = Scaled(grid, Theme.HeaderHeight)
        With grid.ColumnHeadersDefaultCellStyle
            .BackColor = Theme.SurfaceAlt
            .ForeColor = Theme.Text
            .SelectionBackColor = Theme.SurfaceAlt
            .SelectionForeColor = Theme.Text
            .Font = Theme.HeaderFont
            .Padding = New Padding(Theme.Space1 \ 2, 0, Theme.Space1 \ 2, 0)
        End With
    End Sub

    Private Shared Sub StyleGridCells(grid As DataGridView)
        With grid.DefaultCellStyle
            .BackColor = Theme.Surface
            .ForeColor = Theme.Text
            .SelectionBackColor = Theme.AccentSoft
            .SelectionForeColor = Theme.Text
            .Padding = New Padding(Theme.Space1 \ 2, 0, Theme.Space1 \ 2, 0)
        End With
        grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.GridAltRow
    End Sub

    Private Shared Function Scaled(c As Control, value As Integer) As Integer
        Return CInt(Math.Round(value * c.DeviceDpi / DesignDpi))
    End Function
End Class
