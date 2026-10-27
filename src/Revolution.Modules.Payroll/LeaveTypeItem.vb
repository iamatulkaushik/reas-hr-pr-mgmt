''' <summary>A leave type a company defines (pay.LeaveType). LeaveTypeId is Nothing for a type not saved yet. A limit of Nothing means no limit.</summary>
Public NotInheritable Class LeaveTypeItem
    Public Sub New(leaveTypeId As Integer?, code As String, name As String, isEncashable As Boolean, maxEncashDays As Decimal?,
                   maxCarryForwardDays As Decimal?, isActive As Boolean)
        Me.LeaveTypeId = leaveTypeId
        Me.Code = code
        Me.Name = name
        Me.IsEncashable = isEncashable
        Me.MaxEncashDays = maxEncashDays
        Me.MaxCarryForwardDays = maxCarryForwardDays
        Me.IsActive = isActive
    End Sub

    Public ReadOnly Property LeaveTypeId As Integer?
    Public ReadOnly Property Code As String
    Public ReadOnly Property Name As String
    Public ReadOnly Property IsEncashable As Boolean
    Public ReadOnly Property MaxEncashDays As Decimal?
    Public ReadOnly Property MaxCarryForwardDays As Decimal?
    Public ReadOnly Property IsActive As Boolean
End Class
