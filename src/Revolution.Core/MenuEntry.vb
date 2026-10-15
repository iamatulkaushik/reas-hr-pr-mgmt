''' <summary>
''' One menu item a module offers the shell. Metadata only: the screen it opens lives in the UI layer (ShellItem).
''' The shell shows the entry only when the user may View the module and the plan has the required feature (ADR 0016).
''' </summary>
Public NotInheritable Class MenuEntry
    Public Sub New(entryKey As String, menuGroup As String, title As String, moduleKey As ModuleKey,
                   Optional requiredFeature As PlanFeature? = Nothing, Optional sortOrder As Integer = 100)
        If String.IsNullOrWhiteSpace(entryKey) Then
            Throw New ArgumentException("A menu entry needs a key.", NameOf(entryKey))
        End If
        If String.IsNullOrWhiteSpace(menuGroup) Then
            Throw New ArgumentException("A menu entry needs a group.", NameOf(menuGroup))
        End If
        If String.IsNullOrWhiteSpace(title) Then
            Throw New ArgumentException("A menu entry needs a title.", NameOf(title))
        End If
        Me.EntryKey = entryKey.Trim()
        Me.MenuGroup = menuGroup.Trim()
        Me.Title = title.Trim()
        Me.ModuleKey = moduleKey
        Me.RequiredFeature = requiredFeature
        Me.SortOrder = sortOrder
    End Sub

    ''' <summary>Unique across all modules, for example "payroll.runs".</summary>
    Public ReadOnly Property EntryKey As String
    ''' <summary>The top-level menu the entry sits under, for example "Payroll".</summary>
    Public ReadOnly Property MenuGroup As String
    Public ReadOnly Property Title As String
    ''' <summary>The user needs View on this module to see the entry.</summary>
    Public ReadOnly Property ModuleKey As ModuleKey
    ''' <summary>The plan must include this feature. Nothing means any plan.</summary>
    Public ReadOnly Property RequiredFeature As PlanFeature?
    ''' <summary>Lower first. Groups follow the order of their first entry.</summary>
    Public ReadOnly Property SortOrder As Integer
End Class
