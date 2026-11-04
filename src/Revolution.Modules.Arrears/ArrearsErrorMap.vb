''' <summary>Turns the arrears procedures' error numbers (53100 to 53109) into messages a screen can show. Nothing for any other number.</summary>
Public NotInheritable Class ArrearsErrorMap
    Private Sub New()
    End Sub

    Public Shared Function Translate(number As Integer, message As String) As String
        Select Case number
            Case 53100 : Return "Employee not found."
            Case 53101 : Return "A month is already under an arrears claim for this employee. Cancel that claim first, unless it is already paid."
            Case 53102 : Return "Give at least one wage month, each month once."
            Case 53103 : Return "Arrears claim not found."
            Case 53104 : Return "The arrears claim is already cancelled."
            Case 53105 : Return "A paid arrears claim cannot be cancelled."
            Case 53106 : Return "A claim is not open any more. Load the list again."
            Case 53107 : Return "The arrears run is not found."
            Case 53108 : Return "The arrears run already has lines."
            Case 53109 : Return "A claim that a regular run is recovering cannot be cancelled. Return that run to Draft first."
            Case 50002 : Return "No company is selected."
            Case Else : Return Nothing
        End Select
    End Function
End Class
