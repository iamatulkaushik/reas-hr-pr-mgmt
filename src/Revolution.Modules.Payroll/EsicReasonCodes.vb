''' <summary>Reason codes that need a last working day, per the ESIC template. Becomes a format table row later.</summary>
Public NotInheritable Class EsicReasonCodes
    Public Shared ReadOnly LastWorkingDayRequired As IReadOnlyCollection(Of Integer) = New Integer() {2, 3, 4, 5, 6, 10}

    Private Sub New()
    End Sub
End Class
