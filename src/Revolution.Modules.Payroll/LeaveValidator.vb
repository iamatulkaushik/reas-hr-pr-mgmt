Imports Revolution.Core

''' <summary>Checks leave types and ledger postings before they reach the database. The database checks the same rules again.</summary>
Public NotInheritable Class LeaveValidator
    Private Const MaxDays As Decimal = 366D
    Private Const MaxCodeLength As Integer = 10
    Private Const MaxNameLength As Integer = 100
    Private Const MaxNoteLength As Integer = 200

    Private Sub New()
    End Sub

    Public Shared Function ValidateType(leaveType As LeaveTypeItem) As OperationResult
        If leaveType Is Nothing Then Return OperationResult.Fail("Enter the leave type.")
        If String.IsNullOrWhiteSpace(leaveType.Code) OrElse leaveType.Code.Trim().Length > MaxCodeLength Then Return OperationResult.Fail($"Enter a code of up to {MaxCodeLength} characters.")
        If String.IsNullOrWhiteSpace(leaveType.Name) OrElse leaveType.Name.Trim().Length > MaxNameLength Then Return OperationResult.Fail($"Enter a name of up to {MaxNameLength} characters.")
        If leaveType.MaxEncashDays.HasValue AndAlso leaveType.MaxEncashDays.Value <= 0D Then Return OperationResult.Fail("The encashment limit must be more than zero, or empty for no limit.")
        If leaveType.MaxCarryForwardDays.HasValue AndAlso leaveType.MaxCarryForwardDays.Value <= 0D Then Return OperationResult.Fail("The carry-forward limit must be more than zero, or empty for no limit.")
        If leaveType.MaxEncashDays.HasValue AndAlso Not leaveType.IsEncashable Then Return OperationResult.Fail("A leave type that cannot be encashed has no encashment limit.")
        Return OperationResult.Ok()
    End Function

    Public Shared Function ValidatePosting(leaveType As LeaveTypeItem, kind As LeaveEntryKind, days As Decimal, entryDate As Date, note As String) As OperationResult
        If leaveType Is Nothing Then Return OperationResult.Fail("Pick a leave type.")
        If Not [Enum].IsDefined(GetType(LeaveEntryKind), kind) Then Return OperationResult.Fail("Pick what the entry is.")
        If days = 0D OrElse Math.Abs(days) > MaxDays OrElse days <> Math.Round(days, 2) Then Return OperationResult.Fail("Enter days up to two decimals, more than zero.")
        If kind <> LeaveEntryKind.Adjustment AndAlso days < 0D Then Return OperationResult.Fail("Enter the days as a positive number.")
        If entryDate.Year < 2000 Then Return OperationResult.Fail("Enter a valid date.")
        If note IsNot Nothing AndAlso note.Length > MaxNoteLength Then Return OperationResult.Fail($"The note can be up to {MaxNoteLength} characters.")
        If kind = LeaveEntryKind.Encashed AndAlso Not leaveType.IsEncashable Then Return OperationResult.Fail("This leave type cannot be encashed.")
        If kind = LeaveEntryKind.CarryForward AndAlso leaveType.MaxCarryForwardDays.HasValue AndAlso days > leaveType.MaxCarryForwardDays.Value Then
            Return OperationResult.Fail($"At most {leaveType.MaxCarryForwardDays.Value} days can be carried forward.")
        End If
        Return OperationResult.Ok()
    End Function
End Class
