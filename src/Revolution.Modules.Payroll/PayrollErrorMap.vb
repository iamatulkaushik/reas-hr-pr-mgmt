''' <summary>
''' Decides which database errors from the pay procedures are business rules. Error 50002 (no company in session) and 50330 (no user in
''' session) are faults in the session, not user mistakes, so they are not mapped and surface as technical errors.
''' Mapped numbers keep the text the procedure threw. Lane 0300 uses 50301 to 50304 (salary), 50311 to 50319 and 50329 (run), 50321 to 50328 (loans).
''' </summary>
Public NotInheritable Class PayrollErrorMap
    Private Const SqlConstraintConflict As Integer = 547
    Private Const SqlUniqueIndex As Integer = 2601
    Private Const SqlUniqueConstraint As Integer = 2627
    Private Const ComponentCodeKey As String = "UQ_SalaryComponent_Code"
    Private Const ComponentKindCheck As String = "CK_SalaryComponent_Kind"
    Private Const SalaryEmployeeKey As String = "FK_EmployeeSalary_Employee"

    Private Shared ReadOnly RuleNumbers As ISet(Of Integer) = New HashSet(Of Integer) From {
        50301, 50302, 50303, 50304,
        50311, 50313, 50315, 50316, 50318, 50319, 50329,
        50321, 50322, 50323, 50324, 50325, 50326, 50327, 50328}

    Private Sub New()
    End Sub

    ''' <summary>The message for a rule error, or Nothing when the error must surface as a fault.</summary>
    Public Shared Function Translate(number As Integer, text As String) As String
        If number = SqlConstraintConflict Then
            If Mentions(text, ComponentKindCheck) Then Return "A component cannot be both basic and DA."
            If Mentions(text, SalaryEmployeeKey) Then Return "Employee not found."
            Return Nothing
        End If
        If number = SqlUniqueConstraint OrElse number = SqlUniqueIndex Then
            Return If(Mentions(text, ComponentCodeKey), "A component with this code already exists.", Nothing)
        End If
        If RuleNumbers.Contains(number) AndAlso Not String.IsNullOrWhiteSpace(text) Then
            Return text
        End If
        Return Nothing
    End Function

    Private Shared Function Mentions(text As String, name As String) As Boolean
        Return text IsNot Nothing AndAlso text.Contains(name, StringComparison.Ordinal)
    End Function
End Class
