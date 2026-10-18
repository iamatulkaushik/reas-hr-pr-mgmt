''' <summary>Rule errors of the roster procedures (0206). 50002 (no company in session) is a fault, not a rule, so it is not mapped.</summary>
Public NotInheritable Class RosterErrorMap
    Private Const ErrorShiftOrder As Integer = 50020
    Private Const ErrorNotFound As Integer = 50021
    Private Const SqlDuplicateIndex As Integer = 2601
    Private Const SqlDuplicateKey As Integer = 2627
    Private Const ShiftNameKey As String = "UQ_Shift_Name"

    Private Sub New()
    End Sub

    ''' <summary>The message for a rule error, or Nothing when the error must surface as a fault.</summary>
    Public Shared Function Translate(number As Integer, text As String) As String
        If number = ErrorShiftOrder OrElse number = ErrorNotFound Then Return text
        If (number = SqlDuplicateKey OrElse number = SqlDuplicateIndex) AndAlso text IsNot Nothing AndAlso
           text.Contains(ShiftNameKey, StringComparison.Ordinal) Then
            Return "This shift name already exists."
        End If
        Return Nothing
    End Function
End Class
