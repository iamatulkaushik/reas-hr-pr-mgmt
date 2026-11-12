''' <summary>Messages for the asset recovery procedures that payroll calls (53514 to 53517, and 50002). Nothing for any other number.</summary>
Friend NotInheritable Class AssetPayMessages
    Private Sub New()
    End Sub

    Friend Shared Function Translate(number As Integer) As String
        Select Case number
            Case 53514 : Return "The regular run is not found, or it can no longer take recoveries."
            Case 53515 : Return "An asset recovery is not open. Process the run again."
            Case 53516 : Return "The asset recoveries changed. Calculate the settlement again."
            Case 53517 : Return "The regular run is approved, locked or paid, so its asset recoveries stay."
            Case 50002 : Return "No company is selected."
            Case Else : Return Nothing
        End Select
    End Function
End Class
