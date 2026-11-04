Imports System.Windows.Forms
Imports Revolution.Core

''' <summary>
''' One setting in the company settings screen: its menu entry (title, module, plan feature, so the permission and plan checks are the same as for the menu)
''' and the factory that creates its screen. The screen is a BaseForm that the settings screen shows inside itself.
''' </summary>
Public NotInheritable Class SettingsSection
    Public Sub New(entry As MenuEntry, openScreen As Func(Of Form))
        If entry Is Nothing Then Throw New ArgumentNullException(NameOf(entry))
        If openScreen Is Nothing Then Throw New ArgumentNullException(NameOf(openScreen))
        Me.Entry = entry
        Me.OpenScreen = openScreen
    End Sub

    Public ReadOnly Property Entry As MenuEntry
    ''' <summary>Creates a new, unshown screen (a BaseForm).</summary>
    Public ReadOnly Property OpenScreen As Func(Of Form)

    ''' <summary>The name shown in the list on the left.</summary>
    Public ReadOnly Property Title As String
        Get
            Return Entry.Title
        End Get
    End Property
End Class
