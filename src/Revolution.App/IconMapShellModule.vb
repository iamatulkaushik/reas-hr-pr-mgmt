Imports System.Windows.Forms
Imports Revolution.UI.Common

''' <summary>The Icon and menu map screen's menu entry, next to Licences. Shown only to a user with View rights on the Licence module, in practice the Developer.</summary>
Public NotInheritable Class IconMapShellModule
    Implements IShellModule

    Private ReadOnly _items As IReadOnlyList(Of ShellItem)

    ''' <param name="menuItems">The menu positions to list, read when the screen opens (the layout module's Items).</param>
    ''' <param name="screens">The screens the feature modules offer, read when the screen opens.</param>
    Public Sub New(menuItems As Func(Of IReadOnlyList(Of ShellItem)), screens As Func(Of IReadOnlyList(Of ShellItem)),
                   permissions As IPermissionService)
        If menuItems Is Nothing Then Throw New ArgumentNullException(NameOf(menuItems))
        If screens Is Nothing Then Throw New ArgumentNullException(NameOf(screens))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        _items = New List(Of ShellItem) From {
            New ShellItem(New MenuEntry("tools.iconmap", "Administration", "Icon and menu map...", ModuleKey.Licence, sortOrder:=910),
                          Function() New IconMapForm(menuItems, screens, permissions))
        }
    End Sub

    Public ReadOnly Property Items As IReadOnlyList(Of ShellItem) Implements IShellModule.Items
        Get
            Return _items
        End Get
    End Property
End Class
