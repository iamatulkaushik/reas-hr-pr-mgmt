Imports Revolution.Modules.Payroll

Friend NotInheritable Class FakeLeaveRepository
    Implements ILeaveRepository

    Public ReadOnly Types As New List(Of LeaveTypeItem) From {
        New LeaveTypeItem(1, "EL", "Earned leave", True, 30D, 30D, True),
        New LeaveTypeItem(2, "CL", "Casual leave", False, Nothing, Nothing, True)}
    Public ReadOnly SavedTypes As New List(Of LeaveTypeItem)()
    Public ReadOnly Posted As New List(Of String)()
    Public LastLeaveYear As Integer
    Public LastUserId As Integer?
    Public Balances As New List(Of LeaveBalanceRow)()
    Public Entries As New List(Of LeaveEntryView)()
    Public FailWith As String

    Public Function ListTypes() As IReadOnlyList(Of LeaveTypeItem) Implements ILeaveRepository.ListTypes
        Return Types
    End Function

    Public Function SaveType(leaveType As LeaveTypeItem) As Integer Implements ILeaveRepository.SaveType
        ThrowIfFailing()
        SavedTypes.Add(leaveType)
        Return If(leaveType.LeaveTypeId, 55)
    End Function

    Public Function Post(employeeId As Integer, leaveTypeId As Integer, leaveYear As Integer, entryDate As Date, kind As LeaveEntryKind, days As Decimal,
                         note As String, userId As Integer?) As Long Implements ILeaveRepository.Post
        ThrowIfFailing()
        Posted.Add($"{employeeId}:{leaveTypeId}:{kind}:{days}:{note}")
        LastLeaveYear = leaveYear
        LastUserId = userId
        Return 700
    End Function

    Public Function ListBalances(employeeId As Integer) As IReadOnlyList(Of LeaveBalanceRow) Implements ILeaveRepository.ListBalances
        Return Balances
    End Function

    Public Function ListEntries(employeeId As Integer, leaveTypeId As Integer?) As IReadOnlyList(Of LeaveEntryView) Implements ILeaveRepository.ListEntries
        Return Entries
    End Function

    Private Sub ThrowIfFailing()
        If FailWith IsNot Nothing Then Throw New PayrollRuleException(FailWith, 50352, Nothing)
    End Sub
End Class
