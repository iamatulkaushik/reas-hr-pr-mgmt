Imports Revolution.Data

''' <summary>Composition class of the Attendance module (ADR 0015). Revolution.App calls Compose once with the shared host.</summary>
Public NotInheritable Class AttendanceModule
    Private Sub New()
    End Sub

    Public Shared Function Compose(host As ModuleHost) As AttendanceServices
        If host Is Nothing Then Throw New ArgumentNullException(NameOf(host))
        Dim today As Func(Of Date) = Function() Date.Today
        Dim repository As New SqlAttendanceRepository(host.Factory)
        Dim attendance As New AttendanceService(repository, host.Permissions, host.Entitlements, host.Audit, host.Session, today)
        Dim board As New AttendanceBoardService(New SqlAttendanceBoardRepository(host.Factory), attendance, repository, host.Permissions, today)
        Dim roster As New RosterService(New SqlRosterRepository(host.Factory), host.Permissions, host.Entitlements, host.Audit, today)
        Return New AttendanceServices(attendance, board, roster)
    End Function
End Class
