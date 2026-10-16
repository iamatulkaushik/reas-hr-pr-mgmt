''' <summary>Result of checking a licence file: the grant when it is genuine, otherwise the reason it was refused.</summary>
Public NotInheritable Class LicenceCheck
    Private Sub New(grant As LicenceGrant, problem As LicenceProblem, message As String)
        Me.Grant = grant
        Me.Problem = problem
        Me.Message = message
    End Sub

    Public ReadOnly Property Grant As LicenceGrant
    Public ReadOnly Property Problem As LicenceProblem
    Public ReadOnly Property Message As String

    Public ReadOnly Property Succeeded As Boolean
        Get
            Return Problem = LicenceProblem.None
        End Get
    End Property

    Public Shared Function Ok(grant As LicenceGrant) As LicenceCheck
        If grant Is Nothing Then Throw New ArgumentNullException(NameOf(grant))
        Return New LicenceCheck(grant, LicenceProblem.None, String.Empty)
    End Function

    Public Shared Function Fail(problem As LicenceProblem, message As String) As LicenceCheck
        If problem = LicenceProblem.None Then
            Throw New ArgumentException("A failure needs a problem.", NameOf(problem))
        End If
        Return New LicenceCheck(Nothing, problem, message)
    End Function
End Class
