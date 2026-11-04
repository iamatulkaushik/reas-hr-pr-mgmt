''' <summary>An RFID card mapped to an employee. A retired card has a RetiredUtc. Rows are never deleted.</summary>
Public NotInheritable Class RfidCardItem
    Public Sub New(cardId As Integer, employeeId As Integer, empCode As String, fullName As String, cardNo As String, isActive As Boolean, mappedUtc As Date, retiredUtc As Date?)
        Me.CardId = cardId
        Me.EmployeeId = employeeId
        Me.EmpCode = empCode
        Me.FullName = fullName
        Me.CardNo = cardNo
        Me.IsActive = isActive
        Me.MappedUtc = mappedUtc
        Me.RetiredUtc = retiredUtc
    End Sub

    Public ReadOnly Property CardId As Integer
    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property FullName As String
    Public ReadOnly Property CardNo As String
    Public ReadOnly Property IsActive As Boolean
    Public ReadOnly Property MappedUtc As Date
    Public ReadOnly Property RetiredUtc As Date?
End Class
