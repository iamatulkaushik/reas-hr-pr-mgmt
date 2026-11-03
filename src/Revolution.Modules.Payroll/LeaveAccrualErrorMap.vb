''' <summary>Turns the leave accrual procedures' error numbers (53200 to 53203, and 50002) into messages a screen can show. Nothing for any other number.</summary>
Public NotInheritable Class LeaveAccrualErrorMap
    Private Sub New()
    End Sub

    Public Shared Function Translate(number As Integer) As String
        Select Case number
            Case 53200 : Return "Leave type not found."
            Case 53201 : Return "The accrual values are not valid."
            Case 53202 : Return "Leave for this month is already posted for this leave type."
            Case 53203 : Return "An employee in the accrual is not in the company."
            Case 50002 : Return "No company is selected."
            Case Else : Return Nothing
        End Select
    End Function
End Class
