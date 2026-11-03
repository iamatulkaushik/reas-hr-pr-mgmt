''' <summary>A leave type with its accrual rule. Setting is Nothing when the company has not set one: the type then earns nothing by accrual.</summary>
Public NotInheritable Class LeaveAccrualSettingItem
    Public Sub New(leaveTypeId As Integer, code As String, name As String, setting As LeaveAccrualSetting)
        Me.LeaveTypeId = leaveTypeId
        Me.Code = code
        Me.Name = name
        Me.Setting = setting
    End Sub

    Public ReadOnly Property LeaveTypeId As Integer
    Public ReadOnly Property Code As String
    Public ReadOnly Property Name As String
    Public ReadOnly Property Setting As LeaveAccrualSetting
End Class
