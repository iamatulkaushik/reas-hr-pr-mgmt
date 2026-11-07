Imports System.Windows.Forms
Imports Revolution.Modules.Biometric

''' <summary>The Biometric module's menu items for the shell (ADR 0016): biometric users and attendance from punches, each on its own form. Sigma places them in the menu layout (change request 0024).</summary>
Public NotInheritable Class BiometricShellModule
    Implements IShellModule

    Private Const ProcessGroup As String = "Process"
    Private Const UsersSort As Integer = 560
    Private Const PunchAttendanceSort As Integer = 565

    Private ReadOnly _items As IReadOnlyList(Of ShellItem)

    Public Sub New(services As BiometricServices)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        _items = New List(Of ShellItem) From {
            New ShellItem(New MenuEntry("biometric.users", ProcessGroup, "Biometric users", ModuleKey.Biometric, PlanFeature.Biometric, UsersSort),
                          Function() CType(New BiometricUsersForm(services), Form)),
            New ShellItem(New MenuEntry("biometric.punchattendance", ProcessGroup, "Attendance from punches", ModuleKey.Biometric, PlanFeature.Biometric, PunchAttendanceSort),
                          Function() CType(New PunchAttendanceForm(services), Form))}
    End Sub

    Public ReadOnly Property Items As IReadOnlyList(Of ShellItem) Implements IShellModule.Items
        Get
            Return _items
        End Get
    End Property
End Class
