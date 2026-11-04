''' <summary>A stored punch with its device and, when matched, its employee.</summary>
Public NotInheritable Class PunchRow
    Public Sub New(punchId As Long, deviceId As Integer, deviceCode As String, factoryId As Integer, deviceUserId As Integer?, cardNo As String, punchTime As Date,
                   verifyMode As Byte?, deviceStatus As Byte?, employeeId As Integer?, empCode As String, fullName As String)
        Me.PunchId = punchId
        Me.DeviceId = deviceId
        Me.DeviceCode = deviceCode
        Me.FactoryId = factoryId
        Me.DeviceUserId = deviceUserId
        Me.CardNo = cardNo
        Me.PunchTime = punchTime
        Me.VerifyMode = verifyMode
        Me.DeviceStatus = deviceStatus
        Me.EmployeeId = employeeId
        Me.EmpCode = empCode
        Me.FullName = fullName
    End Sub

    Public ReadOnly Property PunchId As Long
    Public ReadOnly Property DeviceId As Integer
    Public ReadOnly Property DeviceCode As String
    Public ReadOnly Property FactoryId As Integer
    Public ReadOnly Property DeviceUserId As Integer?
    Public ReadOnly Property CardNo As String
    Public ReadOnly Property PunchTime As Date
    Public ReadOnly Property VerifyMode As Byte?
    Public ReadOnly Property DeviceStatus As Byte?
    Public ReadOnly Property EmployeeId As Integer?
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property FullName As String
    Public ReadOnly Property IsMatched As Boolean
        Get
            Return EmployeeId.HasValue
        End Get
    End Property
End Class
