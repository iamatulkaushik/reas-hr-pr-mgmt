''' <summary>Turns the arrears procedures' error numbers (53100 to 53104) into messages a screen can show. Nothing for any other number.</summary>
Public NotInheritable Class ArrearsErrorMap
    Private Sub New()
    End Sub

    Public Shared Function Translate(number As Integer, message As String) As String
        Select Case number
            Case 53100 : Return "Employee not found."
            Case 53101 : Return "A month is already under an open arrears claim for this employee. Cancel that claim first."
            Case 53102 : Return "Give at least one wage month, each month once."
            Case 53103 : Return "Arrears claim not found."
            Case 53104 : Return "The arrears claim is already cancelled."
            Case 50002 : Return "No company is selected."
            Case Else : Return Nothing
        End Select
    End Function
End Class
