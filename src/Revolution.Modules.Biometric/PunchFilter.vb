''' <summary>What the punch list shows: dates from FromDate to ToDate inclusive, and optionally one employee, one device, or only punches nobody matched.</summary>
Public NotInheritable Class PunchFilter
    Public Property FromDate As Date
    Public Property ToDate As Date
    Public Property EmployeeId As Integer?
    Public Property DeviceId As Integer?
    Public Property UnmatchedOnly As Boolean
End Class
