Imports System.Windows.Forms
Imports Revolution.UI.Common

''' <summary>The reset user password screen's menu entry. Shown to a user with View on Security: Developer, Owner and Associate (ADR 0022).</summary>
Public NotInheritable Class UserAdminShellModule
    Implements IShellModule

    Private ReadOnly _items As IReadOnlyList(Of ShellItem)

    Public Sub New(admin As IUserPasswordAdminService, session As ISessionContext)
        If admin Is Nothing Then Throw New ArgumentNullException(NameOf(admin))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        _items = New List(Of ShellItem) From {
            New ShellItem(New MenuEntry("security.resetpassword", "Administration", "Reset user password...", ModuleKey.Security, sortOrder:=910),
                          Function() New ResetPasswordForm(admin, If(session.Current Is Nothing, String.Empty, session.Current.UserName)))
        }
    End Sub

    Public ReadOnly Property Items As IReadOnlyList(Of ShellItem) Implements IShellModule.Items
        Get
            Return _items
        End Get
    End Property
End Class
