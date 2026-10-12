''' <summary>Checks the ids being replaced. Clear and Unchanged need no check.</summary>
Public NotInheritable Class IdentifierValidator
    Private Sub New()
    End Sub

    Public Shared Function Validate(input As IdentifierInput) As IReadOnlyList(Of String)
        If input Is Nothing Then
            Throw New ArgumentNullException(NameOf(input))
        End If
        Dim problems As New List(Of String)
        Check(input.Uan, AddressOf UanValidator.IsValid, "UAN must be 12 digits.", problems)
        Check(input.EsiIp, AddressOf EsiIpValidator.IsValid, "ESI IP number must be 10 digits.", problems)
        Check(input.Pan, AddressOf PanValidator.IsValid, "PAN format is wrong.", problems)
        Check(input.Aadhaar, AddressOf AadhaarValidator.IsValid, "Aadhaar number is not valid.", problems)
        Return problems
    End Function

    Private Shared Sub Check(edit As IdEdit, isValid As Func(Of String, Boolean), message As String, problems As List(Of String))
        If edit.Action = IdAction.Replace AndAlso (String.IsNullOrWhiteSpace(edit.Plain) OrElse Not isValid(edit.Plain)) Then
            problems.Add(message)
        End If
    End Sub
End Class
