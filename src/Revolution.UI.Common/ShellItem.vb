Imports System.Windows.Forms

''' <summary>A menu entry together with the factory that opens its screen. Modules build these; the shell shows them (ADR 0016).</summary>
Public NotInheritable Class ShellItem
    Public Sub New(entry As MenuEntry, openScreen As Func(Of Form))
        If entry Is Nothing Then Throw New ArgumentNullException(NameOf(entry))
        If openScreen Is Nothing Then Throw New ArgumentNullException(NameOf(openScreen))
        Me.Entry = entry
        Me.OpenScreen = openScreen
    End Sub

    Public ReadOnly Property Entry As MenuEntry
    ''' <summary>Creates a new, unshown screen (a BaseForm). The shell shows it and closes it when the company changes.</summary>
    Public ReadOnly Property OpenScreen As Func(Of Form)
End Class
