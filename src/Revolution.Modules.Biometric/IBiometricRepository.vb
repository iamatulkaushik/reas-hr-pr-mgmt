''' <summary>Storage for devices, RFID cards and punches (att.usp_Device_*, usp_RfidCard_*, usp_Punch_List, usp_Punch_Resolve).</summary>
Public Interface IBiometricRepository
    Function ListDevices(includeInactive As Boolean) As IReadOnlyList(Of DeviceItem)
    ''' <summary>Adds or changes a device. Returns its id.</summary>
    Function SaveDevice(device As DeviceWrite, userId As Integer?) As Integer
    Function ListCards(employeeId As Integer?, includeRetired As Boolean) As IReadOnlyList(Of RfidCardItem)
    ''' <summary>Maps a card to an employee. The employee's old card is retired; a card on another active employee is refused.</summary>
    Sub MapCard(employeeId As Integer, cardNo As String, userId As Integer?)
    Sub UnmapCard(employeeId As Integer, userId As Integer?)
    Function ListPunches(filter As PunchFilter) As IReadOnlyList(Of PunchRow)
    Function ResolvePunches() As ResolveResult
    ''' <summary>Matched punch times from fromTime up to but not including toTime, in employee and time order (att.usp_Punch_Times).</summary>
    Function ListPunchTimes(fromTime As Date, toTime As Date) As IReadOnlyList(Of EmployeePunchTime)
End Interface
