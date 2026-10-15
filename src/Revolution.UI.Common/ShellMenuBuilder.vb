Imports System.Windows.Forms

''' <summary>Builds the module menus for the shell from the registered modules, showing only what the user may open (ADR 0016).</summary>
Public NotInheritable Class ShellMenuBuilder
    Private Sub New()
    End Sub

    ''' <summary>Throws InvalidOperationException when two entries share a key: that is a programming error found at start-up.</summary>
    Public Shared Function Build(modules As IEnumerable(Of IShellModule), permissions As IPermissionService,
                                 entitlements As IEntitlementService, onOpen As Action(Of ShellItem)) As IReadOnlyList(Of ToolStripMenuItem)
        If modules Is Nothing Then Throw New ArgumentNullException(NameOf(modules))
        If onOpen Is Nothing Then Throw New ArgumentNullException(NameOf(onOpen))
        Dim items = modules.SelectMany(Function(m) m.Items).ToList()
        RejectDuplicateKeys(items)
        Dim byKey = items.ToDictionary(Function(item) item.Entry.EntryKey, StringComparer.Ordinal)
        Dim visible = MenuFilter.Visible(items.Select(Function(item) item.Entry), permissions, entitlements)
        Dim tops As New List(Of ToolStripMenuItem)()
        For Each grp In visible.GroupBy(Function(entry) entry.MenuGroup)
            Dim top As New ToolStripMenuItem(grp.Key)
            For Each member In grp
                top.DropDownItems.Add(CreateItem(byKey(member.EntryKey), onOpen))
            Next
            tops.Add(top)
        Next
        Return tops
    End Function

    Private Shared Function CreateItem(item As ShellItem, onOpen As Action(Of ShellItem)) As ToolStripMenuItem
        Dim menuItem As New ToolStripMenuItem(item.Entry.Title) With {.Tag = item}
        AddHandler menuItem.Click, Sub(sender As Object, args As EventArgs) onOpen(CType(CType(sender, ToolStripMenuItem).Tag, ShellItem))
        Return menuItem
    End Function

    Private Shared Sub RejectDuplicateKeys(items As List(Of ShellItem))
        Dim duplicate = items.GroupBy(Function(item) item.Entry.EntryKey, StringComparer.Ordinal).FirstOrDefault(Function(g) g.Count() > 1)
        If duplicate IsNot Nothing Then
            Throw New InvalidOperationException($"Two menu entries use the key '{duplicate.Key}'. Keys must be unique across modules.")
        End If
    End Sub
End Class
