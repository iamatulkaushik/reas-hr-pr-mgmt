''' <summary>Storage for leave types and the leave ledger (pay.usp_LeaveType_*, usp_LeaveLedger_*, usp_LeaveBalance_List).</summary>
Public Interface ILeaveRepository
    Function ListTypes() As IReadOnlyList(Of LeaveTypeItem)
    ''' <summary>Adds the type when LeaveTypeId is Nothing, otherwise changes it. Returns the id.</summary>
    Function SaveType(leaveType As LeaveTypeItem) As Integer
    ''' <summary>Adds a ledger row. Days is positive for every kind except Adjustment; the database applies the sign. Returns the ledger id.</summary>
    Function Post(employeeId As Integer, leaveTypeId As Integer, leaveYear As Integer, entryDate As Date, kind As LeaveEntryKind, days As Decimal,
                  note As String, userId As Integer?) As Long
    Function ListBalances(employeeId As Integer) As IReadOnlyList(Of LeaveBalanceRow)
    Function ListEntries(employeeId As Integer, leaveTypeId As Integer?) As IReadOnlyList(Of LeaveEntryView)
End Interface
