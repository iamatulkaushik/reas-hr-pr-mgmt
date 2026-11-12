''' <summary>Turns the asset procedures' error numbers (53500 to 53513, and 50002) into messages a screen can show. Nothing for any other number.</summary>
Public NotInheritable Class AssetsErrorMap
    Private Sub New()
    End Sub

    Public Shared Function Translate(number As Integer) As String
        Select Case number
            Case 53500 : Return "The values are not valid."
            Case 53501 : Return "The asset type name is already used."
            Case 53502 : Return "Asset type not found."
            Case 53503 : Return "The tag number is already used."
            Case 53504 : Return "Asset type not found or not active."
            Case 53505 : Return "Asset not found."
            Case 53506 : Return "The asset is not in stock."
            Case 53507 : Return "Employee not found."
            Case 53508 : Return "The asset is not out with an employee."
            Case 53509 : Return "The status cannot be changed from the current one."
            Case 53510 : Return "Only a lost or damaged asset that was issued to an employee can be recovered."
            Case 53511 : Return "The asset already has a recovery."
            Case 53512 : Return "Recovery not found."
            Case 53513 : Return "The recovery is not open, so it cannot be cancelled."
            Case 50002 : Return "No company is selected."
            Case Else : Return Nothing
        End Select
    End Function
End Class
