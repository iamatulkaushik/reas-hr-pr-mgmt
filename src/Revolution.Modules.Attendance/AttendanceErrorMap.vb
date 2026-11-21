''' <summary>
''' Decides which database errors from the att procedures are business rules. Error 50002 (no company in session) is a fault
''' in the session, not a user mistake, so it is not mapped and surfaces as a technical error.
''' </summary>
Public NotInheritable Class AttendanceErrorMap
    Private Const ErrorMonthClosed As Integer = 50001
    Private Const ErrorAlreadyClosed As Integer = 50003
    Private Const ErrorNotClosed As Integer = 50004
    Private Const ErrorReasonShort As Integer = 50005
    Private Const ErrorDailyNeedsPlatinum As Integer = 53601
    Private Const ErrorOtherKindOfRows As Integer = 53602
    Private Const ErrorNotEmployed As Integer = 53603
    Private Const ErrorNotComplete As Integer = 53604
    Private Const ErrorValueRange As Integer = 53605
    Private Const SqlConstraintConflict As Integer = 547
    Private Const EmployeeKeyName As String = "FK_AttendanceDaily_Employee"
    Private Const MonthlyEmployeeKeyName As String = "FK_AttendanceMonthly_Employee"

    Private Sub New()
    End Sub

    ''' <summary>The message for a rule error, or Nothing when the error must surface as a fault.</summary>
    Public Shared Function Translate(number As Integer, text As String) As String
        If number = SqlConstraintConflict Then
            Return If(text IsNot Nothing AndAlso (text.Contains(EmployeeKeyName, StringComparison.Ordinal) OrElse text.Contains(MonthlyEmployeeKeyName, StringComparison.Ordinal)), "Employee not found.", Nothing)
        End If
        If number = ErrorMonthClosed OrElse number = ErrorAlreadyClosed OrElse number = ErrorNotClosed OrElse number = ErrorReasonShort _
           OrElse number = ErrorDailyNeedsPlatinum OrElse number = ErrorOtherKindOfRows OrElse number = ErrorNotEmployed _
           OrElse number = ErrorNotComplete OrElse number = ErrorValueRange Then
            Return text
        End If
        Return Nothing
    End Function
End Class
