Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Colours for the flat menu, toolbar and status bar: white surface, soft blue hover, thin grey lines.</summary>
Friend NotInheritable Class FlatColorTable
    Inherits ProfessionalColorTable

    Public Sub New()
        UseSystemColors = False
    End Sub

    Public Overrides ReadOnly Property MenuStripGradientBegin As Color
        Get
            Return Theme.Surface
        End Get
    End Property

    Public Overrides ReadOnly Property MenuStripGradientEnd As Color
        Get
            Return Theme.Surface
        End Get
    End Property

    Public Overrides ReadOnly Property ToolStripGradientBegin As Color
        Get
            Return Theme.Surface
        End Get
    End Property

    Public Overrides ReadOnly Property ToolStripGradientMiddle As Color
        Get
            Return Theme.Surface
        End Get
    End Property

    Public Overrides ReadOnly Property ToolStripGradientEnd As Color
        Get
            Return Theme.Surface
        End Get
    End Property

    Public Overrides ReadOnly Property ToolStripDropDownBackground As Color
        Get
            Return Theme.Surface
        End Get
    End Property

    Public Overrides ReadOnly Property ImageMarginGradientBegin As Color
        Get
            Return Theme.Surface
        End Get
    End Property

    Public Overrides ReadOnly Property ImageMarginGradientMiddle As Color
        Get
            Return Theme.Surface
        End Get
    End Property

    Public Overrides ReadOnly Property ImageMarginGradientEnd As Color
        Get
            Return Theme.Surface
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemSelected As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemSelectedGradientBegin As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemSelectedGradientEnd As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemPressedGradientBegin As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemPressedGradientMiddle As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemPressedGradientEnd As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property MenuItemBorder As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property MenuBorder As Color
        Get
            Return Theme.BorderSoft
        End Get
    End Property

    Public Overrides ReadOnly Property SeparatorDark As Color
        Get
            Return Theme.BorderSoft
        End Get
    End Property

    Public Overrides ReadOnly Property SeparatorLight As Color
        Get
            Return Theme.Surface
        End Get
    End Property

    Public Overrides ReadOnly Property ButtonSelectedGradientBegin As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property ButtonSelectedGradientMiddle As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property ButtonSelectedGradientEnd As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property ButtonSelectedBorder As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property ButtonPressedGradientBegin As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property ButtonPressedGradientMiddle As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property ButtonPressedGradientEnd As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property ButtonPressedBorder As Color
        Get
            Return Theme.AccentSoft
        End Get
    End Property

    Public Overrides ReadOnly Property StatusStripGradientBegin As Color
        Get
            Return Theme.Surface
        End Get
    End Property

    Public Overrides ReadOnly Property StatusStripGradientEnd As Color
        Get
            Return Theme.Surface
        End Get
    End Property
End Class
