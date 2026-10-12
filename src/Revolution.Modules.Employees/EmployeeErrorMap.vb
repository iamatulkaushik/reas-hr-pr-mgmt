''' <summary>Turns database rule errors from the mst procedures into plain messages. Error 50002 (no company in session) is a fault, not a rule, so it is not mapped.</summary>
Public NotInheritable Class EmployeeErrorMap
    Private Const SqlCheckOrForeignKey As Integer = 547
    Private Const SqlDuplicateIndex As Integer = 2601
    Private Const SqlDuplicateKey As Integer = 2627

    Private Shared ReadOnly ConstraintMessages As New Dictionary(Of String, String)(StringComparer.Ordinal) From {
        {"UQ_Employee_Code", "This employee code already exists."},
        {"UX_Employee_Biometric", "This biometric id belongs to another employee."},
        {"UX_EmpStat_Pan", "This PAN belongs to another employee."},
        {"UX_EmpStat_Aadhaar", "This Aadhaar number belongs to another employee."},
        {"UX_EmpStat_Uan", "This UAN belongs to another employee."},
        {"UX_EmpStat_EsiIp", "This ESI IP number belongs to another employee."},
        {"CK_Employee_MinAge", "Employee must be at least 14 years old on joining."},
        {"CK_Employee_Exit", "Exit date cannot be before the joining date."},
        {"FK_Employee_Department", "The department does not belong to the selected factory."},
        {"FK_Employee_Factory", "Factory not found."},
        {"FK_Employee_Section", "Section not found."},
        {"FK_Employee_Designation", "Designation not found."},
        {"FK_Employee_Grade", "Grade not found."},
        {"FK_Employee_ReportingTo", "Reporting person not found."},
        {"CK_EmpBank_BankMode", "Bank transfer needs an account number and an IFSC."},
        {"CK_EmpBank_Mode", "Payment mode is not valid."},
        {"FK_EmployeeBank_Bank", "Bank not found."},
        {"CK_EmpStat_Regime", "Tax regime must be old or new."}}

    Private Sub New()
    End Sub

    ''' <summary>The message for a rule error, or Nothing when the error is not a rule and must surface as a fault.</summary>
    Public Shared Function Translate(number As Integer, text As String) As String
        Select Case number
            Case 51002
                Return "Employee not found."
            Case 51003
                Return "Nominee shares cannot be above 100 percent."
            Case 51005
                Return "Record not found."
            Case 51006
                Return "The employee was changed by someone else. Reload and try again."
            Case 51007
                Return "Exit status must be Left, Retired, Deceased or Absconded."
            Case SqlDuplicateIndex, SqlDuplicateKey, SqlCheckOrForeignKey
                Return FromConstraint(number, text)
            Case Else
                Return Nothing
        End Select
    End Function

    Private Shared Function FromConstraint(number As Integer, text As String) As String
        For Each pair In ConstraintMessages
            If text IsNot Nothing AndAlso text.Contains(pair.Key, StringComparison.Ordinal) Then
                Return pair.Value
            End If
        Next
        Return If(number = SqlCheckOrForeignKey, "One of the selected values is not valid for this company.", "This value already exists.")
    End Function
End Class
