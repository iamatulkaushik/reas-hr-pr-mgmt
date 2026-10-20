Imports System.Windows.Forms

''' <summary>
''' Builds the module menus for the shell from the registered modules, showing only what the user may open (ADR 0016).
''' A MenuEntry group is a path: "Master|Company|Factory / Units" is Master > Company > Factory / Units.
''' </summary>
Public NotInheritable Class ShellMenuBuilder
    Private Const PathSeparator As Char = "|"c

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
        Dim roots As New List(Of ToolStripMenuItem)()
        Dim nodes As New Dictionary(Of String, ToolStripMenuItem)(StringComparer.Ordinal)
        For Each entry In visible
            Dim target = FindOrCreateGroup(entry.MenuGroup, roots, nodes)
            target.DropDownItems.Add(CreateItem(byKey(entry.EntryKey), onOpen))
        Next
        Return roots
    End Function

    Private Shared Function FindOrCreateGroup(path As String, roots As List(Of ToolStripMenuItem),
                                              nodes As Dictionary(Of String, ToolStripMenuItem)) As ToolStripMenuItem
        Dim parent As ToolStripMenuItem = Nothing
        Dim key As String = String.Empty
        For Each part In path.Split(PathSeparator)
            Dim name = part.Trim()
            key = If(key.Length = 0, name, key & PathSeparator & name)
            Dim node As ToolStripMenuItem = Nothing
            If Not nodes.TryGetValue(key, node) Then
                node = New ToolStripMenuItem(name)
                nodes.Add(key, node)
                If parent Is Nothing Then
                    roots.Add(node)
                Else
                    parent.DropDownItems.Add(node)
                End If
            End If
            parent = node
        Next
        Return parent
    End Function

    Private Shared Function CreateItem(item As ShellItem, onOpen As Action(Of ShellItem)) As ToolStripMenuItem
        Dim menuItem As New ToolStripMenuItem(item.Entry.Title) With {.Tag = item}
        If item.IsPlanned Then
            menuItem.Enabled = False
            menuItem.ToolTipText = "Planned: not available yet."
            Return menuItem
        End If
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
