Imports System.Windows.Forms

''' <summary>
''' A menu entry together with the factory that opens its screen. Modules build these; the shell shows them (ADR 0016).
''' A planned item is a menu position agreed in advance whose screen does not exist yet: it is shown disabled.
''' </summary>
Public NotInheritable Class ShellItem
    Public Sub New(entry As MenuEntry, openScreen As Func(Of Form))
        If entry Is Nothing Then Throw New ArgumentNullException(NameOf(entry))
        If openScreen Is Nothing Then Throw New ArgumentNullException(NameOf(openScreen))
        Me.Entry = entry
        Me.OpenScreen = openScreen
    End Sub

    Private Sub New(entry As MenuEntry)
        If entry Is Nothing Then Throw New ArgumentNullException(NameOf(entry))
        Me.Entry = entry
        Me.OpenScreen = Function() CType(Nothing, Form)
        Me.IsPlanned = True
    End Sub

    Public ReadOnly Property Entry As MenuEntry
    ''' <summary>Creates a new, unshown screen (a BaseForm). The shell shows it and closes it when the company changes.</summary>
    Public ReadOnly Property OpenScreen As Func(Of Form)
    ''' <summary>True for a menu position without a screen yet. The shell shows it disabled.</summary>
    Public ReadOnly Property IsPlanned As Boolean

    Public Shared Function Planned(entry As MenuEntry) As ShellItem
        Return New ShellItem(entry)
    End Function
End Class
