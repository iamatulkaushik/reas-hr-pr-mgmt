Imports System.Windows.Forms
Imports Revolution.UI.Common

''' <summary>The Licence screen's menu entry. Shown only to a user with View rights on the Licence module, in practice the Developer (ADR 0018).</summary>
Public NotInheritable Class LicenceShellModule
    Implements IShellModule

    Private ReadOnly _items As IReadOnlyList(Of ShellItem)

    Public Sub New(admin As ILicenceAdminService)
        If admin Is Nothing Then Throw New ArgumentNullException(NameOf(admin))
        _items = New List(Of ShellItem) From {
            New ShellItem(New MenuEntry("licence.manage", "Administration", "Licences...", ModuleKey.Licence, sortOrder:=900),
                          Function() New LicenceForm(admin))
        }
    End Sub

    Public ReadOnly Property Items As IReadOnlyList(Of ShellItem) Implements IShellModule.Items
        Get
            Return _items
        End Get
    End Property
End Class
