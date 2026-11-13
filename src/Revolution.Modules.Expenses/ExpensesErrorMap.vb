''' <summary>Turns the expense procedures' error numbers (53520 to 53528, and 50002) into messages a screen can show. Nothing for any other number. The two limit errors (53525, 53526) carry the limit in the database message, which the repository passes on.</summary>
Public NotInheritable Class ExpensesErrorMap
    Private Sub New()
    End Sub

    Public Shared Function Translate(number As Integer) As String
        Select Case number
            Case 53520 : Return "The values are not valid."
            Case 53521 : Return "The category name is already used."
            Case 53522 : Return "Category not found or not active."
            Case 53523 : Return "Employee not found."
            Case 53524 : Return "This bill is already claimed."
            Case 53525 : Return "The claim is over the limit per claim for this category."
            Case 53526 : Return "The claims of the month would go over the monthly limit for this category."
            Case 53527 : Return "Claim not found."
            Case 53528 : Return "The claim cannot move from its current status."
            Case 50002 : Return "No company is selected."
            Case Else : Return Nothing
        End Select
    End Function

    ''' <summary>True for the errors whose database message names the limit and should be shown as it is.</summary>
    Public Shared Function CarriesLimit(number As Integer) As Boolean
        Return number = 53525 OrElse number = 53526
    End Function
End Class
