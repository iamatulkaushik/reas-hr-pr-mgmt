''' <summary>An employee as a biometric user: the user id the devices know the person by, the active RFID card, and the newest punch. Not tied to any device: any device that reports the id or the card is matched.</summary>
Public NotInheritable Class BiometricUserItem
    Public Sub New(employeeId As Integer, empCode As String, fullName As String, biometricId As Integer?, cardNo As String, lastPunchTime As Date?, punches30 As Integer)
        Me.EmployeeId = employeeId
        Me.EmpCode = empCode
        Me.FullName = fullName
        Me.BiometricId = biometricId
        Me.CardNo = cardNo
        Me.LastPunchTime = lastPunchTime
        Me.Punches30 = punches30
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property FullName As String
    Public ReadOnly Property BiometricId As Integer?
    Public ReadOnly Property CardNo As String
    Public ReadOnly Property LastPunchTime As Date?
    ''' <summary>Punches in the last 30 days.</summary>
    Public ReadOnly Property Punches30 As Integer

    Public ReadOnly Property IsEnrolled As Boolean
        Get
            Return BiometricId.HasValue OrElse Not String.IsNullOrEmpty(CardNo)
        End Get
    End Property
End Class
