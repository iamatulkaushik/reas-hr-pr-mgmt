''' <summary>Storage for leave accrual (pay.usp_LeaveAccrualSetting_*, usp_LeaveAccrual_Present, usp_LeaveAccrual_Post).</summary>
Public Interface ILeaveAccrualSource
    ''' <summary>Every active leave type with its rule, or Nothing as the rule when none is set.</summary>
    Function ListSettings() As IReadOnlyList(Of LeaveAccrualSettingItem)
    Sub SaveSetting(leaveTypeId As Integer, setting As LeaveAccrualSetting, userId As Integer?)
    ''' <summary>One input per employee who works in the month (first day of the month).</summary>
    Function ListPresent(leaveTypeId As Integer, month As Date) As IReadOnlyList(Of LeaveAccrualInput)
    ''' <summary>Writes the marker and the Accrual ledger rows together. The database refuses a second posting for the same leave type and month. Returns the employee count.</summary>
    Function Post(leaveTypeId As Integer, month As Date, entries As IReadOnlyList(Of LeaveAccrualEntry), userId As Integer?) As Integer
End Interface
