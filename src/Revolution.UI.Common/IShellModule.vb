''' <summary>What a feature module gives the shell: its menu items. One implementation per module (ADR 0016).</summary>
Public Interface IShellModule
    ReadOnly Property Items As IReadOnlyList(Of ShellItem)
End Interface
