Imports Revolution.Modules.Biometric

''' <summary>The brand readers this agent knows. One per brand, each in its own project. eSSL is not here yet: the reader waits for the Owner's decision (pull over the ZK protocol, or devices that push), so a device of that brand is reported as having no reader and nothing is lost: the punches stay on the device.</summary>
Public NotInheritable Class AgentReaders
    Private Sub New()
    End Sub

    Public Shared Function All() As IReadOnlyList(Of IPunchSource)
        Return New List(Of IPunchSource)()
    End Function
End Class
