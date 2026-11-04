''' <summary>Turns the biometric procedures' error numbers (53300 to 53306, and 50002) into messages a screen can show. Nothing for any other number.</summary>
Public NotInheritable Class BiometricErrorMap
    Private Sub New()
    End Sub

    Public Shared Function Translate(number As Integer) As String
        Select Case number
            Case 53300 : Return "Location not found."
            Case 53301 : Return "The device code is already used."
            Case 53302 : Return "Device not found or not active."
            Case 53303 : Return "The values are not valid."
            Case 53304 : Return "Employee not found."
            Case 53305 : Return "The card is already mapped to another employee."
            Case 53306 : Return "The employee has no active card."
            Case 50002 : Return "No company is selected."
            Case Else : Return Nothing
        End Select
    End Function
End Class
