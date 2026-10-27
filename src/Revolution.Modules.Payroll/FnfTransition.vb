''' <summary>
''' The moves a settlement may make: Calculated to Approved, Approved to Paid, and Approved back to Calculated. Calculating (Open or Calculated to Calculated) is a save,
''' not a move. The person who calculated it cannot approve it. The database checks the same rules; this is for the screen to show the right buttons.
''' </summary>
Public NotInheritable Class FnfTransition
    Private Sub New()
    End Sub

    Public Shared Function CanMove(current As FnfStatus, target As FnfStatus) As Boolean
        Return (current = FnfStatus.Calculated AndAlso target = FnfStatus.Approved) OrElse
               (current = FnfStatus.Approved AndAlso target = FnfStatus.Paid) OrElse
               (current = FnfStatus.Approved AndAlso target = FnfStatus.Calculated)
    End Function

    ''' <summary>True when the lines may still be changed: before approval.</summary>
    Public Shared Function CanEdit(current As FnfStatus) As Boolean
        Return current = FnfStatus.Open OrElse current = FnfStatus.Calculated
    End Function

    Public Shared Function CanApprove(makerUserId As Integer?, approverUserId As Integer) As Boolean
        Return Not makerUserId.HasValue OrElse makerUserId.Value <> approverUserId
    End Function
End Class
