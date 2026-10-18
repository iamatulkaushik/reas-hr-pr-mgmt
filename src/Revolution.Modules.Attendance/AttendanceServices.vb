''' <summary>The services of the Attendance module, built once by AttendanceModule.Compose and handed to its forms.</summary>
Public NotInheritable Class AttendanceServices
    Public Sub New(attendance As AttendanceService, board As AttendanceBoardService, roster As RosterService)
        If attendance Is Nothing Then Throw New ArgumentNullException(NameOf(attendance))
        If board Is Nothing Then Throw New ArgumentNullException(NameOf(board))
        If roster Is Nothing Then Throw New ArgumentNullException(NameOf(roster))
        Me.Attendance = attendance
        Me.Board = board
        Me.Roster = roster
    End Sub

    Public ReadOnly Property Attendance As AttendanceService
    Public ReadOnly Property Board As AttendanceBoardService
    Public ReadOnly Property Roster As RosterService
End Class
