''' <summary>The services of the Arrears module that screens use.</summary>
Public NotInheritable Class ArrearsServices
    Public Sub New(arrears As ArrearsService)
        If arrears Is Nothing Then Throw New ArgumentNullException(NameOf(arrears))
        Me.Arrears = arrears
    End Sub

    Public ReadOnly Property Arrears As ArrearsService
End Class
